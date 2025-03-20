# Generated from Grammar.g4 by ANTLR 4.13.2
from antlr4 import *
if "." in __name__:
    from .GrammarParser import GrammarParser
else:
    from GrammarParser import GrammarParser

# This class defines a complete listener for a parse tree produced by GrammarParser.
class GrammarListener(ParseTreeListener):

    # Enter a parse tree produced by GrammarParser#start.
    def enterStart(self, ctx:GrammarParser.StartContext):
        pass

    # Exit a parse tree produced by GrammarParser#start.
    def exitStart(self, ctx:GrammarParser.StartContext):
        pass


    # Enter a parse tree produced by GrammarParser#statements.
    def enterStatements(self, ctx:GrammarParser.StatementsContext):
        pass

    # Exit a parse tree produced by GrammarParser#statements.
    def exitStatements(self, ctx:GrammarParser.StatementsContext):
        pass


    # Enter a parse tree produced by GrammarParser#statement.
    def enterStatement(self, ctx:GrammarParser.StatementContext):
        pass

    # Exit a parse tree produced by GrammarParser#statement.
    def exitStatement(self, ctx:GrammarParser.StatementContext):
        pass


    # Enter a parse tree produced by GrammarParser#simple_statement.
    def enterSimple_statement(self, ctx:GrammarParser.Simple_statementContext):
        pass

    # Exit a parse tree produced by GrammarParser#simple_statement.
    def exitSimple_statement(self, ctx:GrammarParser.Simple_statementContext):
        pass


    # Enter a parse tree produced by GrammarParser#compound_statement.
    def enterCompound_statement(self, ctx:GrammarParser.Compound_statementContext):
        pass

    # Exit a parse tree produced by GrammarParser#compound_statement.
    def exitCompound_statement(self, ctx:GrammarParser.Compound_statementContext):
        pass


    # Enter a parse tree produced by GrammarParser#assignment.
    def enterAssignment(self, ctx:GrammarParser.AssignmentContext):
        pass

    # Exit a parse tree produced by GrammarParser#assignment.
    def exitAssignment(self, ctx:GrammarParser.AssignmentContext):
        pass


    # Enter a parse tree produced by GrammarParser#import_statement.
    def enterImport_statement(self, ctx:GrammarParser.Import_statementContext):
        pass

    # Exit a parse tree produced by GrammarParser#import_statement.
    def exitImport_statement(self, ctx:GrammarParser.Import_statementContext):
        pass


    # Enter a parse tree produced by GrammarParser#export_statement.
    def enterExport_statement(self, ctx:GrammarParser.Export_statementContext):
        pass

    # Exit a parse tree produced by GrammarParser#export_statement.
    def exitExport_statement(self, ctx:GrammarParser.Export_statementContext):
        pass


    # Enter a parse tree produced by GrammarParser#conditional.
    def enterConditional(self, ctx:GrammarParser.ConditionalContext):
        pass

    # Exit a parse tree produced by GrammarParser#conditional.
    def exitConditional(self, ctx:GrammarParser.ConditionalContext):
        pass


    # Enter a parse tree produced by GrammarParser#foreach_loop.
    def enterForeach_loop(self, ctx:GrammarParser.Foreach_loopContext):
        pass

    # Exit a parse tree produced by GrammarParser#foreach_loop.
    def exitForeach_loop(self, ctx:GrammarParser.Foreach_loopContext):
        pass


    # Enter a parse tree produced by GrammarParser#expression.
    def enterExpression(self, ctx:GrammarParser.ExpressionContext):
        pass

    # Exit a parse tree produced by GrammarParser#expression.
    def exitExpression(self, ctx:GrammarParser.ExpressionContext):
        pass


    # Enter a parse tree produced by GrammarParser#logical_expr.
    def enterLogical_expr(self, ctx:GrammarParser.Logical_exprContext):
        pass

    # Exit a parse tree produced by GrammarParser#logical_expr.
    def exitLogical_expr(self, ctx:GrammarParser.Logical_exprContext):
        pass


    # Enter a parse tree produced by GrammarParser#comparison.
    def enterComparison(self, ctx:GrammarParser.ComparisonContext):
        pass

    # Exit a parse tree produced by GrammarParser#comparison.
    def exitComparison(self, ctx:GrammarParser.ComparisonContext):
        pass


    # Enter a parse tree produced by GrammarParser#additive_expr.
    def enterAdditive_expr(self, ctx:GrammarParser.Additive_exprContext):
        pass

    # Exit a parse tree produced by GrammarParser#additive_expr.
    def exitAdditive_expr(self, ctx:GrammarParser.Additive_exprContext):
        pass


    # Enter a parse tree produced by GrammarParser#multiplicative_expr.
    def enterMultiplicative_expr(self, ctx:GrammarParser.Multiplicative_exprContext):
        pass

    # Exit a parse tree produced by GrammarParser#multiplicative_expr.
    def exitMultiplicative_expr(self, ctx:GrammarParser.Multiplicative_exprContext):
        pass


    # Enter a parse tree produced by GrammarParser#factor.
    def enterFactor(self, ctx:GrammarParser.FactorContext):
        pass

    # Exit a parse tree produced by GrammarParser#factor.
    def exitFactor(self, ctx:GrammarParser.FactorContext):
        pass


    # Enter a parse tree produced by GrammarParser#literal.
    def enterLiteral(self, ctx:GrammarParser.LiteralContext):
        pass

    # Exit a parse tree produced by GrammarParser#literal.
    def exitLiteral(self, ctx:GrammarParser.LiteralContext):
        pass


    # Enter a parse tree produced by GrammarParser#batch_literal.
    def enterBatch_literal(self, ctx:GrammarParser.Batch_literalContext):
        pass

    # Exit a parse tree produced by GrammarParser#batch_literal.
    def exitBatch_literal(self, ctx:GrammarParser.Batch_literalContext):
        pass


    # Enter a parse tree produced by GrammarParser#batch_items.
    def enterBatch_items(self, ctx:GrammarParser.Batch_itemsContext):
        pass

    # Exit a parse tree produced by GrammarParser#batch_items.
    def exitBatch_items(self, ctx:GrammarParser.Batch_itemsContext):
        pass


    # Enter a parse tree produced by GrammarParser#color_literal.
    def enterColor_literal(self, ctx:GrammarParser.Color_literalContext):
        pass

    # Exit a parse tree produced by GrammarParser#color_literal.
    def exitColor_literal(self, ctx:GrammarParser.Color_literalContext):
        pass


    # Enter a parse tree produced by GrammarParser#binary_operation.
    def enterBinary_operation(self, ctx:GrammarParser.Binary_operationContext):
        pass

    # Exit a parse tree produced by GrammarParser#binary_operation.
    def exitBinary_operation(self, ctx:GrammarParser.Binary_operationContext):
        pass


    # Enter a parse tree produced by GrammarParser#operation.
    def enterOperation(self, ctx:GrammarParser.OperationContext):
        pass

    # Exit a parse tree produced by GrammarParser#operation.
    def exitOperation(self, ctx:GrammarParser.OperationContext):
        pass


    # Enter a parse tree produced by GrammarParser#watermark_op.
    def enterWatermark_op(self, ctx:GrammarParser.Watermark_opContext):
        pass

    # Exit a parse tree produced by GrammarParser#watermark_op.
    def exitWatermark_op(self, ctx:GrammarParser.Watermark_opContext):
        pass


    # Enter a parse tree produced by GrammarParser#image_watermark_op.
    def enterImage_watermark_op(self, ctx:GrammarParser.Image_watermark_opContext):
        pass

    # Exit a parse tree produced by GrammarParser#image_watermark_op.
    def exitImage_watermark_op(self, ctx:GrammarParser.Image_watermark_opContext):
        pass


    # Enter a parse tree produced by GrammarParser#quantize_op.
    def enterQuantize_op(self, ctx:GrammarParser.Quantize_opContext):
        pass

    # Exit a parse tree produced by GrammarParser#quantize_op.
    def exitQuantize_op(self, ctx:GrammarParser.Quantize_opContext):
        pass


    # Enter a parse tree produced by GrammarParser#text_watermark_op.
    def enterText_watermark_op(self, ctx:GrammarParser.Text_watermark_opContext):
        pass

    # Exit a parse tree produced by GrammarParser#text_watermark_op.
    def exitText_watermark_op(self, ctx:GrammarParser.Text_watermark_opContext):
        pass


    # Enter a parse tree produced by GrammarParser#watermark_options.
    def enterWatermark_options(self, ctx:GrammarParser.Watermark_optionsContext):
        pass

    # Exit a parse tree produced by GrammarParser#watermark_options.
    def exitWatermark_options(self, ctx:GrammarParser.Watermark_optionsContext):
        pass


    # Enter a parse tree produced by GrammarParser#watermark_option.
    def enterWatermark_option(self, ctx:GrammarParser.Watermark_optionContext):
        pass

    # Exit a parse tree produced by GrammarParser#watermark_option.
    def exitWatermark_option(self, ctx:GrammarParser.Watermark_optionContext):
        pass


    # Enter a parse tree produced by GrammarParser#watermark_text_params.
    def enterWatermark_text_params(self, ctx:GrammarParser.Watermark_text_paramsContext):
        pass

    # Exit a parse tree produced by GrammarParser#watermark_text_params.
    def exitWatermark_text_params(self, ctx:GrammarParser.Watermark_text_paramsContext):
        pass


    # Enter a parse tree produced by GrammarParser#watermark_text_param.
    def enterWatermark_text_param(self, ctx:GrammarParser.Watermark_text_paramContext):
        pass

    # Exit a parse tree produced by GrammarParser#watermark_text_param.
    def exitWatermark_text_param(self, ctx:GrammarParser.Watermark_text_paramContext):
        pass


    # Enter a parse tree produced by GrammarParser#watermark_position.
    def enterWatermark_position(self, ctx:GrammarParser.Watermark_positionContext):
        pass

    # Exit a parse tree produced by GrammarParser#watermark_position.
    def exitWatermark_position(self, ctx:GrammarParser.Watermark_positionContext):
        pass


    # Enter a parse tree produced by GrammarParser#resize_op.
    def enterResize_op(self, ctx:GrammarParser.Resize_opContext):
        pass

    # Exit a parse tree produced by GrammarParser#resize_op.
    def exitResize_op(self, ctx:GrammarParser.Resize_opContext):
        pass


    # Enter a parse tree produced by GrammarParser#resize_params.
    def enterResize_params(self, ctx:GrammarParser.Resize_paramsContext):
        pass

    # Exit a parse tree produced by GrammarParser#resize_params.
    def exitResize_params(self, ctx:GrammarParser.Resize_paramsContext):
        pass


    # Enter a parse tree produced by GrammarParser#dimension_params.
    def enterDimension_params(self, ctx:GrammarParser.Dimension_paramsContext):
        pass

    # Exit a parse tree produced by GrammarParser#dimension_params.
    def exitDimension_params(self, ctx:GrammarParser.Dimension_paramsContext):
        pass


    # Enter a parse tree produced by GrammarParser#aspect_ratio.
    def enterAspect_ratio(self, ctx:GrammarParser.Aspect_ratioContext):
        pass

    # Exit a parse tree produced by GrammarParser#aspect_ratio.
    def exitAspect_ratio(self, ctx:GrammarParser.Aspect_ratioContext):
        pass


    # Enter a parse tree produced by GrammarParser#crop_op.
    def enterCrop_op(self, ctx:GrammarParser.Crop_opContext):
        pass

    # Exit a parse tree produced by GrammarParser#crop_op.
    def exitCrop_op(self, ctx:GrammarParser.Crop_opContext):
        pass


    # Enter a parse tree produced by GrammarParser#crop_params.
    def enterCrop_params(self, ctx:GrammarParser.Crop_paramsContext):
        pass

    # Exit a parse tree produced by GrammarParser#crop_params.
    def exitCrop_params(self, ctx:GrammarParser.Crop_paramsContext):
        pass


    # Enter a parse tree produced by GrammarParser#filter_op.
    def enterFilter_op(self, ctx:GrammarParser.Filter_opContext):
        pass

    # Exit a parse tree produced by GrammarParser#filter_op.
    def exitFilter_op(self, ctx:GrammarParser.Filter_opContext):
        pass


    # Enter a parse tree produced by GrammarParser#filter_type.
    def enterFilter_type(self, ctx:GrammarParser.Filter_typeContext):
        pass

    # Exit a parse tree produced by GrammarParser#filter_type.
    def exitFilter_type(self, ctx:GrammarParser.Filter_typeContext):
        pass


    # Enter a parse tree produced by GrammarParser#format_op.
    def enterFormat_op(self, ctx:GrammarParser.Format_opContext):
        pass

    # Exit a parse tree produced by GrammarParser#format_op.
    def exitFormat_op(self, ctx:GrammarParser.Format_opContext):
        pass


    # Enter a parse tree produced by GrammarParser#format_type.
    def enterFormat_type(self, ctx:GrammarParser.Format_typeContext):
        pass

    # Exit a parse tree produced by GrammarParser#format_type.
    def exitFormat_type(self, ctx:GrammarParser.Format_typeContext):
        pass


    # Enter a parse tree produced by GrammarParser#enhancement_op.
    def enterEnhancement_op(self, ctx:GrammarParser.Enhancement_opContext):
        pass

    # Exit a parse tree produced by GrammarParser#enhancement_op.
    def exitEnhancement_op(self, ctx:GrammarParser.Enhancement_opContext):
        pass


    # Enter a parse tree produced by GrammarParser#enhancement_type.
    def enterEnhancement_type(self, ctx:GrammarParser.Enhancement_typeContext):
        pass

    # Exit a parse tree produced by GrammarParser#enhancement_type.
    def exitEnhancement_type(self, ctx:GrammarParser.Enhancement_typeContext):
        pass


    # Enter a parse tree produced by GrammarParser#metadata_op.
    def enterMetadata_op(self, ctx:GrammarParser.Metadata_opContext):
        pass

    # Exit a parse tree produced by GrammarParser#metadata_op.
    def exitMetadata_op(self, ctx:GrammarParser.Metadata_opContext):
        pass


    # Enter a parse tree produced by GrammarParser#metadata_field.
    def enterMetadata_field(self, ctx:GrammarParser.Metadata_fieldContext):
        pass

    # Exit a parse tree produced by GrammarParser#metadata_field.
    def exitMetadata_field(self, ctx:GrammarParser.Metadata_fieldContext):
        pass


    # Enter a parse tree produced by GrammarParser#pattern.
    def enterPattern(self, ctx:GrammarParser.PatternContext):
        pass

    # Exit a parse tree produced by GrammarParser#pattern.
    def exitPattern(self, ctx:GrammarParser.PatternContext):
        pass


    # Enter a parse tree produced by GrammarParser#batch_op.
    def enterBatch_op(self, ctx:GrammarParser.Batch_opContext):
        pass

    # Exit a parse tree produced by GrammarParser#batch_op.
    def exitBatch_op(self, ctx:GrammarParser.Batch_opContext):
        pass


    # Enter a parse tree produced by GrammarParser#expression_list.
    def enterExpression_list(self, ctx:GrammarParser.Expression_listContext):
        pass

    # Exit a parse tree produced by GrammarParser#expression_list.
    def exitExpression_list(self, ctx:GrammarParser.Expression_listContext):
        pass


    # Enter a parse tree produced by GrammarParser#options.
    def enterOptions(self, ctx:GrammarParser.OptionsContext):
        pass

    # Exit a parse tree produced by GrammarParser#options.
    def exitOptions(self, ctx:GrammarParser.OptionsContext):
        pass


    # Enter a parse tree produced by GrammarParser#option.
    def enterOption(self, ctx:GrammarParser.OptionContext):
        pass

    # Exit a parse tree produced by GrammarParser#option.
    def exitOption(self, ctx:GrammarParser.OptionContext):
        pass


    # Enter a parse tree produced by GrammarParser#value.
    def enterValue(self, ctx:GrammarParser.ValueContext):
        pass

    # Exit a parse tree produced by GrammarParser#value.
    def exitValue(self, ctx:GrammarParser.ValueContext):
        pass



del GrammarParser