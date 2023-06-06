using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LocoMat.Localization.Filters;

public interface ILiteralFilter
{
    string Name { get; }
    string Description { get; }
    bool IsProhibited(LiteralExpressionSyntax literal);
}