using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace LocoMat;

public class CsProcessor
{
    private readonly BackupService _backupService;
    private readonly ExpressionFilterService _filter;
    private readonly ConfigurationData _config;
    private readonly CustomActions _customActions;
    private readonly ILogger<CsProcessor> _logger;
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
