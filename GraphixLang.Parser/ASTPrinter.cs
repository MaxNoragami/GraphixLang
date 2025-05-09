using GraphixLang.Lexer;
using System.Text;

namespace GraphixLang.Parser;

public class ASTPrinter : IASTVisitor
{
    private StringBuilder _sb = new StringBuilder();
    private int _indent = 0;
    
    public string Print(ASTNode node)
    {
        _sb.Clear();
        _indent = 0;
        
        node.Accept(this);
        
        return _sb.ToString();
    }
    
    private void AppendIndent()
    {
        _sb.Append(new string(' ', _indent * 2));
    }
    
    private void IncreaseIndent()
    {
        _indent++;
    }
    
    private void DecreaseIndent()
    {
        if (_indent > 0)
        {
            _indent--;
        }
    }
    
    public void Visit(ProgramNode node)
    {
        AppendIndent();
        _sb.AppendLine("Program:");
        
        IncreaseIndent();
        
        foreach (var block in node.Blocks)
        {
            block.Accept(this);
        }
        
        DecreaseIndent();
    }
    
    public void Visit(BlockNode node)
    {
        AppendIndent();
        _sb.AppendLine("Block:");
        
        IncreaseIndent();
        
        foreach (var statement in node.Statements)
        {
            statement.Accept(this);
        }
        
        DecreaseIndent();
    }
    
    public void Visit(BatchDeclarationNode node)
    {
        AppendIndent();
        _sb.Append($"BatchDeclaration: {node.Identifier} = ");
        
        // Instead of directly accessing node.Path, visit the Expression
        node.Expression.Accept(this);
        
        _sb.AppendLine();
    }

    public void Visit(HueNode node)
    {
        AppendIndent();
        _sb.AppendLine($"Hue: {node.ImageIdentifier} {node.HueValue}");
    }
    
    public void Visit(WatermarkNode node)
    {
        AppendIndent();
        _sb.AppendLine($"Watermark: {node.ImageIdentifier} \"{node.Text}\" {node.ColorValue}");
    }

    public void Visit(ForEachNode node)
    {
        AppendIndent();
        _sb.AppendLine($"ForEach: {node.VarIdentifier} in {node.BatchIdentifier} EXPORT TO \"{node.ExportPath}\"");
        
        IncreaseIndent();
        node.Body.Accept(this);
        DecreaseIndent();
    }

    
    public void Visit(VariableDeclarationNode node)
    {
        AppendIndent();
        _sb.Append($"VariableDeclaration: {GetTypeName(node.Type)} {node.Identifier}");
        
        if (node.Initializer != null)
        {
            _sb.Append(" = ");
            node.Initializer.Accept(this);
        }
        
        _sb.AppendLine();
    }
    
    public void Visit(ImageDeclarationNode node)
    {
        AppendIndent();
        _sb.AppendLine($"ImageDeclaration: {node.Identifier} = \"{node.Path}\"");
    }

    public void Visit(ImageWatermarkNode node)
    {
        AppendIndent();
        _sb.AppendLine($"ImageWatermark: {node.ImageIdentifier} with {node.WatermarkImageIdentifier}, transparency: {node.Transparency}");
    }

    public void Visit(StripMetadataNode node)
    {
        AppendIndent();
        _sb.Append($"StripMetadata: {node.ImageIdentifier} ");
        
        if (node.StripAll)
        {
            _sb.Append("ALL");
        }
        else
        {
            _sb.Append(string.Join(", ", node.MetadataTypes.Select(GetMetadataTypeName)));
        }
        
        _sb.AppendLine();
    }

    public void Visit(AddMetadataNode node)
    {
        AppendIndent();
        _sb.AppendLine($"AddMetadata: {node.ImageIdentifier} {GetMetadataTypeName(node.MetadataType)} \"{node.Value}\"");
    }

