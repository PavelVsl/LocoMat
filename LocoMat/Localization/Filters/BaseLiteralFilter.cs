using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LocoMat.Localization.Filters;

public abstract class BaseLiteralFilter : ILiteralFilter
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract bool IsProhibited(LiteralExpressionSyntax literal);
}