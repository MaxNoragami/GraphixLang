using GraphixLang.Lexer;
using ParserNamespace = GraphixLang.Parser;
using GraphixLang.Integration;
using System;
using System.Collections.Generic;
using System.IO;

namespace GraphixLang.Presentation
{
    class Program
    {
        static void Main(string[] args)
        {
            // Check if Python dependencies are installed
            Console.WriteLine("Checking Python dependencies...");
            var exporter = new AstExporter();
            string dependencyStatus = exporter.CheckAndInstallDependencies(true);
            Console.WriteLine(dependencyStatus);
            
            var testInputDir = Directory.EnumerateFiles("TestInputs");

            foreach (var testInputFile in testInputDir)
            {
                if (testInputFile.Contains(".pixil"))
                {
                    string input = File.ReadAllText(testInputFile);
                    Console.WriteLine("\n\n{0}", input);

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

                        // Execute using the Python interpreter
                        Console.WriteLine("\nExecuting using Python interpreter...");
                        try
                        {
                            string result = exporter.ExecuteAst(ast);
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

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}