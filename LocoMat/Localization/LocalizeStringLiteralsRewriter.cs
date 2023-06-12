using LocoMat.Localization.Filters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace LocoMat.Localization;

public class LocalizeStringLiteralsRewriter : CSharpSyntaxRewriter
{
    private readonly ILogger<LocalizeStringLiteralsRewriter> _logger;
    private readonly ResourceKeys _modelKeys;
    private readonly ILiteralFilter _filter;


    public LocalizeStringLiteralsRewriter(ResourceKeys modelKeys, ILiteralFilter filter, ILogger<LocalizeStringLiteralsRewriter> logger) : base()
    {
        _logger = logger; 
        _modelKeys = modelKeys;
        _filter = filter;
    }

    public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
    {
        if (!node.IsKind(SyntaxKind.StringLiteralExpression))
            return base.VisitLiteralExpression(node);

        if (!IsLocalizable(node)) return base.VisitLiteralExpression(node);
        //var message = $"Processing literal \"{node.Token.ValueText}\"";
        //_logger.LogInformation(message);
        var line = node.SyntaxTree.GetText().Lines[node.GetLocation().GetLineSpan().Span.Start.Line].ToString().Trim();
        //get the text of the literal line
        var text = node.Token.ValueText;
        var resourceKey = node.GetResourceKey();
        var localizerCall = SyntaxFactory.ParseExpression($"D[\"{resourceKey}\"]");
        _modelKeys.TryAdd(resourceKey, text);
        // replace the literal in line with a call to the localizer
        var resultLine = line.Replace(text, localizerCall.ToString());
        _logger.LogInformation($"Replace: {line}\n" +
                               $" =>>     {resultLine}");        
        return localizerCall;
    }

    public bool IsLocalizable(LiteralExpressionSyntax literal)
    {
        if (literal == null) return false;
        return !_filter.IsProhibited(literal);
    }
}
