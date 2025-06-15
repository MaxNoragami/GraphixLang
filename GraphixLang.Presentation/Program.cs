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

                        // Execute using the C# interpreter
                        Console.WriteLine("\nExecuting using C# interpreter...");
                        try
                        {
                            var interpreter = new GraphixInterpreter();
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

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}