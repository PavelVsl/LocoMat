using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LocoMat;

public class VerbatimStringFilter : BaseLiteralFilter
{
    public override string Name => "Verbatim strings";
    public override string Description => "Literals that are verbatim strings";

    public override bool IsProhibited(LiteralExpressionSyntax literal)
    {
        return literal.Token.Text.StartsWith("@");
    }
}