using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LocoMat;

public interface ILiteralFilter
{
    string Name { get; }
    string Description { get; }
    bool IsProhibited(LiteralExpressionSyntax literal);
}