    public void Visit(AssignmentNode node)
    {
        AppendIndent();
        _sb.Append($"Assignment: {node.Identifier} = ");
        node.Value.Accept(this);
        _sb.AppendLine();
    }
    
    public void Visit(IfNode node)
    {
        AppendIndent();
        _sb.Append("If: ");
        node.Condition.Accept(this);
        _sb.AppendLine();
        
        IncreaseIndent();
        node.ThenBranch.Accept(this);
        DecreaseIndent();
        
        foreach (var elifBranch in node.ElifBranches)
        {
            elifBranch.Accept(this);
        }
        
        if (node.ElseBranch != null)
        {
            AppendIndent();
            _sb.AppendLine("Else:");
            
            IncreaseIndent();
            node.ElseBranch.Accept(this);
            DecreaseIndent();
        }
    }
    
    public void Visit(ElifBranchNode node)
    {
        AppendIndent();
        _sb.Append("Elif: ");
        node.Condition.Accept(this);
        _sb.AppendLine();
        
        IncreaseIndent();
        node.Body.Accept(this);
        DecreaseIndent();
    }
    
    public void Visit(SetFilterNode node)
    {
        AppendIndent();
        _sb.AppendLine($"SetFilter: {node.ImageIdentifier} {GetFilterName(node.FilterType)}");
    }
    
    public void Visit(RotateNode node)
    {
        AppendIndent();
        _sb.AppendLine($"Rotate: {node.ImageIdentifier} {GetDirectionName(node.Direction)}");
    }
    
    public void Visit(CropNode node)
    {
        AppendIndent();
        _sb.Append($"Crop: {node.ImageIdentifier} (");
        node.Width.Accept(this);
        _sb.Append(", ");
        node.Height.Accept(this);
        _sb.AppendLine(")");
    }

    public void Visit(OrientationNode node)
    {
        AppendIndent();
        _sb.AppendLine($"Orientation: {node.ImageIdentifier} {GetOrientationTypeName(node.OrientationType)}");
    }

    
    public void Visit(BinaryExpressionNode node)
    {
        _sb.Append("(");
        node.Left.Accept(this);
        _sb.Append($" {GetOperatorSymbol(node.Operator)} ");
        node.Right.Accept(this);
        _sb.Append(")");
    }
    
    public void Visit(LiteralNode node)
    {
        switch (node.Type)
        {
            case TokenType.INT_VALUE:
                _sb.Append(node.Value);
                break;
            case TokenType.DBL_VALUE:
                _sb.Append(node.Value);
                break;
            case TokenType.STR_VALUE:
                _sb.Append($"\"{node.Value}\"");
                break;
            case TokenType.BOOL_VALUE:
                _sb.Append((bool)node.Value ? "true" : "false");
                break;
            case TokenType.PXLS_VALUE:
                _sb.Append($"{node.Value}p");
                break;
            default:
                _sb.Append(node.Value);
                break;
        }
    }
    
    public void Visit(BatchExpressionNode node)
    {
        for (int i = 0; i < node.Terms.Count; i++)
        {
            node.Terms[i].Accept(this);
            if (i < node.Terms.Count - 1)
            {
                _sb.Append(" + ");
            }
        }
    }

    public void Visit(VariableReferenceNode node)
    {
        _sb.Append(node.Identifier);
    }
    
    public void Visit(MetadataNode node)
    {
        _sb.Append($"METADATA {node.ImageIdentifier} {GetMetadataTypeName(node.MetadataType)}");
    }
    
    private string GetTypeName(TokenType type)
    {
        switch (type)
        {
            case TokenType.TYPE_INT: return "INT";
            case TokenType.TYPE_DBL: return "DOUBLE";
            case TokenType.TYPE_STR: return "STRING";
            case TokenType.TYPE_BOOL: return "BOOL";
            case TokenType.TYPE_IMG: return "IMG";
            case TokenType.TYPE_BATCH: return "BATCH";
            default: return type.ToString();
        }
    }
    
