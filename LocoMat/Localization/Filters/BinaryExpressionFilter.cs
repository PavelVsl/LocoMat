using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LocoMat.Localization.Filters;

public class BinaryExpressionFilter : BaseLiteralFilter
{
    public override string Name => "Logical expression";
    public override string Description => "Literals used in logical expressions";

    public override bool IsProhibited(LiteralExpressionSyntax literal)
    {
        return literal.Parent is BinaryExpressionSyntax binaryExpression;
    }
}