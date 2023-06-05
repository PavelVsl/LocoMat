using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LocoMat;

public class SwitchStatementLabelFilter : BaseLiteralFilter
{
    public override string Name => "Switch statements labels";
    public override string Description => "Literals used as labels in switch statements (excluding case pattern switch labels)";

    public override bool IsProhibited(LiteralExpressionSyntax literal)
    {
        return literal.Parent is SwitchLabelSyntax && !(literal.Parent is CasePatternSwitchLabelSyntax);
    }
}