    private string GetFilterName(TokenType type)
    {
        switch (type)
        {
            case TokenType.SHARPEN: return "SHARPEN";
            case TokenType.NEGATIVE: return "NEGATIVE";
            case TokenType.BW: return "BW";
            case TokenType.SEPIA: return "SEPIA";
            default: return type.ToString();
        }
    }
    
    private string GetDirectionName(TokenType type)
    {
        switch (type)
        {
            case TokenType.LEFT: return "LEFT";
            case TokenType.RIGHT: return "RIGHT";
            default: return type.ToString();
        }
    }
    
    private string GetMetadataTypeName(TokenType type)
    {
        switch (type)
        {
            case TokenType.FWIDTH: return "FWIDTH";
            case TokenType.FHEIGHT: return "FHEIGHT";
            case TokenType.FNAME: return "FNAME";
            case TokenType.FSIZE: return "FSIZE";
            case TokenType.GPS: return "GPS";
            case TokenType.CAMERA: return "CAMERA";
            case TokenType.ADVANCE: return "ADVANCE";
            case TokenType.ORIGIN: return "ORIGIN";
            case TokenType.DESCRIPTION: return "DESCRIPTION";
            case TokenType.TAGS: return "TAGS";
            case TokenType.TITLE: return "TITLE";
            case TokenType.COPYRIGHT: return "COPYRIGHT";
            default: return type.ToString();
        }
    }
    
    private string GetOperatorSymbol(TokenType type)
    {
        switch (type)
        {
            case TokenType.PLUS: return "+";
            case TokenType.MINUS: return "-";
            case TokenType.MULTIPLY: return "*";
            case TokenType.DIVIDE: return "/";
            case TokenType.EQUAL: return "==";
            case TokenType.NOT_EQUAL: return "!=";
            case TokenType.GREATER: return ">";
            case TokenType.GREATER_EQUAL: return ">=";
            case TokenType.SMALLER: return "<";
            case TokenType.SMALLER_EQUAL: return "<=";
            default: return type.ToString();
        }
    }

    public void Visit(RenameNode node)
    {
        AppendIndent();
        _sb.Append($"Rename: {node.ImageIdentifier} to ");
        
        for (int i = 0; i < node.Terms.Count; i++)
        {
            node.Terms[i].Accept(this);
            
            if (i < node.Terms.Count - 1)
            {
                _sb.Append(" + ");
            }
        }
        
        _sb.AppendLine();
    }

    public void Visit(RenameTermNode node)
    {
        switch (node.Type)
        {
            case RenameTermType.STRING:
                _sb.Append($"\"{node.StringValue}\"");
                break;
                
            case RenameTermType.COUNTER:
                _sb.Append("COUNTER");
                break;
                
            case RenameTermType.METADATA:
                node.MetadataValue.Accept(this);
                break;
        }
    }

    public void Visit(ExportNode node)
    {
        AppendIndent();
        _sb.AppendLine($"Export: {node.ImageIdentifier} to \"{node.DestinationPath}\" {(node.KeepOriginal ? "OGKEEP" : "OGDELETE")}");
    }

    public void Visit(ConvertNode node)
    {
        AppendIndent();
        _sb.AppendLine($"Convert: {node.ImageIdentifier} to {GetFormatName(node.TargetFormat)}");
    }

    private string GetFormatName(TokenType format)
    {
        switch (format)
        {
            case TokenType.PNG: return "PNG";
            case TokenType.JPG: return "JPG";
            case TokenType.JPEG: return "JPEG";
            case TokenType.WEBP: return "WEBP";
            case TokenType.TIFF: return "TIFF";
            case TokenType.BMP: return "BMP";
            default: return format.ToString();
        }
    }

    private string GetOrientationTypeName(TokenType type)
    {
        switch (type)
        {
            case TokenType.LANDSCAPE: return "LANDSCAPE";
            case TokenType.PORTRAIT: return "PORTRAIT";
            default: return type.ToString();
        }
    }
}
