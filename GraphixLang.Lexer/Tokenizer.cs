using System.Text;
using System.Text.RegularExpressions;

namespace GraphixLang.Lexer;

public class Tokenizer
{
    private readonly string _input;
    private int _position;
    private int _line;
    private int _column;

    // Regex Patterns
    private static readonly Regex NumberRegex = new Regex(@"(\d+\.\d+|\d+p|\d+)", RegexOptions.Compiled);

    public Tokenizer(string input)
    {
        _input = input;
        _position = 0;
        _line = 1;
        _column = 1;
    }

    public List<Token> Tokenize()
    {
        List<Token> tokens = new List<Token>();

        while(_position < _input.Length)
        {
            char current = _input[_position];

            // Skip whitespaces
            if (char.IsWhiteSpace(current))
            {
                // Checks for line feed encounters
                if (current == '\n')
                {
                    _line++;
                    _column = 1;
                }
                else if (current == '\r')
                {
                    // Handle '\r\n' Windows and carriage return old Mac
                    if (_position + 1 < _input.Length && _input[_position + 1] == '\n')
                    {
                        _position++; // Skip the '\n' in '\r\n'
                    }
                    _line++;
                    _column = 1;
                }
                else
                {
                    // In case of spaces or other stuff
                    _column++;
                }
                _position++;
                continue;
            }

            Token? token = MatchToken();
            if(token != null)
            {
                tokens.Add(token);
            }
            else
            {
                Console.WriteLine(string.Format("Unexpected char '{0}', at line {1}, column {2}.", current, _line, _column));
                break;
            }
        }
        
        tokens.Add(new Token(TokenType.EOF, "", _line, _column));
        return tokens;
    }

    private Token? MatchToken()
    {
        char current = _input[_position];

        // Special case for aspect ratios - check this first
        if (char.IsDigit(current))
        {
            Token? ratioToken = TryMatchAspectRatio();
            if (ratioToken != null)
                return ratioToken;
        }

        // Tokenizing symbols
        if(current == ')') return Advance(TokenType.CLOSE_P, ")");
        if(current == '(') return Advance(TokenType.OPEN_P, "(");
        if(current == ',') return Advance(TokenType.COMMA, ",");
        if(current == '}') return Advance(TokenType.CLOSE_BLOCK, "}");
        if(current == '{') return Advance(TokenType.OPEN_BLOCK, "{");
        if(current == ';') return Advance(TokenType.EOL, ";");
        if(current == '/') return Advance(TokenType.DIVIDE, "/");
        if(current == '*') return Advance(TokenType.MULTIPLY, "*");
        if(current == '+') return Advance(TokenType.PLUS, "+");
        if(current == '-') return Advance(TokenType.MINUS, "-");

        if (current == '=')
        {
            if (Peek() == '=') return AdvanceTwo(TokenType.EQUAL, "==");
            return Advance(TokenType.ASSIGN, "=");
        }

        if (current == '>')
        {
            if (Peek() == '=') return AdvanceTwo(TokenType.GREATER_EQUAL, ">=");
            return Advance(TokenType.GREATER, ">");
        }

        if (current == '<')
        {
            if (Peek() == '=') return AdvanceTwo(TokenType.SMALLER_EQUAL, "<=");
            return Advance(TokenType.SMALLER, "<");
        }

        if (current == '!')
        {
            if (Peek() == '=') return AdvanceTwo(TokenType.NOT_EQUAL, "!=");
            return null;
        }
        
        // Tokenizing reserved words
        if (char.IsLetter(current) || current == '$' || current == '#')
        {
            string word = ReadWord();
            return CreateWordToken(word);
        }

        // Tokenizing numerical values
        if (char.IsDigit(current))
        {
            return MatchNumber();
        }

        // Tokenizing strings
        if (current == '"')
        {
            return ReadString();
        }

        if (current == '~')
        {
            if (_position + 1 < _input.Length)
            {
                char colorType = _input[_position + 1];
                if (colorType == 'H')
                {
                    return ReadHexColor();
                }
                else if (colorType == 'R')
                {
                    return ReadRGBColor();
                }
            }
        }
        
        // No match :(
        return null;
    }

