using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LocoMat;

public class InterpolatedStringFilter : BaseLiteralFilter
{
    public override string Name => "Interpolated strings";
    public override string Description => "Literals used in interpolated strings";

    public override bool IsProhibited(LiteralExpressionSyntax literal)
    {
        return literal.Parent is InterpolatedStringExpressionSyntax
               || literal.Parent is InterpolatedStringTextSyntax
               || literal.Parent is InterpolationSyntax
               || literal.Parent is InterpolationAlignmentClauseSyntax;
    }
}