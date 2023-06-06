using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LocoMat.Localization.Filters;

public class AttributeDeclarationArgumentFilter : BaseLiteralFilter
{
    public override string Name => "Attribute declarations arguments";
    public override string Description => "Literals used as arguments in attribute declarations";

    public override bool IsProhibited(LiteralExpressionSyntax literal)
    {
        return literal.Parent is AttributeArgumentSyntax;
    }
}