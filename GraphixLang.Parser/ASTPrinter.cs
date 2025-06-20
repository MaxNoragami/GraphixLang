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

    public void Visit(ResizeNode node)
    {
        AppendIndent();
        if (node.IsAspectRatioMode)
        {
            _sb.AppendLine($"Resize: {node.ImageIdentifier} to {GetAspectRatioName(node.AspectRatio)}");
        }
        else
        {
            _sb.Append($"Resize: {node.ImageIdentifier} to (");
            node.Width.Accept(this);
            _sb.Append(", ");
            node.Height.Accept(this);
            _sb.Append(")");
            
            if (!node.MaintainAspectRatio)
            {
                _sb.Append(" RATIOFALSE");
            }
            
            _sb.AppendLine();
        }
    }

    private string GetAspectRatioName(TokenType ratio)
    {
        switch (ratio)
        {
            case TokenType.RATIO_16_9: return "16:9";
            case TokenType.RATIO_9_16: return "9:16";
            case TokenType.RATIO_4_3: return "4:3";
            case TokenType.RATIO_3_4: return "3:4";
            case TokenType.RATIO_1_1: return "1:1";
            case TokenType.RATIO_2_3: return "2:3";
            case TokenType.RATIO_3_2: return "3:2";
            case TokenType.RATIO_2_1: return "2:1";
            case TokenType.RATIO_1_2: return "1:2";
            case TokenType.RATIO_16_10: return "16:10";
            case TokenType.RATIO_10_16: return "10:16";
            case TokenType.RATIO_21_9: return "21:9";
            case TokenType.RATIO_9_21: return "9:21";
            default: return ratio.ToString();
        }
    }

    public void Visit(CompressNode node)
    {
        AppendIndent();
        _sb.AppendLine($"Compress: {node.ImageIdentifier} quality: {node.Quality}");
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

    public void Visit(WebOptimizeNode node)
    {
        AppendIndent();
        if (node.IsLossless)
        {
            _sb.AppendLine($"WebOptimize: {node.ImageIdentifier} LOSSLESS");
        }
        else
        {
            _sb.AppendLine($"WebOptimize: {node.ImageIdentifier} LOSSY quality: {node.Quality}");
        }
    }

    public void Visit(QuantizeNode node)
    {
        AppendIndent();
        _sb.AppendLine($"Quantize: {node.ImageIdentifier} colors: {node.Colors}");
    }

    public void Visit(BrightnessNode node)
    {
        AppendIndent();
        _sb.AppendLine($"Brightness: {node.ImageIdentifier} {node.Value}");
    }

    public void Visit(ContrastNode node)
    {
        AppendIndent();
        _sb.AppendLine($"Contrast: {node.ImageIdentifier} {node.Value}");
    }

    public void Visit(OpacityNode node)
    {
        AppendIndent();
        _sb.AppendLine($"Opacity: {node.ImageIdentifier} {node.Value}");
    }

    public void Visit(NoiseNode node)
    {
        AppendIndent();
        _sb.AppendLine($"Noise: {node.ImageIdentifier} {node.Value}");
    }

    public void Visit(BlurNode node)
    {
        AppendIndent();
        _sb.AppendLine($"Blur: {node.ImageIdentifier} {node.Value}");
    }

    public void Visit(PixelateNode node)
    {
        AppendIndent();
        _sb.AppendLine($"Pixelate: {node.ImageIdentifier} {node.Value}");
    }
}
