using System.Xml;
using Microsoft.Extensions.Logging;

namespace LocoMat.Localization;

public class LocalizationService(
    ILogger<LocalizationService> logger,
    ConfigurationData config,
    ResourceKeys modelKeys,
    CsProcessor csProcessor,
    RazorProcessor razorProcessor
)
    : ILocalizationService
{
    public async Task Localize()
    {
        //if resource at modelPath exists, load it  
        if (File.Exists(config.Resource))
        {
            var doc = new XmlDocument();
            doc.Load(config.Resource);
            var elemList = doc.GetElementsByTagName("data");
            foreach (XmlNode node in elemList) modelKeys.TryAdd(node.Attributes["name"].Value, node.InnerText);
        }

        //get folderPath folder name  from config.Project project file name
        var folderPath = Path.GetDirectoryName(config.Project);

        // recurse through the directory folderPath   
        if (Directory.Exists(folderPath))
        {
            var files = Directory.GetFiles(folderPath, config.Include, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var changed = false;
                //if partial class cs file exists, process it, file name is same as razor file name with .razor.cs extension
                var csFile = file + ".cs";
                if (File.Exists(csFile))
                {
                    logger.LogDebug($"Processing file: {csFile}", csFile);
                    changed = await csProcessor.ProcessFile(csFile);
                }
                if (config.ExcludeFiles.Contains(Path.GetFileName(file))) continue;
                logger.LogDebug($"Processing file: {file}");
                await razorProcessor.ProcessRazorFile(file,changed);
            }
        }

        Utilities.EnsureFolderExists(config.Resource);
        razorProcessor.GenerateResourceStubFile();
        if (modelKeys.Count > 0) CreateResxFile(modelKeys);
        //await _resourceGenerator.TranslateResourceFile();
    }

    public void CreateResxFile(Dictionary<string, string> resourceKeys)
    {
        var filePath = config.Resource;
        CreateResxFile(filePath, resourceKeys);
    }

    public void CreateResxFile(string filePath, Dictionary<string, string> resourceKeys)
    {
        Utilities.EnsureFolderExists(config.Resource);
        var existingResources = Utilities.GetOrCreateResxFile(filePath);
        foreach (var resource in resourceKeys)
            existingResources.TryAdd(resource.Key, resource.Value);
        if (!config.TestMode)
        {
            Utilities.WriteResourcesToFile(existingResources, filePath);
            logger.LogInformation($"Created resource: {filePath}");
        }
    }
}
