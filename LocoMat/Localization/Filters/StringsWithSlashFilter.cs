using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LocoMat.Localization.Filters;

public class StringsWithSlashFilter : BaseLiteralFilter
{
    public override string Name => "Strings with slash";
    public override string Description => "Literals that are strings with slash";

    public override bool IsProhibited(LiteralExpressionSyntax literal)
    {
        return literal.Token.ValueText.Contains("/");
    }
}