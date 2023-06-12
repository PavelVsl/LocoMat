using LocoMat.Localization.Filters;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;

namespace LocoMat.Localization;

public class CsProcessor
{
    private readonly BackupService _backupService;
    private readonly ILiteralFilter _filter;
    private readonly LocalizeStringLiteralsRewriter _rewriter;
    private readonly ILogger<CsProcessor> _logger;
    private readonly ResourceKeys _modelKeys;

    public CsProcessor(
        ILogger<CsProcessor> logger,
        ResourceKeys modelKeys,
        BackupService backupService,
        ILiteralFilter filter,
        LocalizeStringLiteralsRewriter rewriter
    )
    {
        _logger = logger;
        _modelKeys = modelKeys;
        _backupService = backupService;
        _filter = filter;
        _rewriter = rewriter;
    }

    public async Task<bool> ProcessFile(string filePath)
    {
        var code = await File.ReadAllTextAsync(filePath);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var updatedRoot = _rewriter.Visit(root);
        var newCode = updatedRoot.ToFullString();
        if (newCode == code)
        {
            _logger.LogDebug($"No changes to {filePath}");
            return false;
        }
        _logger.LogInformation($"Writing changes to {filePath}");
        await _backupService.WriteAllTextWithBackup(filePath, updatedRoot.ToFullString());
        return true;
    }
}
