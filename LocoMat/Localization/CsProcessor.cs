using LocoMat.Localization.Filters;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;

namespace LocoMat.Localization;

public class CsProcessor
{
    private readonly BackupService _backupService;
    private readonly ILiteralFilter _filter;
    private readonly ILogger<CsProcessor> _logger;
    private readonly ResourceKeys _modelKeys;

    public CsProcessor(
        ILogger<CsProcessor> logger,
        ResourceKeys modelKeys,
        BackupService backupService,
        ILiteralFilter filter
    )
    {
        _logger = logger;
        _modelKeys = modelKeys;
        _backupService = backupService;
        _filter = filter;
    }

    public async Task ProcessFile(string filePath)
    {
        var code = await File.ReadAllTextAsync(filePath);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        var rewriter = new LocalizeStringLiteralsRewriter(_modelKeys, _filter); // Pass necessary dependencies
        var updatedRoot = rewriter.Visit(root);
        await _backupService.WriteAllTextWithBackup(filePath, updatedRoot.ToFullString());
    }
}