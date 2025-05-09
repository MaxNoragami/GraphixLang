using GraphixLang.Lexer;

namespace GraphixLang.Parser;

public abstract class ASTNode
{
    public virtual void Accept(IASTVisitor visitor)
    {
        // Base implementation does nothing
    }
}

public interface IASTVisitor
{
    void Visit(ProgramNode node);
    void Visit(BlockNode node);
    void Visit(BatchDeclarationNode node);
    void Visit(ForEachNode node);
    void Visit(VariableDeclarationNode node);
    void Visit(AssignmentNode node);
    void Visit(IfNode node);
    void Visit(ElifBranchNode node);
    void Visit(SetFilterNode node);
    void Visit(RotateNode node);
    void Visit(CropNode node);
    void Visit(BinaryExpressionNode node);
    void Visit(LiteralNode node);
    void Visit(VariableReferenceNode node);
    void Visit(OrientationNode node);
    void Visit(MetadataNode node);
    void Visit(BatchExpressionNode node);
}

public class ProgramNode : ASTNode
{
    public List<BlockNode> Blocks { get; } = new List<BlockNode>();
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class BlockNode : ASTNode
{
    public List<ASTNode> Statements { get; } = new List<ASTNode>();
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class BatchDeclarationNode : ASTNode
{
    public string Identifier { get; set; }
    public ExpressionNode Expression { get; set; }  // Changed from Path (string) to Expression
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class BatchExpressionNode : ExpressionNode
{
    public List<ExpressionNode> Terms { get; } = new List<ExpressionNode>();
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class ForEachNode : ASTNode
{
    public string VarIdentifier { get; set; }
    public string BatchIdentifier { get; set; }
    public BlockNode Body { get; set; }
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class VariableDeclarationNode : ASTNode
{
    public TokenType Type { get; set; }
    public string Identifier { get; set; }
    public ExpressionNode Initializer { get; set; }
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class AssignmentNode : ASTNode
{
    public string Identifier { get; set; }
    public ExpressionNode Value { get; set; }
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class IfNode : ASTNode
{
    public ExpressionNode Condition { get; set; }
    public BlockNode ThenBranch { get; set; }
    public List<ElifBranchNode> ElifBranches { get; } = new List<ElifBranchNode>();
    public BlockNode ElseBranch { get; set; }
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class ElifBranchNode : ASTNode
{
    public ExpressionNode Condition { get; set; }
    public BlockNode Body { get; set; }
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class SetFilterNode : ASTNode
{
    public string ImageIdentifier { get; set; }
    public TokenType FilterType { get; set; }
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class RotateNode : ASTNode
{
    public string ImageIdentifier { get; set; }
    public TokenType Direction { get; set; }
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class CropNode : ASTNode
{
    public string ImageIdentifier { get; set; }
    public ExpressionNode Width { get; set; }
    public ExpressionNode Height { get; set; }
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class OrientationNode : ASTNode
{
    public string ImageIdentifier { get; set; }
    public TokenType OrientationType { get; set; }
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public abstract class ExpressionNode : ASTNode
{
    // Base class for expressions
}

public class BinaryExpressionNode : ExpressionNode
{
    public ExpressionNode Left { get; set; }
    public TokenType Operator { get; set; }
    public ExpressionNode Right { get; set; }
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class LiteralNode : ExpressionNode
{
    public TokenType Type { get; set; }
    public object Value { get; set; }
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class VariableReferenceNode : ExpressionNode
{
    public string Identifier { get; set; }
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class MetadataNode : ExpressionNode
{
    public string ImageIdentifier { get; set; }
    public TokenType MetadataType { get; set; }
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}