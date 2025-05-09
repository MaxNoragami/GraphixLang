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
    void Visit(HueNode node);
    void Visit(WatermarkNode node);
    void Visit(ImageDeclarationNode node);
    void Visit(ImageWatermarkNode node);
    void Visit(StripMetadataNode node);
    void Visit(AddMetadataNode node);
    void Visit(RenameNode node);
    void Visit(RenameTermNode node);
    void Visit(ExportNode node);
    void Visit(ConvertNode node);
    void Visit(ResizeNode node);
    void Visit(QuantizeNode node);
    void Visit(CompressNode node);
    void Visit(BrightnessNode node);
    void Visit(ContrastNode node);
    void Visit(OpacityNode node);
    void Visit(NoiseNode node);
    void Visit(BlurNode node);
    void Visit(PixelateNode node);
    void Visit(WebOptimizeNode node);
}

public class ProgramNode : ASTNode
{
    public List<BlockNode> Blocks { get; } = new List<BlockNode>();
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class QuantizeNode : ASTNode
{
    public string ImageIdentifier { get; set; }
    public int Colors { get; set; }  // Number of colors (0-255)
    
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

public class WebOptimizeNode : ASTNode
{
    public string ImageIdentifier { get; set; }
    public bool IsLossless { get; set; }
    public int Quality { get; set; }  // Only used in LOSSY mode
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class BrightnessNode : ASTNode
{
    public string ImageIdentifier { get; set; }
    public int Value { get; set; }
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class ContrastNode : ASTNode
{
    public string ImageIdentifier { get; set; }
    public int Value { get; set; }
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class OpacityNode : ASTNode
{
    public string ImageIdentifier { get; set; }
    public int Value { get; set; }
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class NoiseNode : ASTNode
{
    public string ImageIdentifier { get; set; }
    public int Value { get; set; }
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class BlurNode : ASTNode
{
    public string ImageIdentifier { get; set; }
    public int Value { get; set; }
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class PixelateNode : ASTNode
{
    public string ImageIdentifier { get; set; }
    public int Value { get; set; }
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class WatermarkNode : ASTNode
{
    public string ImageIdentifier { get; set; }
    public string Text { get; set; }
    public string ColorValue { get; set; }
    public bool IsHexColor { get; set; }
    
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

public class ExportNode : ASTNode
{
    public string ImageIdentifier { get; set; }
    public string DestinationPath { get; set; }
    public bool KeepOriginal { get; set; }
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class ConvertNode : ASTNode
{
    public string ImageIdentifier { get; set; }
    public TokenType TargetFormat { get; set; }
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class ResizeNode : ASTNode
{
    public string ImageIdentifier { get; set; }
    public ExpressionNode Width { get; set; }     // Only used for resolution mode
    public ExpressionNode Height { get; set; }    // Only used for resolution mode
    public TokenType AspectRatio { get; set; }    // Only used for aspect ratio mode
    public bool MaintainAspectRatio { get; set; } // True by default, false if RATIOFALSE is specified
    public bool IsAspectRatioMode { get; set; }   // True if using aspect ratio, false if using resolution
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class CompressNode : ASTNode
{
    public string ImageIdentifier { get; set; }
    public int Quality { get; set; }  // 0-100
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class ImageDeclarationNode : ASTNode
{
    public string Identifier { get; set; }
    public string Path { get; set; }
    
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
    public string ExportPath { get; set; }  // Required export path
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

public class HueNode : ASTNode
{
    public string ImageIdentifier { get; set; }
    public int HueValue { get; set; }
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class StripMetadataNode : ASTNode
{
    public string ImageIdentifier { get; set; }
    public bool StripAll { get; set; }
    public List<TokenType> MetadataTypes { get; } = new List<TokenType>();
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}


public class AddMetadataNode : ASTNode
{
    public string ImageIdentifier { get; set; }
    public TokenType MetadataType { get; set; }
    public string Value { get; set; }
    
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

public class ImageWatermarkNode : ASTNode
{
    public string ImageIdentifier { get; set; }
    public string WatermarkImageIdentifier { get; set; }
    public int Transparency { get; set; }
    
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

public enum RenameTermType
{
    STRING,
    COUNTER,
    METADATA
}

public class RenameTermNode : ASTNode
{
    public RenameTermType Type { get; set; }
    public string StringValue { get; set; }  // For string literals
    public MetadataNode MetadataValue { get; set; }  // For metadata expressions
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}


public class RenameNode : ASTNode
{
    public string ImageIdentifier { get; set; }
    public List<RenameTermNode> Terms { get; } = new List<RenameTermNode>();
    
    public override void Accept(IASTVisitor visitor)
    {
        visitor.Visit(this);
    }
}