    private Token? TryMatchAspectRatio()
    {
        // Store the current position to restore if not a match
        int startPosition = _position;
        int startColumn = _column;
        
        // Read potential first number
        StringBuilder sb = new StringBuilder();
        while (_position < _input.Length && char.IsDigit(_input[_position]))
        {
            sb.Append(_input[_position]);
            _position++;
            _column++;
        }
        
        // Check for colon
        if (_position < _input.Length && _input[_position] == ':')
        {
            sb.Append(_input[_position]);
            _position++;
            _column++;
            
            // Read second number
            while (_position < _input.Length && char.IsDigit(_input[_position]))
            {
                sb.Append(_input[_position]);
                _position++;
                _column++;
            }
            
            string ratio = sb.ToString();
            
            // Check if it's one of our supported ratios
            switch (ratio)
            {
                case "16:9": return new Token(TokenType.RATIO_16_9, ratio, _line, startColumn);
                case "9:16": return new Token(TokenType.RATIO_9_16, ratio, _line, startColumn);
                case "4:3": return new Token(TokenType.RATIO_4_3, ratio, _line, startColumn);
                case "3:4": return new Token(TokenType.RATIO_3_4, ratio, _line, startColumn);
                case "1:1": return new Token(TokenType.RATIO_1_1, ratio, _line, startColumn);
                case "2:3": return new Token(TokenType.RATIO_2_3, ratio, _line, startColumn);
                case "3:2": return new Token(TokenType.RATIO_3_2, ratio, _line, startColumn);
                case "2:1": return new Token(TokenType.RATIO_2_1, ratio, _line, startColumn);
                case "1:2": return new Token(TokenType.RATIO_1_2, ratio, _line, startColumn);
                case "16:10": return new Token(TokenType.RATIO_16_10, ratio, _line, startColumn);
                case "10:16": return new Token(TokenType.RATIO_10_16, ratio, _line, startColumn);
                case "21:9": return new Token(TokenType.RATIO_21_9, ratio, _line, startColumn);
                case "9:21": return new Token(TokenType.RATIO_9_21, ratio, _line, startColumn);
            }
        }
        
        // No match, restore position and column
        _position = startPosition;
        _column = startColumn;
        return null;
    }

    private Token Advance(TokenType type, string val)
    {
        _position++;
        _column++;
        return new Token(type, val , _line, _column - 1);
    }

    private Token AdvanceTwo(TokenType type, string val)
    {
        _position += 2;
        _column += 2;
        return new Token(type, val , _line, _column - 2);
    }
    
    // For having a look at the next char
    private char Peek()
    {
        return _position + 1 < _input.Length ? _input[_position + 1] : '\0';
    }

    // Reads the entire word
    private string ReadWord()
    {
        // To keep track where the word started
        int start = _position;

        // Special handling for aspect ratios which can contain ':'
        bool foundColon = false;
        
        while(_position < _input.Length && 
            (char.IsLetterOrDigit(_input[_position]) || 
            _input[_position] == '$' || 
            _input[_position] == '#' ||
            // Allow colon for aspect ratios like "16:9"
            (_input[_position] == ':' && !foundColon && _position > start && char.IsDigit(_input[_position - 1]))))
        {
            if (_input[_position] == ':')
                foundColon = true;
                
            _position++;
            _column++;
        }
        
        return _input.Substring(start, _position - start);
    }

