using GraphixLang.Lexer;
using ParserNamespace = GraphixLang.Parser;
using GraphixLang.Interpreter;
using System;
using System.Collections.Generic;
using System.IO;

namespace GraphixLang.Presentation
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                // Process the specified file path
                string filePath = args[0];
                
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"File not found: {filePath}");
                    return;
                }
                
                // Get the directory containing the file to use as the base directory
                string baseDir = Path.GetDirectoryName(Path.GetFullPath(filePath));
                Console.WriteLine($"Using base directory: {baseDir}");
                
                ProcessFile(filePath, baseDir);
            }
            else
            {
                // No args provided - fall back to processing all files in TestInputs directory
                Console.WriteLine("No file path provided. Processing all files in TestInputs directory.");
                var testInputDir = Directory.EnumerateFiles("TestInputs");

                foreach (var testInputFile in testInputDir)
                {
                    if (testInputFile.Contains(".pixil"))
                    {
                        ProcessFile(testInputFile);
                    }
                }
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
        
        static void ProcessFile(string filePath, string baseDir = null)
        {
            string input = File.ReadAllText(filePath);
            Console.WriteLine($"\n\nProcessing file: {filePath}");
            Console.WriteLine(input);

            try
            {
                // Tokenization Process
                Tokenizer lexer = new Tokenizer(input);
                List<Token> tokens = lexer.Tokenize();

                Console.WriteLine("\nTokens, detailed view:");
                foreach (var token in tokens)
                    Console.WriteLine($"{token.Type}: '{token.Value}' at line {token.Line}, column {token.Column}");

                // Parsing Process
                var parser = new ParserNamespace.Parser(tokens);
                ParserNamespace.ProgramNode ast = parser.Parse();

                // Print the AST
                ParserNamespace.ASTPrinter printer = new ParserNamespace.ASTPrinter();
                string astString = printer.Print(ast);
                Console.WriteLine("\nAbstract Syntax Tree:");
                Console.WriteLine(astString);

                // Execute using the C# interpreter
                Console.WriteLine("\nExecuting using C# interpreter...");
                try
                {
                    // Pass the base directory to the interpreter
                    var interpreter = new GraphixInterpreter(baseDir);
                    string result = interpreter.ExecuteAst(ast);
                    Console.WriteLine("\nExecution result:");
                    Console.WriteLine(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nInterpreter execution error: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
            }
        }
    }
}