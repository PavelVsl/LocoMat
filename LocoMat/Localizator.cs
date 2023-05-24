using System.Xml;
using Microsoft.Extensions.Logging;

namespace LocoMat;

public class Localizator
{
    private readonly ILogger<RazorProcessor> _logger;
    private readonly ConfigurationData _config;
    private readonly CustomActions _customActions;
    private readonly ResourceKeys _resourceKeys;
    private readonly ResourceGenerator _resourceGenerator;
    private readonly CsProcessor _csProcessor;
    private readonly bool testMode;
    private BackupService _backupService;
    private readonly RazorProcessor _razorProcessor;

    public Localizator(ILogger<RazorProcessor> logger, ConfigurationData config, CustomActions customActions, ResourceKeys modelKeys, ResourceGenerator resourceGenerator, CsProcessor csProcessor, BackupService backupService, RazorProcessor razorProcessor)
    {
        _logger = logger;
        _config = config;
        _customActions = customActions;
        _resourceKeys = modelKeys;
        _resourceGenerator = resourceGenerator;
        _csProcessor = csProcessor;
        _backupService = backupService;
        _razorProcessor = razorProcessor;
        testMode = config.TestMode;
       
    }
    
    public async Task Localize()
    {
        //if resource at modelPath exists, load it  
        if (File.Exists(_config.ResourcePath))
        {
            var doc = new XmlDocument();
            doc.Load(_config.ResourcePath);
            var elemList = doc.GetElementsByTagName("data");
            foreach (XmlNode node in elemList) _resourceKeys.TryAdd(node.Attributes["name"].Value, node.InnerText);
        }

        //get folderPath folder name  from config.Project project file name
        var folderPath = Path.GetDirectoryName(_config.Project);

        // recurse through the directory folderPath   
        if (Directory.Exists(folderPath))
        {
            var files = Directory.GetFiles(folderPath, _config.IncludeFiles, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (_config.ExcludeFiles.Contains(Path.GetFileName(file))) continue;
                _logger.LogInformation("Processing file: " + file);
                await _razorProcessor.ProcessRazorFile(file);
                //if partial class cs file exists, process it, file name is same as razor file name with .razor.cs extension
                var csFile = file + ".cs";
                if (File.Exists(csFile))
                {
                    _logger.LogInformation("Processing file: " + csFile);
                    await _csProcessor.ProcessFile(csFile);
                }
            }
        }
        Utilities.EnsureFolderExists(_config.ResourcePath);
        _razorProcessor.GenerateResourceStubFile();
        if (_resourceKeys.Count > 0) _resourceGenerator.CreateResxFile(_resourceKeys);
        await _resourceGenerator.TranslateResourceFile();
    }

}
