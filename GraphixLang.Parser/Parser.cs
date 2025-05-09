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
    private Dictionary<string, TokenType> _variableTypes = new Dictionary<string, TokenType>();


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
                return ParseVariableDeclaration();
            case TokenType.TYPE_IMG:
                return ParseImageDeclaration();
            case TokenType.VAR_IDENTIFIER:
                return ParseAssignmentStatement();
            case TokenType.IF:
                return ParseIfStatement();
            case TokenType.RENAME:
                return ParseRenameStatement();
            case TokenType.EXPORT:
                return ParseExportStatement();
            case TokenType.CONVERT:
                return ParseConvertStatement();
            case TokenType.STRIP:
                if (_position + 1 < _tokens.Count && _tokens[_position + 1].Type == TokenType.METADATA)
                {
                    return ParseStripMetadataStatement();
                }
                throw new SyntaxError($"Unexpected token after STRIP at line {CurrentToken.Line}, column {CurrentToken.Column}");
            case TokenType.ADD:
                if (_position + 1 < _tokens.Count && _tokens[_position + 1].Type == TokenType.METADATA)
                {
                    return ParseAddMetadataStatement();
                }
                throw new SyntaxError($"Unexpected token after ADD at line {CurrentToken.Line}, column {CurrentToken.Column}"); 
            case TokenType.SET:
                // Need to look ahead to determine whether it's a filter or hue operation
                string varIdentifier = "";
                if (_position + 1 < _tokens.Count && _tokens[_position + 1].Type == TokenType.VAR_IDENTIFIER)
                {
                    varIdentifier = _tokens[_position + 1].Value;
                    
                    if (_position + 2 < _tokens.Count)
                    {
                        // Check the token after the variable identifier to determine operation type
                        if (_tokens[_position + 2].Type == TokenType.HUE)
                        {
                            return ParseHueStatement();
                        }
                        else
                        {
                            return ParseSetStatement();
                        }
                    }
                }
                return ParseSetStatement(); // Default to filter if we can't determine
            case TokenType.ROTATE:
                return ParseRotateStatement();
            case TokenType.CROP:
                return ParseCropStatement();
            case TokenType.WATERMARK:
                return ParseWatermarkStatement();
            case TokenType.ORIENTATION:
                return ParseOrientationStatement();
            default:
                throw new SyntaxError($"Unexpected token {CurrentToken.Type} at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
    }

    private BatchDeclarationNode ParseBatchDeclaration()
    {
        Consume(TokenType.TYPE_BATCH);
        
        if (CurrentToken.Type != TokenType.BATCH_IDENTIFIER)
        {
            throw new SyntaxError($"Expected a batch identifier at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        string batchIdentifier = CurrentToken.Value;
        Consume(TokenType.BATCH_IDENTIFIER);
        
        Consume(TokenType.ASSIGN);
        
        ExpressionNode expression = ParseBatchExpression();
        
        Consume(TokenType.EOL);
        
        return new BatchDeclarationNode
        {
            Identifier = batchIdentifier,
            Expression = expression  // Updated to store the full expression
        };
    }

    private ImageDeclarationNode ParseImageDeclaration()
    {
        Consume(TokenType.TYPE_IMG);
        
        if (CurrentToken.Type != TokenType.VAR_IDENTIFIER || !CurrentToken.Value.StartsWith("$"))
        {
            throw new SyntaxError($"Expected a variable identifier at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        string identifier = CurrentToken.Value;
        Consume(TokenType.VAR_IDENTIFIER);

        _variableTypes[identifier] = TokenType.TYPE_IMG;
        
        Consume(TokenType.ASSIGN);
        
        if (CurrentToken.Type != TokenType.STR_VALUE)
        {
            throw new SyntaxError($"Expected a string value at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        string path = CurrentToken.Value.Trim('"');
        Consume(TokenType.STR_VALUE);
        
        // Validate that the path ends with an acceptable image format
        string[] validExtensions = { ".png", ".jpg", ".jpeg", ".webp", ".tiff", ".bmp" };
        bool isValidImagePath = false;
        
        foreach (var ext in validExtensions)
        {
            if (path.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            {
                isValidImagePath = true;
                break;
            }
        }
        
        if (!isValidImagePath)
        {
            throw new SyntaxError($"Invalid image file format at line {CurrentToken.Line}, column {CurrentToken.Column}. Supported formats: PNG, JPG, JPEG, WEBP, TIFF, BMP");
        }
        
        Consume(TokenType.EOL);
        
        return new ImageDeclarationNode
        {
            Identifier = identifier,
            Path = path
        };
    }

    private ExpressionNode ParseBatchExpression()
    {
        ExpressionNode left = ParseBatchTerm();
        
        while (CurrentToken.Type == TokenType.PLUS)
        {
            TokenType op = CurrentToken.Type;
            Consume();
            
            ExpressionNode right = ParseBatchTerm();
            
            left = new BinaryExpressionNode
            {
                Left = left,
                Operator = op,
                Right = right
            };
        }
        
        return left;
    }

    private StripMetadataNode ParseStripMetadataStatement()
    {
        Consume(TokenType.STRIP);
        Consume(TokenType.METADATA);
        
        if (CurrentToken.Type != TokenType.VAR_IDENTIFIER || !CurrentToken.Value.StartsWith("$"))
        {
            throw new SyntaxError($"Expected a variable identifier at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        string imageIdentifier = CurrentToken.Value;
        Consume(TokenType.VAR_IDENTIFIER);
        
        var node = new StripMetadataNode
        {
            ImageIdentifier = imageIdentifier
        };
        
        // Check if ALL or specific metadata types
        if (CurrentToken.Type == TokenType.ALL)
        {
            node.StripAll = true;
            Consume(TokenType.ALL);
        }
        else
        {
            // Parse metadata list
            node.StripAll = false;

            do
            {
                if (!IsStripableMetadataType(CurrentToken.Type))
                {
                    if (CurrentToken.Type == TokenType.FNAME || 
                        CurrentToken.Type == TokenType.FSIZE || 
                        CurrentToken.Type == TokenType.FWIDTH || 
                        CurrentToken.Type == TokenType.FHEIGHT)
                    {
                        throw new SyntaxError($"Cannot strip essential metadata: {CurrentToken.Type} at line {CurrentToken.Line}, column {CurrentToken.Column}");
                    }
                    
                    throw new SyntaxError($"Expected a strippable metadata type at line {CurrentToken.Line}, column {CurrentToken.Column}");
                }
                
                node.MetadataTypes.Add(CurrentToken.Type);
                Consume();
                
                if (CurrentToken.Type != TokenType.COMMA)
                    break;
                    
                Consume(TokenType.COMMA);
                
            } while (true);
        }
        
        Consume(TokenType.EOL);
        
        return node;
    }
    private AddMetadataNode ParseAddMetadataStatement()
    {
        Consume(TokenType.ADD);
        Consume(TokenType.METADATA);
        
        if (CurrentToken.Type != TokenType.VAR_IDENTIFIER || !CurrentToken.Value.StartsWith("$"))
        {
            throw new SyntaxError($"Expected a variable identifier at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        string imageIdentifier = CurrentToken.Value;
        Consume(TokenType.VAR_IDENTIFIER);
        
        // Restrict ADD to only specific metadata types
        if (CurrentToken.Type != TokenType.TAGS && 
            CurrentToken.Type != TokenType.TITLE && 
            CurrentToken.Type != TokenType.COPYRIGHT &&
            CurrentToken.Type != TokenType.DESCRIPTION)
        {
            throw new SyntaxError($"Expected TAGS, TITLE, COPYRIGHT or DESCRIPTION at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        TokenType metadataType = CurrentToken.Type;
        Consume();
        
        if (CurrentToken.Type != TokenType.STR_VALUE)
        {
            throw new SyntaxError($"Expected a string value at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        string value = CurrentToken.Value.Trim('"');
        Consume(TokenType.STR_VALUE);
        
        Consume(TokenType.EOL);
        
        return new AddMetadataNode
        {
            ImageIdentifier = imageIdentifier,
            MetadataType = metadataType,
            Value = value
        };
    }

    private bool IsStripableMetadataType(TokenType type)
    {
        // Cannot strip basic metadata fields
        if (type == TokenType.FNAME || 
            type == TokenType.FSIZE || 
            type == TokenType.FWIDTH || 
            type == TokenType.FHEIGHT)
        {
            return false;
        }
        
        return type == TokenType.GPS ||
            type == TokenType.CAMERA ||
            type == TokenType.ADVANCE ||
            type == TokenType.ORIGIN ||
            type == TokenType.DESCRIPTION ||
            type == TokenType.TAGS ||
            type == TokenType.TITLE ||
            type == TokenType.COPYRIGHT;
    }

    private RenameNode ParseRenameStatement()
    {
        Consume(TokenType.RENAME);
        
        if (CurrentToken.Type != TokenType.VAR_IDENTIFIER || !CurrentToken.Value.StartsWith("$"))
        {
            throw new SyntaxError($"Expected a variable identifier at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        string imageIdentifier = CurrentToken.Value;
        Consume(TokenType.VAR_IDENTIFIER);
        
        var node = new RenameNode
        {
            ImageIdentifier = imageIdentifier
        };
        
        // Parse the rename expression (a sequence of terms connected by '+')
        node.Terms.Add(ParseRenameTerm());
        
        while (CurrentToken.Type == TokenType.PLUS)
        {
            Consume(TokenType.PLUS);
            node.Terms.Add(ParseRenameTerm());
        }
        
        // Validate that at least one term contains METADATA FNAME
        bool hasOriginalFilename = false;
        foreach (var term in node.Terms)
        {
            if (term.Type == RenameTermType.METADATA && 
                term.MetadataValue.MetadataType == TokenType.FNAME)
            {
                hasOriginalFilename = true;
                break;
            }
        }
        
        if (!hasOriginalFilename)
        {
            throw new SyntaxError($"RENAME operation must include the original filename (METADATA $photo FNAME) at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        Consume(TokenType.EOL);
        
        return node;
    }

    private RenameTermNode ParseRenameTerm()
    {
        var termNode = new RenameTermNode();
        
        switch (CurrentToken.Type)
        {
            case TokenType.STR_VALUE:
                termNode.Type = RenameTermType.STRING;
                termNode.StringValue = CurrentToken.Value.Trim('"');
                Consume(TokenType.STR_VALUE);
                break;
                
            case TokenType.COUNTER:
                termNode.Type = RenameTermType.COUNTER;
                Consume(TokenType.COUNTER);
                break;
                
            case TokenType.METADATA:
                termNode.Type = RenameTermType.METADATA;
                MetadataNode metadata = ParseMetadataExpression();
                
                // Ensure only FNAME is used in RENAME operations
                if (metadata.MetadataType != TokenType.FNAME)
                {
                    throw new SyntaxError($"Only FNAME metadata can be used in RENAME operations, but got {metadata.MetadataType} at line {CurrentToken.Line}, column {CurrentToken.Column}");
                }
                
                termNode.MetadataValue = metadata;
                break;
                
            default:
                throw new SyntaxError($"Expected a string, COUNTER or METADATA at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        return termNode;
    }

    private ExpressionNode ParseBatchTerm()
    {
        if (CurrentToken.Type == TokenType.STR_VALUE)
        {
            return ParseLiteral();
        }
        else if (CurrentToken.Type == TokenType.BATCH_IDENTIFIER)
        {
            string identifier = CurrentToken.Value;
            Consume(TokenType.BATCH_IDENTIFIER);
            return new VariableReferenceNode { Identifier = identifier };
        }
        else
        {
            throw new SyntaxError($"Expected a string value or batch identifier at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
    }

    private HueNode ParseHueStatement()
    {
        Consume(TokenType.SET);
    
        if (CurrentToken.Type != TokenType.VAR_IDENTIFIER || !CurrentToken.Value.StartsWith("$"))
        {
            throw new SyntaxError($"Expected a variable identifier at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        string imageIdentifier = CurrentToken.Value;
        Consume(TokenType.VAR_IDENTIFIER);
        
        // Type check: ensure imageIdentifier is an IMG
        EnsureImageType(imageIdentifier, "HUE");
        
        Consume(TokenType.HUE);
        
        // Check that the next token is a valid integer value
        if (CurrentToken.Type != TokenType.INT_VALUE)
        {
            throw new SyntaxError($"Expected an integer value at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        // Parse the hue value
        int hueValue = int.Parse(CurrentToken.Value);
        Consume(TokenType.INT_VALUE);
        
        // Validate that the hue value is in the range 0-360
        if (hueValue < 0 || hueValue > 360)
        {
            throw new SyntaxError($"Hue value must be between 0 and 360 at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        Consume(TokenType.EOL);
        
        return new HueNode
        {
            ImageIdentifier = imageIdentifier,
            HueValue = hueValue
        };
    }

    private ASTNode ParseWatermarkStatement()
    {
        Consume(TokenType.WATERMARK);
        
        if (CurrentToken.Type != TokenType.VAR_IDENTIFIER || !CurrentToken.Value.StartsWith("$"))
        {
            throw new SyntaxError($"Expected a variable identifier at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        string imageIdentifier = CurrentToken.Value;
        Consume(TokenType.VAR_IDENTIFIER);
        
        // Type check: ensure the target image identifier is an IMG
        EnsureImageType(imageIdentifier, "WATERMARK");
        
        // Check if it's a text watermark or image watermark
        if (CurrentToken.Type == TokenType.STR_VALUE)
        {
            // Text watermark
            string text = CurrentToken.Value.Trim('"');
            Consume(TokenType.STR_VALUE);
            
            bool isHexColor = false;
            string colorValue = "";
            
            if (CurrentToken.Type == TokenType.HEX_COLOR)
            {
                isHexColor = true;
                colorValue = CurrentToken.Value;
                Consume(TokenType.HEX_COLOR);
            }
            else if (CurrentToken.Type == TokenType.RGB_COLOR)
            {
                isHexColor = false;
                colorValue = CurrentToken.Value;
                Consume(TokenType.RGB_COLOR);
            }
            else
            {
                throw new SyntaxError($"Expected a color value at line {CurrentToken.Line}, column {CurrentToken.Column}");
            }
            
            Consume(TokenType.EOL);
            
            return new WatermarkNode
            {
                ImageIdentifier = imageIdentifier,
                Text = text,
                ColorValue = colorValue,
                IsHexColor = isHexColor
            };
        }
        else if (CurrentToken.Type == TokenType.VAR_IDENTIFIER)
        {
            // Image watermark
            string watermarkImageIdentifier = CurrentToken.Value;
            Consume(TokenType.VAR_IDENTIFIER);
            
            // Type check: ensure the watermark source is also an IMG
            EnsureImageType(watermarkImageIdentifier, "WATERMARK source");
            
            if (CurrentToken.Type != TokenType.INT_VALUE)
            {
                throw new SyntaxError($"Expected an integer value (0-255) for transparency at line {CurrentToken.Line}, column {CurrentToken.Column}");
            }
            
            int transparency = int.Parse(CurrentToken.Value);
            Consume(TokenType.INT_VALUE);
            
            // Validate transparency is within 0-255
            if (transparency < 0 || transparency > 255)
            {
                throw new SyntaxError($"Transparency value must be between 0 and 255 at line {CurrentToken.Line}, column {CurrentToken.Column}");
            }
            
            Consume(TokenType.EOL);
            
            return new ImageWatermarkNode
            {
                ImageIdentifier = imageIdentifier,
                WatermarkImageIdentifier = watermarkImageIdentifier,
                Transparency = transparency
            };
        }
        else
        {
            throw new SyntaxError($"Expected a string value or variable identifier at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
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

        _variableTypes[varIdentifier] = TokenType.TYPE_IMG;
        
        Consume(TokenType.IN);
        
        if (CurrentToken.Type != TokenType.BATCH_IDENTIFIER)
        {
            throw new SyntaxError($"Expected a batch identifier at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        string batchIdentifier = CurrentToken.Value;
        Consume(TokenType.BATCH_IDENTIFIER);
        
        // Make EXPORT TO mandatory
        if (CurrentToken.Type != TokenType.EXPORT)
        {
            throw new SyntaxError($"Expected EXPORT after batch identifier at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        Consume(TokenType.EXPORT);
        
        if (CurrentToken.Type != TokenType.TO)
        {
            throw new SyntaxError($"Expected TO after EXPORT at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        Consume(TokenType.TO);
        
        if (CurrentToken.Type != TokenType.STR_VALUE)
        {
            throw new SyntaxError($"Expected a string value for export path at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        string exportPath = CurrentToken.Value.Trim('"');
        Consume(TokenType.STR_VALUE);
        
        var body = ParseBlock();
        
        // Check for EXPORT statements inside the body
        CheckNoExportInBlock(body);
        
        return new ForEachNode
        {
            VarIdentifier = varIdentifier,
            BatchIdentifier = batchIdentifier,
            ExportPath = exportPath,
            Body = body
        };
    }

    // Keep the helper method to check for EXPORT statements
    private void CheckNoExportInBlock(BlockNode block)
    {
        foreach (var statement in block.Statements)
        {
            // Check if the statement is an ExportNode
            if (statement is ExportNode)
            {
                throw new SyntaxError($"EXPORT statements are not allowed inside FOREACH blocks. The export destination is already specified in the FOREACH statement.");
            }
            
            // Recursively check nested blocks
            if (statement is IfNode ifNode)
            {
                CheckNoExportInBlock(ifNode.ThenBranch);
                
                if (ifNode.ElseBranch != null)
                {
                    CheckNoExportInBlock(ifNode.ElseBranch);
                }
                
                foreach (var elifBranch in ifNode.ElifBranches)
                {
                    CheckNoExportInBlock(elifBranch.Body);
                }
            }
        }
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
        
        // Store variable type for later type checking
        _variableTypes[identifier] = type;
        
        ExpressionNode initializer = null;
        if (CurrentToken.Type == TokenType.ASSIGN)
        {
            Consume(TokenType.ASSIGN);
            
            // If we're assigning a metadata value, check type compatibility
            if (_position < _tokens.Count && _tokens[_position].Type == TokenType.METADATA)
            {
                Token metadataToken = _tokens[_position + 2]; // Skip METADATA and identifier to get the metadata type
                
                // Check type compatibility
                if ((metadataToken.Type == TokenType.FSIZE && type != TokenType.TYPE_DBL) ||
                    ((metadataToken.Type == TokenType.FWIDTH || metadataToken.Type == TokenType.FHEIGHT) && type != TokenType.TYPE_INT) ||
                    (metadataToken.Type == TokenType.FNAME && type != TokenType.TYPE_STR))
                {
                    throw new SyntaxError($"Type mismatch: {metadataToken.Type} requires {GetRequiredType(metadataToken.Type)} but variable is {type} at line {CurrentToken.Line}, column {CurrentToken.Column}");
                }
            }
            
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

    // Helper method to get required type
    private string GetRequiredType(TokenType metadataType)
    {
        if (metadataType == TokenType.FSIZE)
            return "DOUBLE";
        if (metadataType == TokenType.FWIDTH || metadataType == TokenType.FHEIGHT)
            return "INT";
        return "STRING";
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
        
        // Type check: ensure imageIdentifier is an IMG
        EnsureImageType(imageIdentifier, "SET");
        
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
        
        // Type check: ensure imageIdentifier is an IMG
        EnsureImageType(imageIdentifier, "ROTATE");
        
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
        
        // Type check: ensure imageIdentifier is an IMG
        EnsureImageType(imageIdentifier, "CROP");
        
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

        EnsureImageType(imageIdentifier, "ORIENTATION");
        
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
            
            // Use the new method to create a binary expression with type checking
            left = CreateBinaryExpression(left, op, right);
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

    private ExportNode ParseExportStatement()
    {
        Consume(TokenType.EXPORT);
        
        if (CurrentToken.Type != TokenType.VAR_IDENTIFIER || !CurrentToken.Value.StartsWith("$"))
        {
            throw new SyntaxError($"Expected a variable identifier at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        string imageIdentifier = CurrentToken.Value;
        Consume(TokenType.VAR_IDENTIFIER);
        
        Consume(TokenType.TO);
        
        if (CurrentToken.Type != TokenType.STR_VALUE)
        {
            throw new SyntaxError($"Expected a string value at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        string destinationPath = CurrentToken.Value.Trim('"');
        Consume(TokenType.STR_VALUE);
        
        bool keepOriginal;
        
        if (CurrentToken.Type == TokenType.OGKEEP)
        {
            keepOriginal = true;
            Consume(TokenType.OGKEEP);
        }
        else if (CurrentToken.Type == TokenType.OGDELETE)
        {
            keepOriginal = false;
            Consume(TokenType.OGDELETE);
        }
        else
        {
            throw new SyntaxError($"Expected OGKEEP or OGDELETE at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        Consume(TokenType.EOL);
        
        return new ExportNode
        {
            ImageIdentifier = imageIdentifier,
            DestinationPath = destinationPath,
            KeepOriginal = keepOriginal
        };
    }

    private ConvertNode ParseConvertStatement()
    {
        Consume(TokenType.CONVERT);
        
        if (CurrentToken.Type != TokenType.VAR_IDENTIFIER || !CurrentToken.Value.StartsWith("$"))
        {
            throw new SyntaxError($"Expected a variable identifier at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        string imageIdentifier = CurrentToken.Value;
        Consume(TokenType.VAR_IDENTIFIER);
        
        // Type check: ensure imageIdentifier is an IMG
        EnsureImageType(imageIdentifier, "CONVERT");
        
        Consume(TokenType.TO);
        
        // Check for a valid format type
        if (!IsImageFormatToken(CurrentToken.Type))
        {
            throw new SyntaxError($"Expected a valid image format (PNG, JPG, JPEG, WEBP, TIFF, BMP) at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        TokenType targetFormat = CurrentToken.Type;
        Consume(); // Consume the format token
        
        Consume(TokenType.EOL);
        
        return new ConvertNode
        {
            ImageIdentifier = imageIdentifier,
            TargetFormat = targetFormat
        };
    }

    private bool IsImageFormatToken(TokenType type)
    {
        return type == TokenType.PNG ||
            type == TokenType.JPG ||
            type == TokenType.JPEG ||
            type == TokenType.WEBP ||
            type == TokenType.TIFF ||
            type == TokenType.BMP;
    }

    private void Consume()
    {
        _position++;
    }

    private void EnsureImageType(string identifier, string operation)
    {
        if (!_variableTypes.TryGetValue(identifier, out TokenType type))
        {
            throw new SyntaxError($"{operation} operation requires an image variable, but {identifier} is undefined at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
        
        if (type != TokenType.TYPE_IMG)
        {
            throw new SyntaxError($"{operation} operation requires an image variable, but {identifier} is {GetTypeName(type)} at line {CurrentToken.Line}, column {CurrentToken.Column}");
        }
    }

    // Helper method to get type name for error messages
    private string GetTypeName(TokenType type)
    {
        switch (type)
        {
            case TokenType.TYPE_INT: return "INT";
            case TokenType.TYPE_DBL: return "DOUBLE";
            case TokenType.TYPE_STR: return "STRING";
            case TokenType.TYPE_BOOL: return "BOOL";
            case TokenType.TYPE_PXLS: return "PIXEL";
            case TokenType.TYPE_IMG: return "IMG";
            case TokenType.TYPE_BATCH: return "BATCH";
            default: return type.ToString();
        }
    }

    private TokenType GetExpressionType(ExpressionNode expr)
    {
        TokenType NormalizeType(TokenType type)
        {
            switch (type)
            {
                case TokenType.INT_VALUE: return TokenType.TYPE_INT;
                case TokenType.DBL_VALUE: return TokenType.TYPE_DBL;
                case TokenType.STR_VALUE: return TokenType.TYPE_STR;
                case TokenType.BOOL_VALUE: return TokenType.TYPE_BOOL;
                case TokenType.PXLS_VALUE: return TokenType.TYPE_PXLS;
                default: return type;
            }
        }

        if (expr is LiteralNode literal)
        {
            return NormalizeType(literal.Type);
        }
        else if (expr is VariableReferenceNode varRef)
        {
            if (_variableTypes.TryGetValue(varRef.Identifier, out TokenType type))
            {
                return type;
            }
            throw new SyntaxError($"Unknown variable: {varRef.Identifier}");
        }
        else if (expr is MetadataNode metaNode)
        {
            switch (metaNode.MetadataType)
            {
                case TokenType.FWIDTH:
                case TokenType.FHEIGHT:
                    return TokenType.TYPE_INT;
                case TokenType.FSIZE:
                    return TokenType.TYPE_DBL;
                case TokenType.FNAME:
                    return TokenType.TYPE_STR;
                default:
                    return TokenType.TYPE_STR;
            }
        }
        
        // For binary expressions, the result type depends on the operation
        // This is simplified - full type checking would be more complex
        return TokenType.TYPE_INT;
    }

    private bool AreCompatibleTypes(TokenType type1, TokenType type2)
    {
        // Map literal types to their corresponding variable types
        TokenType NormalizeType(TokenType type)
        {
            switch (type)
            {
                case TokenType.INT_VALUE: return TokenType.TYPE_INT;
                case TokenType.DBL_VALUE: return TokenType.TYPE_DBL;
                case TokenType.STR_VALUE: return TokenType.TYPE_STR;
                case TokenType.BOOL_VALUE: return TokenType.TYPE_BOOL;
                case TokenType.PXLS_VALUE: return TokenType.TYPE_PXLS;
                default: return type;
            }
        }

        // Normalize both types
        TokenType normalizedType1 = NormalizeType(type1);
        TokenType normalizedType2 = NormalizeType(type2);
        
        // Same normalized type is always compatible
        if (normalizedType1 == normalizedType2)
            return true;
        
        // Numeric types are compatible with each other
        bool isType1Numeric = normalizedType1 == TokenType.TYPE_INT || normalizedType1 == TokenType.TYPE_DBL || normalizedType1 == TokenType.TYPE_PXLS;
        bool isType2Numeric = normalizedType2 == TokenType.TYPE_INT || normalizedType2 == TokenType.TYPE_DBL || normalizedType2 == TokenType.TYPE_PXLS;
        
        if (isType1Numeric && isType2Numeric)
            return true;
        
        // Other combinations are incompatible
        return false;
    }

    private BinaryExpressionNode CreateBinaryExpression(ExpressionNode left, TokenType op, ExpressionNode right)
    {
        // For comparison operators, verify that the types are compatible
        if (IsComparisonOperator(op))
        {
            TokenType leftType = GetExpressionType(left);
            TokenType rightType = GetExpressionType(right);
            
            if (!AreCompatibleTypes(leftType, rightType))
            {
                throw new SyntaxError($"Cannot compare {GetTypeName(leftType)} with {GetTypeName(rightType)} at line {CurrentToken?.Line}, column {CurrentToken?.Column}");
            }
        }
        
        return new BinaryExpressionNode
        {
            Left = left,
            Operator = op,
            Right = right
        };
    }
}