    // Tokenizes the read word
    private Token CreateWordToken(string word)
    {
        // Checks if reserved word
        switch(word.ToUpper())
        {
            case "INT": return new Token(TokenType.TYPE_INT, word, _line, _column - word.Length);
            case "PIXEL": return new Token(TokenType.TYPE_PXLS, word, _line, _column - word.Length);
            case "DOUBLE": return new Token(TokenType.TYPE_DBL, word, _line, _column - word.Length);
            case "STRING": return new Token(TokenType.TYPE_STR, word, _line, _column - word.Length);
            case "BOOL": return new Token(TokenType.TYPE_BOOL, word, _line, _column - word.Length);
            case "IMG": return new Token(TokenType.TYPE_IMG, word, _line, _column - word.Length);
            case "BATCH": return new Token(TokenType.TYPE_BATCH, word, _line, _column - word.Length);
            case "TRUE": return new Token(TokenType.BOOL_VALUE, word, _line, _column - word.Length);
            case "FALSE": return new Token(TokenType.BOOL_VALUE, word, _line, _column - word.Length);
            case "IN": return new Token(TokenType.IN, word, _line, _column - word.Length);
            case "FOREACH": return new Token(TokenType.FOREACH, word, _line, _column - word.Length);
            case "FSIZE": return new Token(TokenType.FSIZE, word, _line, _column - word.Length);
            case "FNAME": return new Token(TokenType.FNAME, word, _line, _column - word.Length);
            case "FHEIGHT": return new Token(TokenType.FHEIGHT, word, _line, _column - word.Length);
            case "FWIDTH": return new Token(TokenType.FWIDTH, word, _line, _column - word.Length);
            case "METADATA": return new Token(TokenType.METADATA, word, _line, _column - word.Length);
            case "RIGHT": return new Token(TokenType.RIGHT, word, _line, _column - word.Length);
            case "LEFT": return new Token(TokenType.LEFT, word, _line, _column - word.Length);
            case "SET": return new Token(TokenType.SET, word, _line, _column - word.Length);
            case "ELIF": return new Token(TokenType.ELIF, word, _line, _column - word.Length);
            case "ELSE": return new Token(TokenType.ELSE, word, _line, _column - word.Length);
            case "IF": return new Token(TokenType.IF, word, _line, _column - word.Length);
            case "SHARPEN": return new Token(TokenType.SHARPEN, word, _line, _column - word.Length);
            case "NEGATIVE": return new Token(TokenType.NEGATIVE, word, _line, _column - word.Length);
            case "BW": return new Token(TokenType.BW, word, _line, _column - word.Length);
            case "SEPIA": return new Token(TokenType.SEPIA, word, _line, _column - word.Length);
            case "CROP": return new Token(TokenType.CROP, word, _line, _column - word.Length);
            case "ORIENTATION": return new Token(TokenType.ORIENTATION, word, _line, _column - word.Length);
            case "LANDSCAPE": return new Token(TokenType.LANDSCAPE, word, _line, _column - word.Length);
            case "PORTRAIT": return new Token(TokenType.PORTRAIT, word, _line, _column - word.Length);
            case "ROTATE": return new Token(TokenType.ROTATE, word, _line, _column - word.Length);
            case "HUE": return new Token(TokenType.HUE, word, _line, _column - word.Length);
            case "WATERMARK": return new Token(TokenType.WATERMARK, word, _line, _column - word.Length);
            case "STRIP": return new Token(TokenType.STRIP, word, _line, _column - word.Length);
            case "ADD": return new Token(TokenType.ADD, word, _line, _column - word.Length);
            case "ALL": return new Token(TokenType.ALL, word, _line, _column - word.Length);
            case "TAGS": return new Token(TokenType.TAGS, word, _line, _column - word.Length);
            case "TITLE": return new Token(TokenType.TITLE, word, _line, _column - word.Length);
            case "COPYRIGHT": return new Token(TokenType.COPYRIGHT, word, _line, _column - word.Length);
            case "GPS": return new Token(TokenType.GPS, word, _line, _column - word.Length);
            case "CAMERA": return new Token(TokenType.CAMERA, word, _line, _column - word.Length);
            case "ADVANCE": return new Token(TokenType.ADVANCE, word, _line, _column - word.Length);
            case "ORIGIN": return new Token(TokenType.ORIGIN, word, _line, _column - word.Length);
            case "DESCRIPTION": return new Token(TokenType.DESCRIPTION, word, _line, _column - word.Length);
            case "RENAME": return new Token(TokenType.RENAME, word, _line, _column - word.Length);
            case "COUNTER": return new Token(TokenType.COUNTER, word, _line, _column - word.Length);
            case "EXPORT": return new Token(TokenType.EXPORT, word, _line, _column - word.Length);
            case "TO": return new Token(TokenType.TO, word, _line, _column - word.Length);
            case "OGKEEP": return new Token(TokenType.OGKEEP, word, _line, _column - word.Length);
            case "OGDELETE": return new Token(TokenType.OGDELETE, word, _line, _column - word.Length);
            case "CONVERT": return new Token(TokenType.CONVERT, word, _line, _column - word.Length);
            case "PNG": return new Token(TokenType.PNG, word, _line, _column - word.Length);
            case "JPG": return new Token(TokenType.JPG, word, _line, _column - word.Length);
            case "JPEG": return new Token(TokenType.JPEG, word, _line, _column - word.Length);
            case "WEBP": return new Token(TokenType.WEBP, word, _line, _column - word.Length);
            case "TIFF": return new Token(TokenType.TIFF, word, _line, _column - word.Length);
            case "BMP": return new Token(TokenType.BMP, word, _line, _column - word.Length);
            case "RESIZE": return new Token(TokenType.RESIZE, word, _line, _column - word.Length);
            case "RATIOFALSE": return new Token(TokenType.RATIOFALSE, word, _line, _column - word.Length);
            case "COMPRESS": return new Token(TokenType.COMPRESS, word, _line, _column - word.Length);
            case "BRIGHTNESS": return new Token(TokenType.BRIGHTNESS, word, _line, _column - word.Length);
            case "CONTRAST": return new Token(TokenType.CONTRAST, word, _line, _column - word.Length);
            case "OPACITY": return new Token(TokenType.OPACITY, word, _line, _column - word.Length);
            case "NOISE": return new Token(TokenType.NOISE, word, _line, _column - word.Length);
            case "BLUR": return new Token(TokenType.BLUR, word, _line, _column - word.Length);
            case "PIXELATE": return new Token(TokenType.PIXELATE, word, _line, _column - word.Length);
        }

        // Checks if identifiers
        if (word.First() == '$') return new Token(TokenType.VAR_IDENTIFIER, word, _line, _column - word.Length);
        if (word.First() == '#') return new Token(TokenType.BATCH_IDENTIFIER, word, _line, _column - word.Length);

        throw new Exception(string.Format("Unexpected word '{0}', at line {1}, column {2}.", word, _line, _column - word.Length));
    }

