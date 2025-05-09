using GraphixLang.Lexer;

namespace GraphixLang.Parser;

public class SyntaxError : Exception
{
    public SyntaxError(string message) : base(message) { }
}

public class Parser
{
    private readonly List<Token> _tokens;
    private int _position;
    private Token CurrentToken => _position < _tokens.Count ? _tokens[_position] : null;

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
        _position = 0;
    }

    public ProgramNode Parse()
    {
        var program = new ProgramNode();
        
        while (_position < _tokens.Count && CurrentToken.Type != TokenType.EOF)
        {
            var block = ParseBlock();
            if (block != null)
            {
                program.Blocks.Add(block);
            }
            else
            {
                throw new SyntaxError($"Expected a block at line {CurrentToken.Line}, column {CurrentToken.Column}");
            }
        }
        
        return program;
    }

    private BlockNode ParseBlock()
    {
        if (CurrentToken.Type != TokenType.OPEN_BLOCK)
        {
            return null;
        }
        
        Consume(TokenType.OPEN_BLOCK);
        
        var block = new BlockNode();
        
        while (_position < _tokens.Count && CurrentToken.Type != TokenType.CLOSE_BLOCK)
        {
            var statement = ParseStatement();
            if (statement != null)
            {
                block.Statements.Add(statement);
            }
            else
            {
                throw new SyntaxError($"Expected a statement at line {CurrentToken.Line}, column {CurrentToken.Column}");
            }
        }
        
        Consume(TokenType.CLOSE_BLOCK);
        
        return block;
    }

    private ASTNode ParseStatement()
    {
        switch (CurrentToken.Type)
        {
            case TokenType.TYPE_BATCH:
                return ParseBatchDeclaration();
            case TokenType.FOREACH:
                return ParseForEachStatement();
            case TokenType.TYPE_INT:
            case TokenType.TYPE_DBL:
            case TokenType.TYPE_STR:
            case TokenType.TYPE_BOOL:
            case TokenType.TYPE_IMG:
                return ParseVariableDeclaration();
            case TokenType.VAR_IDENTIFIER:
                return ParseAssignmentStatement();
            case TokenType.IF:
                return ParseIfStatement();
            case TokenType.SET:
                return ParseSetStatement();
            case TokenType.ROTATE:
                return ParseRotateStatement();
            case TokenType.CROP:
                return ParseCropStatement();
            case TokenType.ORIENTATION:
                return ParseOrientationStatement();
            default:
                throw new SyntaxError($"Unexpected token {CurrentToken.Type} at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
    }

    private BatchDeclarationNode ParseBatchDeclaration()
    {
        Consume(TokenType.TYPE_BATCH);
        
        if (CurrentToken.Type != TokenType.VAR_IDENTIFIER || !CurrentToken.Value.StartsWith("#"))
        {
            throw new SyntaxError($"Expected a batch identifier at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        string identifier = CurrentToken.Value;
        Consume(TokenType.VAR_IDENTIFIER);
        
        Consume(TokenType.ASSIGN);
        
        if (CurrentToken.Type != TokenType.STR_VALUE)
        {
            throw new SyntaxError($"Expected a string value at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        string path = CurrentToken.Value.Trim('"');
        Consume(TokenType.STR_VALUE);
        
        Consume(TokenType.EOL);
        
        return new BatchDeclarationNode
        {
            Identifier = identifier,
            Path = path
        };
    }

    private ForEachNode ParseForEachStatement()
    {
        Consume(TokenType.FOREACH);
        Consume(TokenType.TYPE_IMG);
        
        if (CurrentToken.Type != TokenType.VAR_IDENTIFIER || !CurrentToken.Value.StartsWith("$"))
        {
            throw new SyntaxError($"Expected a variable identifier at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        string varIdentifier = CurrentToken.Value;
        Consume(TokenType.VAR_IDENTIFIER);
        
        Consume(TokenType.IN);
        
        if (CurrentToken.Type != TokenType.VAR_IDENTIFIER || !CurrentToken.Value.StartsWith("#"))
        {
            throw new SyntaxError($"Expected a batch identifier at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        string batchIdentifier = CurrentToken.Value;
        Consume(TokenType.VAR_IDENTIFIER);
        
        var body = ParseBlock();
        
        return new ForEachNode
        {
            VarIdentifier = varIdentifier,
            BatchIdentifier = batchIdentifier,
            Body = body
        };
    }

    private VariableDeclarationNode ParseVariableDeclaration()
    {
        TokenType type = CurrentToken.Type;
        Consume();
        
        if (CurrentToken.Type != TokenType.VAR_IDENTIFIER || !CurrentToken.Value.StartsWith("$"))
        {
            throw new SyntaxError($"Expected a variable identifier at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        string identifier = CurrentToken.Value;
        Consume(TokenType.VAR_IDENTIFIER);
        
        ExpressionNode initializer = null;
        if (CurrentToken.Type == TokenType.ASSIGN)
        {
            Consume(TokenType.ASSIGN);
            initializer = ParseExpression();
        }
        
        Consume(TokenType.EOL);
        
        return new VariableDeclarationNode
        {
            Type = type,
            Identifier = identifier,
            Initializer = initializer
        };
    }

    private AssignmentNode ParseAssignmentStatement()
    {
        string identifier = CurrentToken.Value;
        Consume(TokenType.VAR_IDENTIFIER);
        
        Consume(TokenType.ASSIGN);
        
        ExpressionNode value = ParseExpression();
        
        Consume(TokenType.EOL);
        
        return new AssignmentNode
        {
            Identifier = identifier,
            Value = value
        };
    }

    private IfNode ParseIfStatement()
    {
        Consume(TokenType.IF);
        
        ExpressionNode condition = ParseExpression();
        
        BlockNode thenBranch = ParseBlock();
        
        var ifNode = new IfNode
        {
            Condition = condition,
            ThenBranch = thenBranch
        };
        
        // Parse optional ELIF branches
        while (_position < _tokens.Count && CurrentToken.Type == TokenType.ELIF)
        {
            Consume(TokenType.ELIF);
            
            ExpressionNode elifCondition = ParseExpression();
            BlockNode elifBody = ParseBlock();
            
            ifNode.ElifBranches.Add(new ElifBranchNode
            {
                Condition = elifCondition,
                Body = elifBody
            });
        }
        
        // Parse optional ELSE branch
        if (_position < _tokens.Count && CurrentToken.Type == TokenType.ELSE)
        {
            Consume(TokenType.ELSE);
            ifNode.ElseBranch = ParseBlock();
        }
        
        return ifNode;
    }

    private SetFilterNode ParseSetStatement()
    {
        Consume(TokenType.SET);
        
        if (CurrentToken.Type != TokenType.VAR_IDENTIFIER || !CurrentToken.Value.StartsWith("$"))
        {
            throw new SyntaxError($"Expected a variable identifier at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        string imageIdentifier = CurrentToken.Value;
        Consume(TokenType.VAR_IDENTIFIER);
        
        // Check that the next token is a valid filter type
        if (CurrentToken.Type != TokenType.SHARPEN && 
            CurrentToken.Type != TokenType.NEGATIVE && 
            CurrentToken.Type != TokenType.BW && 
            CurrentToken.Type != TokenType.SEPIA)
        {
            throw new SyntaxError($"Expected a filter type at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        TokenType filterType = CurrentToken.Type;
        Consume();
        
        Consume(TokenType.EOL);
        
        return new SetFilterNode
        {
            ImageIdentifier = imageIdentifier,
            FilterType = filterType
        };
    }

    private RotateNode ParseRotateStatement()
    {
        Consume(TokenType.ROTATE);
        
        if (CurrentToken.Type != TokenType.VAR_IDENTIFIER || !CurrentToken.Value.StartsWith("$"))
        {
            throw new SyntaxError($"Expected a variable identifier at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        string imageIdentifier = CurrentToken.Value;
        Consume(TokenType.VAR_IDENTIFIER);
        
        // Check that the next token is a valid direction
        if (CurrentToken.Type != TokenType.RIGHT && CurrentToken.Type != TokenType.LEFT)
        {
            throw new SyntaxError($"Expected a direction (RIGHT or LEFT) at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        TokenType direction = CurrentToken.Type;
        Consume();
        
        Consume(TokenType.EOL);
        
        return new RotateNode
        {
            ImageIdentifier = imageIdentifier,
            Direction = direction
        };
    }

    private CropNode ParseCropStatement()
    {
        Consume(TokenType.CROP);
        
        if (CurrentToken.Type != TokenType.VAR_IDENTIFIER || !CurrentToken.Value.StartsWith("$"))
        {
            throw new SyntaxError($"Expected a variable identifier at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        string imageIdentifier = CurrentToken.Value;
        Consume(TokenType.VAR_IDENTIFIER);
        
        Consume(TokenType.OPEN_P);
        
        ExpressionNode width = ParseExpression();
        
        Consume(TokenType.COMMA);
        
        ExpressionNode height = ParseExpression();
        
        Consume(TokenType.CLOSE_P);
        
        Consume(TokenType.EOL);
        
        return new CropNode
        {
            ImageIdentifier = imageIdentifier,
            Width = width,
            Height = height
        };
    }

    private OrientationNode ParseOrientationStatement()
    {
        Consume(TokenType.ORIENTATION);
        
        if (CurrentToken.Type != TokenType.VAR_IDENTIFIER || !CurrentToken.Value.StartsWith("$"))
        {
            throw new SyntaxError($"Expected a variable identifier at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        string imageIdentifier = CurrentToken.Value;
        Consume(TokenType.VAR_IDENTIFIER);
        
        // Check that the next token is a valid orientation type
        if (CurrentToken.Type != TokenType.LANDSCAPE && CurrentToken.Type != TokenType.PORTRAIT)
        {
            throw new SyntaxError($"Expected an orientation type (LANDSCAPE or PORTRAIT) at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        TokenType orientationType = CurrentToken.Type;
        Consume();
        
        Consume(TokenType.EOL);
        
        return new OrientationNode
        {
            ImageIdentifier = imageIdentifier,
            OrientationType = orientationType
        };
    }

    private ExpressionNode ParseExpression()
    {
        return ParseComparisonExpression();
    }

    private ExpressionNode ParseComparisonExpression()
    {
        ExpressionNode left = ParseAdditiveExpression();
        
        while (IsComparisonOperator(CurrentToken.Type))
        {
            TokenType op = CurrentToken.Type;
            Consume();
            
            ExpressionNode right = ParseAdditiveExpression();
            
            left = new BinaryExpressionNode
            {
                Left = left,
                Operator = op,
                Right = right
            };
        }
        
        return left;
    }

    private ExpressionNode ParseAdditiveExpression()
    {
        ExpressionNode left = ParseMultiplicativeExpression();
        
        while (CurrentToken.Type == TokenType.PLUS || CurrentToken.Type == TokenType.MINUS)
        {
            TokenType op = CurrentToken.Type;
            Consume();
            
            ExpressionNode right = ParseMultiplicativeExpression();
            
            left = new BinaryExpressionNode
            {
                Left = left,
                Operator = op,
                Right = right
            };
        }
        
        return left;
    }

    private ExpressionNode ParseMultiplicativeExpression()
    {
        ExpressionNode left = ParsePrimaryExpression();
        
        while (CurrentToken.Type == TokenType.MULTIPLY || CurrentToken.Type == TokenType.DIVIDE)
        {
            TokenType op = CurrentToken.Type;
            Consume();
            
            ExpressionNode right = ParsePrimaryExpression();
            
            left = new BinaryExpressionNode
            {
                Left = left,
                Operator = op,
                Right = right
            };
        }
        
        return left;
    }

    private ExpressionNode ParsePrimaryExpression()
    {
        switch (CurrentToken.Type)
        {
            case TokenType.INT_VALUE:
            case TokenType.DBL_VALUE:
            case TokenType.STR_VALUE:
            case TokenType.BOOL_VALUE:
            case TokenType.PXLS_VALUE:
                return ParseLiteral();
                
            case TokenType.VAR_IDENTIFIER:
                string identifier = CurrentToken.Value;
                Consume(TokenType.VAR_IDENTIFIER);
                return new VariableReferenceNode { Identifier = identifier };
                
            case TokenType.OPEN_P:
                Consume(TokenType.OPEN_P);
                ExpressionNode expr = ParseExpression();
                Consume(TokenType.CLOSE_P);
                return expr;
                
            case TokenType.METADATA:
                return ParseMetadataExpression();
                
            default:
                throw new SyntaxError($"Unexpected token {CurrentToken.Type} at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
    }

    private LiteralNode ParseLiteral()
    {
        TokenType type = CurrentToken.Type;
        string value = CurrentToken.Value;
        Consume();
        
        object parsedValue;
        
        switch (type)
        {
            case TokenType.INT_VALUE:
                parsedValue = int.Parse(value);
                break;
            case TokenType.DBL_VALUE:
                parsedValue = double.Parse(value);
                break;
            case TokenType.STR_VALUE:
                parsedValue = value.Trim('"');
                break;
            case TokenType.BOOL_VALUE:
                parsedValue = value.ToUpper() == "TRUE";
                break;
            case TokenType.PXLS_VALUE:
                parsedValue = int.Parse(value.TrimEnd('p'));
                break;
            default:
                throw new SyntaxError($"Unexpected literal type {type} at line {CurrentToken?.Line}, column {CurrentToken?.Column}");
        }
        
        return new LiteralNode
        {
            Type = type,
            Value = parsedValue
        };
    }

    private MetadataNode ParseMetadataExpression()
    {
        Consume(TokenType.METADATA);
        
        if (CurrentToken.Type != TokenType.VAR_IDENTIFIER || !CurrentToken.Value.StartsWith("$"))
        {
            throw new SyntaxError($"Expected a variable identifier at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        string imageIdentifier = CurrentToken.Value;
        Consume(TokenType.VAR_IDENTIFIER);
        
        // Check that the next token is a valid metadata type
        if (CurrentToken.Type != TokenType.FWIDTH && 
            CurrentToken.Type != TokenType.FHEIGHT && 
            CurrentToken.Type != TokenType.FNAME && 
            CurrentToken.Type != TokenType.FSIZE)
        {
            throw new SyntaxError($"Expected a metadata type at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        TokenType metadataType = CurrentToken.Type;
        Consume();
        
        return new MetadataNode
        {
            ImageIdentifier = imageIdentifier,
            MetadataType = metadataType
        };
    }

    private bool IsComparisonOperator(TokenType type)
    {
        return type == TokenType.EQUAL || 
                type == TokenType.NOT_EQUAL || 
                type == TokenType.GREATER || 
                type == TokenType.GREATER_EQUAL || 
                type == TokenType.SMALLER || 
                type == TokenType.SMALLER_EQUAL;
    }

    private void Consume(TokenType expected)
    {
        if (CurrentToken == null)
        {
            throw new SyntaxError($"Unexpected end of input, expected {expected}");
        }
        
        if (CurrentToken.Type != expected)
        {
            throw new SyntaxError($"Expected {expected}, but got {CurrentToken.Type} at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        _position++;
    }

    private void Consume()
    {
        _position++;
    }
}
