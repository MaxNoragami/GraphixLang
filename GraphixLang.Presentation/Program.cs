using GraphixLang.Lexer;
using GraphixLang.Parser;

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
            Parser parser = new Parser(tokens);
            ProgramNode ast = parser.Parse();

            // Print the AST
            ASTPrinter printer = new ASTPrinter();
            string astString = printer.Print(ast);
            Console.WriteLine("\nAbstract Syntax Tree:");
            Console.WriteLine(astString);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError: {ex.Message}");
        }
    }
}