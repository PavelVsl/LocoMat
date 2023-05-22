using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LocoMat;

public class MemberAccessExpressionFilter : BaseLiteralFilter
{
    public override string Name => "Member access expressions";
    public override string Description => "Literals used in member access expressions";

    public override bool IsProhibited(LiteralExpressionSyntax literal)
    {
        return literal.Parent is MemberAccessExpressionSyntax;
    }
}