    // Tokenizes the numbers
    private Token MatchNumber()
    {
        Match match = NumberRegex.Match(_input, _position);
        if (match.Success && match.Index == _position) // Ensure the match starts at the current position
        {
            string value = match.Value;
            _position += value.Length; // Advance the position
            _column += value.Length;   // Update the column

            // Determine the token type based on the matched value
            if (value.Last() == 'p') return new Token(TokenType.PXLS_VALUE, value, _line, _column - value.Length);
            if (value.Contains(".")) return new Token(TokenType.DBL_VALUE, value, _line, _column - value.Length);
            return new Token(TokenType.INT_VALUE, value, _line, _column - value.Length);
        }

        throw new Exception(string.Format("Unexpected num value '{0}', at line {1}, column {2}.", match, _line, _column - match.Length));
    }

    // Strings tokenization
    private Token ReadString()
    {
        _position++; // Skip opening quote
        _column++;
        int start = _position;
        while (_position < _input.Length && _input[_position] != '"')
        {
            _position++;
            _column++;
        }
        string val = _input.Substring(start, _position - start);
        _position++; // Skip closing quote
        _column++;
        return new Token(TokenType.STR_VALUE, $"\"{val}\"", _line, _column - val.Length - 2);
    }

    private Token ReadHexColor()
    {
        // Starting position is at '~'
        int start = _position;
        _position += 2; // Skip '~H'
        _column += 2;
        
        // Count the hex digits
        int digitCount = 0;
        while (_position < _input.Length && 
            ((_input[_position] >= '0' && _input[_position] <= '9') || 
            (_input[_position] >= 'A' && _input[_position] <= 'F') || 
            (_input[_position] >= 'a' && _input[_position] <= 'f')))
        {
            _position++;
            _column++;
            digitCount++;
        }
        
        // Enforce exactly 6 or 8 hex digits
        if (digitCount != 6 && digitCount != 8)
        {
            throw new Exception($"Hex color must have exactly 6 digits (RRGGBB) or 8 digits (RRGGBBAA) at line {_line}, column {_column}");
        }
        
        // Expect closing '~'
        if (_position < _input.Length && _input[_position] == '~')
        {
            _position++;
            _column++;
            return new Token(TokenType.HEX_COLOR, _input.Substring(start, _position - start), _line, _column - (_position - start));
        }
        
        throw new Exception($"Expected closing '~' for hex color at line {_line}, column {_column}");
    }

    private Token ReadRGBColor()
    {
        // Starting position is at '~'
        int start = _position;
        _position += 2; // Skip '~R'
        _column += 2;
        
        // Count the digits
        int digitCount = 0;
        while (_position < _input.Length && _input[_position] >= '0' && _input[_position] <= '9')
        {
            _position++;
            _column++;
            digitCount++;
        }
        
        // Enforce exactly 9 or 12 digits
        if (digitCount != 9 && digitCount != 12)
        {
            throw new Exception($"RGB color must have exactly 9 digits (RRRGGGBBB) or 12 digits (RRRGGGBBBAAA) at line {_line}, column {_column}");
        }
        
        // Expect closing '~'
        if (_position < _input.Length && _input[_position] == '~')
        {
            _position++;
            _column++;
            return new Token(TokenType.RGB_COLOR, _input.Substring(start, _position - start), _line, _column - (_position - start));
        }
        
        throw new Exception($"Expected closing '~' for RGB color at line {_line}, column {_column}");
    }
}
