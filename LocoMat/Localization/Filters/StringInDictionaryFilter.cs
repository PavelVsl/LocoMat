using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LocoMat.Localization.Filters;

public class StringInDictionaryFilter : BaseLiteralFilter
{
    public override string Name => "String in dictionary access";
    public override string Description => "String literals used in dictionary access";

    public override bool IsProhibited(LiteralExpressionSyntax literal)
    {
        return literal.Parent is ArgumentSyntax &&
               literal.Parent.Parent is BracketedArgumentListSyntax &&
               //literal.Parent.Parent.Parent is ElementAccessExpressionSyntax &&
               literal.Token.Value is string;
    }
}