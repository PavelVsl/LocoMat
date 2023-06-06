using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LocoMat.Localization.Filters;

public class NamedArgumentFilter : BaseLiteralFilter
{
    public override string Name => "Named arguments";
    public override string Description => "Literals used as names in named arguments";

    public override bool IsProhibited(LiteralExpressionSyntax literal)
    {
        return literal.Parent is NameColonSyntax || literal.Parent is NameEqualsSyntax;
    }
}