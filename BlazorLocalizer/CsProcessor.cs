using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace BlazorLocalizer;

public class CsProcessor
{
    private readonly BackupService _backupService;
    private readonly ExpressionFilterService _filter;
    private readonly ConfigurationData _config;
    private readonly CustomActions _customActions;
    private readonly ILogger<CsProcessor> _logger;
    private readonly string _matchFile;
    private readonly SortedSet<string> _matchStrings = new();
    private readonly ResourceKeys _modelKeys;
    private readonly ResourceGenerator _resourceGenerator;

    public CsProcessor(
        ILogger<CsProcessor> logger,
        ConfigurationData config,
        CustomActions customActions,
        ResourceKeys modelKeys,
        ResourceGenerator resourceGenerator,
        BackupService backupService,
        ExpressionFilterService filter
    )
    {
        _logger = logger;
        _config = config;
        _customActions = customActions;
        _modelKeys = modelKeys;
        _resourceGenerator = resourceGenerator;
        _backupService = backupService;
        _filter = filter;
        _matchFile = Path.Combine(Path.GetDirectoryName(_config.Project), "matchstring.txt");
    }

    public async Task SaveMatches()
    {
        await File.WriteAllLinesAsync(_matchFile, _matchStrings);
    }

    public async Task ProcessFile(string filePath)
    {
        var code = await File.ReadAllTextAsync(filePath);
        var tree = CSharpSyntaxTree.ParseText(code);
        CompilationUnitSyntax croot = tree.GetCompilationUnitRoot();
        var root = tree.GetRoot();
        var rewriter = new LocalizeStringLiteralsRewriter(_modelKeys, _filter); // Pass necessary dependencies
        var updatedRoot = rewriter.Visit(root);
        await _backupService.WriteAllTextWithBackup(filePath, updatedRoot.ToFullString());
    }
}

public class ExpressionInfo
{
    public string NameSpaceName { get; set; }
    public string ClassName { get; set; }
    public string MethodName { get; set; }
    public string GenericParameterName { get; set; }
    public string ElementName { get; set; }
    public string MatchString => $"{ClassName}.{MethodName}.{GenericParameterName}.{ElementName}";

    public override string ToString()
    {
        return $"{nameof(NameSpaceName)}: {NameSpaceName}, {nameof(ClassName)}: {ClassName}, {nameof(MethodName)}: {MethodName}, {nameof(GenericParameterName)}: {GenericParameterName}, {nameof(ElementName)}: {ElementName}";
    }
}

public class LocalizeStringLiteralsRewriter : CSharpSyntaxRewriter
{
    private readonly ILogger<LocalizeStringLiteralsRewriter> _logger;
    private readonly ResourceKeys _modelKeys;
    private readonly ExpressionFilterService _filter;

    public LocalizeStringLiteralsRewriter(ResourceKeys modelKeys, ExpressionFilterService filter) : base()
    {
        _logger = new Logger<LocalizeStringLiteralsRewriter>(new LoggerFactory());
        _modelKeys = modelKeys;
        _filter = filter;
    }

    public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
    {
        if (!node.IsKind(SyntaxKind.StringLiteralExpression))
            return base.VisitLiteralExpression(node);

        if (!_filter.IsLocalizable(node))
        {
            return base.VisitLiteralExpression(node);
        }
        var message = $"Processing literal \"{node.Token.ValueText}\"";
        _logger.LogInformation(message);
        var text = node.Token.ValueText;
        var resourceKey = node.GetResourceKey();
        var invocationExpr = SyntaxFactory.ParseExpression($"D[\"{resourceKey}\"]");
        _modelKeys.TryAdd(resourceKey, text);
        return invocationExpr;
    }
}

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

