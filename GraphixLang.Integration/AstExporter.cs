using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using GraphixLang.Parser;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace GraphixLang.Integration
{
    public class AstExporter
    {
        private readonly string _pythonInterpreterPath = "/usr/bin/python3"; // Default for Mac
        private readonly string _scriptPath;
        private readonly string[] _requiredPackages = new[] { "Pillow", "piexif" };
        
        public AstExporter()
        {
            // Get the path to the Python interpreter script
            _scriptPath = Path.Combine(
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../")), 
                "GraphixLang.Interpreter", 
                "interpreter.py");
            
            // Check if Python is available
            if (!File.Exists(_pythonInterpreterPath))
            {
                // Try alternative paths for different OS
                if (File.Exists("/usr/local/bin/python3"))
                    _pythonInterpreterPath = "/usr/local/bin/python3";
                else if (File.Exists("/usr/bin/python"))
                    _pythonInterpreterPath = "/usr/bin/python";
                else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    // On Windows, try to use "python" command
                    _pythonInterpreterPath = "python";
                }
            }
        }

        /// <summary>
        /// Check if required packages are installed and install them if needed
        /// </summary>
        public string CheckAndInstallDependencies(bool autoInstall = false)
        {
            StringBuilder output = new StringBuilder();
            StringBuilder errors = new StringBuilder();

            foreach (var package in _requiredPackages)
            {
                try
                {
                    // Check if package is installed
                    var checkProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = _pythonInterpreterPath,
                            Arguments = $@"-c ""import {package.ToLower().Replace("pillow", "PIL")}; print('{package} is installed')""",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };
                    
                    checkProcess.Start();
                    string checkOutput = checkProcess.StandardOutput.ReadToEnd();
                    string checkError = checkProcess.StandardError.ReadToEnd();
                    checkProcess.WaitForExit();

                    if (checkProcess.ExitCode != 0)
                    {
                        // Package is not installed
                        if (autoInstall)
                        {
                            // Try to install package
                            output.AppendLine($"Installing {package}...");
                            
                            var installProcess = new Process
                            {
                                StartInfo = new ProcessStartInfo
                                {
                                    FileName = _pythonInterpreterPath,
                                    Arguments = $"-m pip install {package}",
                                    UseShellExecute = false,
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = true,
                                    CreateNoWindow = true
                                }
                            };
                            
                            installProcess.Start();
                            string installOutput = installProcess.StandardOutput.ReadToEnd();
                            string installError = installProcess.StandardError.ReadToEnd();
                            installProcess.WaitForExit();
                            
                            if (installProcess.ExitCode == 0)
                                output.AppendLine($"Successfully installed {package}");
                            else
                            {
                                errors.AppendLine($"Failed to install {package}. Error: {installError}");
                                errors.AppendLine($"Please install manually with: {_pythonInterpreterPath} -m pip install {package}");
                            }
                        }
                        else
                        {
                            errors.AppendLine($"Required package '{package}' is not installed.");
                            errors.AppendLine($"Please install with: {_pythonInterpreterPath} -m pip install {package}");
                        }
                    }
                    else
                    {
                        output.AppendLine($"{package} is installed");
                    }
                }
                catch (Exception ex)
                {
                    errors.AppendLine($"Error checking/installing {package}: {ex.Message}");
                }
            }

            if (errors.Length > 0)
                return errors.ToString();
            return output.ToString();
        }
        
        public string ExecuteAst(ProgramNode ast)
        {
            try
            {
                // First check if required packages are installed
                string dependencyCheck = CheckAndInstallDependencies(false);
                if (dependencyCheck.Contains("not installed"))
                {
                    return $"Missing required Python packages. Please install them:\n\n{dependencyCheck}";
                }
                
                // For debugging - uncomment to see the JSON structure
                // string debugJson = JsonConvert.SerializeObject(ast, Formatting.Indented);
                // Console.WriteLine($"Debug AST JSON: {debugJson}");
                
                // Define serialization settings to include type information
                var settings = new JsonSerializerSettings 
                {
                    TypeNameHandling = TypeNameHandling.None, // Don't add .NET type information
                    Formatting = Formatting.Indented,
                    // Add a custom converter to ensure explicit Type properties for each node
                    Converters = new List<JsonConverter> { new AstNodeTypeConverter() }
                };
                
                // Serialize AST to JSON with type information
                string jsonAst = JsonConvert.SerializeObject(ast, settings);
                
                // Write to temporary file
                string tempFile = Path.Combine(Path.GetTempPath(), $"ast_{Guid.NewGuid()}.json");
                File.WriteAllText(tempFile, jsonAst);
                
                // For debugging - uncomment to see the saved JSON
                // Console.WriteLine($"Saved AST to: {tempFile}");
                // Console.WriteLine($"JSON content: {jsonAst}");
                
                try
                {
                    // Create process to run Python interpreter
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = _pythonInterpreterPath,
                            Arguments = $@"""{_scriptPath}"" ""{tempFile}""",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };
                    
                    var outputBuilder = new StringBuilder();
                    var errorBuilder = new StringBuilder();
                    
                    // Set up output handlers
                    process.OutputDataReceived += (sender, args) => 
                    {
                        if (args.Data != null)
                            outputBuilder.AppendLine(args.Data);
                    };
                    
                    process.ErrorDataReceived += (sender, args) =>
                    {
                        if (args.Data != null)
                            errorBuilder.AppendLine(args.Data);
                    };
                    
                    // Start the process
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                    
                    // Check for errors
                    if (process.ExitCode != 0)
                    {
                        // Check if it's a missing package error
                        if (errorBuilder.ToString().Contains("No module named"))
                        {
                            string missingPackage = "a required package";
                            if (errorBuilder.ToString().Contains("'PIL'"))
                                missingPackage = "Pillow";
                            else if (errorBuilder.ToString().Contains("'piexif'"))
                                missingPackage = "piexif";
                            
                            // Return a helpful message on how to install the package
                            return $"Error: {missingPackage} is not installed. Please install it with:\n\n" + 
                                   $"{_pythonInterpreterPath} -m pip install {missingPackage}\n\n" +
                                   $"Or run the CheckAndInstallDependencies method to install all required packages.\n\n" +
                                   $"Error details:\n{errorBuilder}";
                        }
                        
                        throw new Exception($"Python interpreter exited with code {process.ExitCode}.\n{errorBuilder}");
                    }
                    
                    return outputBuilder.ToString();
                }
                finally
                {
                    // Clean up temp file
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                }
            }
            catch (Exception ex)
            {
                return $"Error executing AST: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Custom JsonConverter to ensure that each node has a Type property explicitly included
        /// </summary>
        private class AstNodeTypeConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return typeof(ASTNode).IsAssignableFrom(objectType);
            }
            
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotImplementedException("Reading JSON is not supported by this converter");
            }
            
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                // Start writing the object
                writer.WriteStartObject();
                
                // Write the type property first to ensure it's at the beginning of the JSON
                Type type = value.GetType();
                writer.WritePropertyName("type");
                
                // Remove the "Node" suffix from the type name to match what the Python interpreter expects
                string typeName = type.Name;
                if (typeName.EndsWith("Node"))
                {
                    typeName = typeName.Substring(0, typeName.Length - 4);
                }
                writer.WriteValue(typeName);
                
                // Write all other properties
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    // Skip certain properties that might cause circular references or aren't needed
                    if (prop.Name == "Type" || prop.Name == "Parent")
                        continue;
                    
                    // Get the property value
                    var propValue = prop.GetValue(value);
                    if (propValue != null)
                    {
                        writer.WritePropertyName(prop.Name);
                        
                        // Special handling for any TokenType enum properties
                        if (propValue is GraphixLang.Lexer.TokenType)
                        {
                            // Convert all TokenType enums to their string representation
                            writer.WriteValue(propValue.ToString());
                        }
                        else
                        {
                            serializer.Serialize(writer, propValue);
                        }
                    }
                }
                
                // End the object
                writer.WriteEndObject();
            }
        }
        
        // Optional: For debugging purposes
        public string DebugAstJson(ProgramNode ast)
        {
            var settings = new JsonSerializerSettings 
            {
                TypeNameHandling = TypeNameHandling.None,
                Formatting = Formatting.Indented,
                Converters = new List<JsonConverter> { new AstNodeTypeConverter() }
            };
            
            return JsonConvert.SerializeObject(ast, settings);
        }
    }
}
