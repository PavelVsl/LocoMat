using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LocoMat.Localization.Filters;

public class IndexerFilter : BaseLiteralFilter
{
    public override string Name => "Indexer";
    public override string Description => "Literals used in indexer";

    public override bool IsProhibited(LiteralExpressionSyntax literal)
    {
        return literal.Parent is ElementAccessExpressionSyntax;
    }
}