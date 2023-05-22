using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LocoMat;

public static class ExpressionInfoExtensions
{
    public static string GetResourceKey(this LiteralExpressionSyntax node)
    {
        var text = node.Token.ValueText;
        var key = text.GenerateResourceKey();
        var expressionInfo = GetExpressionInfo(node);
        if (!string.IsNullOrEmpty(expressionInfo.GenericParameterName)) return $"{expressionInfo.GenericParameterName}.{key}";
        return $"{expressionInfo.ClassName}.{key}";
    }
    
    public static ExpressionInfo GetExpressionInfo(CSharpSyntaxNode node)
    {
        var invocationExpression = node.Ancestors().OfType<InvocationExpressionSyntax>().FirstOrDefault();
        var methodName = invocationExpression?.Expression.ToString();
        var genericArgumentList = invocationExpression?.DescendantNodes().OfType<TypeArgumentListSyntax>().FirstOrDefault();
        var genericParameterName = genericArgumentList?.Arguments.FirstOrDefault()?.ToString();
        var namespaceDeclaration = node.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
        var nameSpaceName = namespaceDeclaration?.Name.ToString();
        var classDeclaration = node.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        var className = classDeclaration?.Identifier.ToString();
        var elementName = node.Ancestors().OfType<ElementAccessExpressionSyntax>().FirstOrDefault()?.Expression.ToString();
        ;
        return new ExpressionInfo
        {
            NameSpaceName = nameSpaceName,
            ClassName = className,
            MethodName = methodName,
            GenericParameterName = genericParameterName,
            ElementName = elementName,
        };
    }
}