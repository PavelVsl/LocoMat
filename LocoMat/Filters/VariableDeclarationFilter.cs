using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LocoMat;

public class VariableDeclarationFilter : BaseLiteralFilter
{
    public override string Name => "Variable declarations";
    public override string Description => "Literals used as names in variable declarations";

    public override bool IsProhibited(LiteralExpressionSyntax literal)
    {
        return literal.Parent is VariableDeclaratorSyntax;
    }
}