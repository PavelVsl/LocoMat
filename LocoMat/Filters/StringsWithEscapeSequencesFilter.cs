using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LocoMat;

public class StringsWithEscapeSequencesFilter : BaseLiteralFilter
{
    public override string Name => "Strings with escape sequences";
    public override string Description => "Literals that are strings with escape sequences";

    public override bool IsProhibited(LiteralExpressionSyntax literal)
    {
        return literal.Token.ValueText.Contains("\\");
    }
}