namespace GraphixLang.Lexer;

public class Token
{
    public TokenType Type { get; private set; }
    public string Value { get; private set; }
    public int Line { get; private set; }
    public int Column { get; private set; }

    public Token(TokenType type, string val, int line, int column)
    {
        Type = type;
        Value = val;
        Line = line;
        Column = column;
    }
}
