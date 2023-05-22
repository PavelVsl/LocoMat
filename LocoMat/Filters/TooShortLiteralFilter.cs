using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LocoMat;

public class TooShortLiteralFilter : BaseLiteralFilter
{
    public override string Name => "Too short literals";
    public override string Description => "Literals that are too short (less than 3 characters)";

    public override bool IsProhibited(LiteralExpressionSyntax literal)
    {
        return literal.Token.ValueText.Length < 3;
    }
}