using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LocoMat.Localization.Filters;

public class TooLongLiteralFilter : BaseLiteralFilter
{
    public override string Name => "Too long literals";
    public override string Description => "Literals that are too long (more than 150 characters)";

    public override bool IsProhibited(LiteralExpressionSyntax literal)
    {
        return literal.Token.ValueText.Length > 150;
    }
}