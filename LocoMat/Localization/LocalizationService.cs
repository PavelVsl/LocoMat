using System.Xml;
using Microsoft.Extensions.Logging;

namespace LocoMat;

public class LocalizationService : ILocalizationService
{
    private readonly ILogger<LocalizationService> _logger;
    private readonly ConfigurationData _config;
    private readonly ResourceKeys _resourceKeys;
    private readonly CsProcessor _csProcessor;
    private readonly RazorProcessor _razorProcessor;

    public LocalizationService(
        ILogger<LocalizationService> logger, 
        ConfigurationData config, 
        ResourceKeys modelKeys, 
        CsProcessor csProcessor, 
        RazorProcessor razorProcessor)
    {
        _logger = logger;
        _config = config;
        _resourceKeys = modelKeys;
        _csProcessor = csProcessor;
        _razorProcessor = razorProcessor;
    }
    
    public async Task Localize()
    {
        //if resource at modelPath exists, load it  
        if (File.Exists(_config.Resource))
        {
            var doc = new XmlDocument();
            doc.Load(_config.Resource);
            var elemList = doc.GetElementsByTagName("data");
            foreach (XmlNode node in elemList) _resourceKeys.TryAdd(node.Attributes["name"].Value, node.InnerText);
        }

        //get folderPath folder name  from config.Project project file name
        var folderPath = Path.GetDirectoryName(_config.Project);

        // recurse through the directory folderPath   
        if (Directory.Exists(folderPath))
        {
            var files = Directory.GetFiles(folderPath, _config.Include, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (_config.ExcludeFiles.Contains(Path.GetFileName(file))) continue;
                _logger.LogInformation("Processing file: {File}", file);
                await _razorProcessor.ProcessRazorFile(file);
                //if partial class cs file exists, process it, file name is same as razor file name with .razor.cs extension
                var csFile = file + ".cs";
                if (File.Exists(csFile))
                {
                    _logger.LogInformation("Processing file: {File}", csFile);
                    await _csProcessor.ProcessFile(csFile);
                }
            }
        }
        Utilities.EnsureFolderExists(_config.Resource);
        _razorProcessor.GenerateResourceStubFile();
        if (_resourceKeys.Count > 0) CreateResxFile(_resourceKeys);
        //await _resourceGenerator.TranslateResourceFile();
    }
    
    public void CreateResxFile(Dictionary<string, string> resourceKeys)
    {
        var filePath = _config.Resource;
        CreateResxFile(filePath,resourceKeys);
    }

    public void CreateResxFile(string filePath, Dictionary<string, string> resourceKeys)
    {
        Utilities.EnsureFolderExists(_config.Resource);
        var existingResources = Utilities.GetOrCreateResxFile(filePath);
        foreach (var resource in resourceKeys)
            existingResources.TryAdd(resource.Key, resource.Value);
        if (!_config.TestMode)
        {
            Utilities.WriteResourcesToFile(existingResources, filePath);
            _logger.LogInformation("Created resource: {FilePath}", filePath);
        }
    }

}
