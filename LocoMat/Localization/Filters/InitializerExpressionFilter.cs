using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LocoMat.Localization.Filters;

public class InitializerExpressionFilter : BaseLiteralFilter
{
    public override string Name => "Initializer expression";
    public override string Description => "Literals used in initializer expressions";

    public override bool IsProhibited(LiteralExpressionSyntax literal)
    {
        return literal.Ancestors().Any(x => x is InitializerExpressionSyntax);
    }
}