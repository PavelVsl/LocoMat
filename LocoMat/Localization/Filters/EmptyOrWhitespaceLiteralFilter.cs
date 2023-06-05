using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LocoMat;

public class EmptyOrWhitespaceLiteralFilter : BaseLiteralFilter
{
    public override string Name => "Empty or whitespace literals";
    public override string Description => "Literals that are empty or whitespace";

    public override bool IsProhibited(LiteralExpressionSyntax literal)
    {
        return string.IsNullOrWhiteSpace(literal.Token.ValueText);
    }
}