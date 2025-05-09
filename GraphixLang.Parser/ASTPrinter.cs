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
    
    public void Visit(ForEachNode node)
    {
        AppendIndent();
        _sb.AppendLine($"ForEach: {node.VarIdentifier} in {node.BatchIdentifier}");
        
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
