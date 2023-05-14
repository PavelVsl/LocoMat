using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Converters;
using static BlazorLocalizer.Utilities;

namespace BlazorLocalizer;

public class RazorProcessor
{
    private readonly ILogger<RazorProcessor> _logger;
    private readonly ConfigurationData _config;
    private readonly CustomActions _customActions;
    private readonly ResourceKeys _resourceKeys;
    private readonly ResourceGenerator _resourceGenerator;
    private readonly CsProcessor _csProcessor;
    private readonly bool testMode;
    private BackupService _backupService;

    public RazorProcessor(ILogger<RazorProcessor> logger, ConfigurationData config, CustomActions customActions, ResourceKeys modelKeys, ResourceGenerator resourceGenerator, CsProcessor csProcessor, BackupService backupService)
    {
        _logger = logger;
        _config = config;
        _customActions = customActions;
        _resourceKeys = modelKeys;
        _resourceGenerator = resourceGenerator;
        _csProcessor = csProcessor;
        _backupService = backupService;
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
                await ProcessRazorFile(file);
                //if partial class cs file exists, process it, file name is same as razor file name with .razor.cs extension
                var csFile = file + ".cs";
                if (File.Exists(csFile))
                {
                    _logger.LogInformation("Processing file: " + csFile);

                    await _csProcessor.ProcessFile(csFile);
                }
            }
        }

        GenerateCsFile();
        await _csProcessor.SaveMatches();
        if (_resourceKeys.Count > 0) _resourceGenerator.CreateResxFile(_resourceKeys);
        await _resourceGenerator.TranslateResourceFile();
    }

    private async Task ProcessCsFile(string csFile)
    {
        // Step 1: Open cs file
        var csContent = await File.ReadAllTextAsync(csFile);
        var className = Path.GetFileNameWithoutExtension(csFile);

        var newCsContent = csContent;
        // // Step 2: Add localizer injection                                                                                                                      
        // string newCsContent = AddLocalizerInjection(csContent, csFile);

        // Step 3: Search localizable strings and replace them with localizer calls
        newCsContent = ProcessCustomActions(_customActions, newCsContent, className, ".cs");

        //compare newCsContent with csContent, if different, write to disk
        if (newCsContent == csContent)
        {
            _logger.LogDebug($"No changes in {csFile}");
            return;
        }

        // Step 4: Write the modified razor content back to the file
        if (testMode)
        {
            _logger.LogInformation($"Changes to {csFile} not written to disk.");
            _logger.LogInformation(newCsContent);
        }
        else
        {
            await _backupService.WriteAllTextWithBackup(csFile, newCsContent);
        }
    }

    private async Task ProcessRazorFile(string razorFileName)
    {
        // Step 1: Open razor file
        var razorContent = await File.ReadAllTextAsync(razorFileName);
        var className = Path.GetFileNameWithoutExtension(razorFileName);

        // Step 2: Add localizer injection                                                                                                                      
        var newRazorContent = AddLocalizerInjection(razorContent, razorFileName);

        // Step 3: Search localizable strings and replace them with localizer calls
        newRazorContent = ProcessCustomActions(_customActions, newRazorContent, className);

        // Step 4: Write the modified razor content back to the file
        if (newRazorContent == razorContent)
        {
            _logger.LogDebug($"No changes to {razorFileName}.");
            return;
        }

        if (testMode)
        {
            _logger.LogInformation($"Changes to {razorFileName} not written to disk.");
            _logger.LogInformation(newRazorContent);
        }
        else
        {
            await _backupService.WriteAllTextWithBackup(razorFileName, newRazorContent);
        }
    }


    private static string AddLocalizerInjection(string razorContent, string razorFileName)
    {
        // Get the class name from the filename
        var className = Path.GetFileNameWithoutExtension(razorFileName);

        // Check if the localizer is already injected
        var pattern = $@"@inject\s+Microsoft.Extensions.Localization.IStringLocalizer<SharedResources>\s+D\b";
        var regex = new Regex(pattern);

        if (regex.IsMatch(razorContent)) return razorContent;

        // Prepend the localizer injection directive
        var localizerInjection = $"@inject Microsoft.Extensions.Localization.IStringLocalizer<SharedResources> D{Environment.NewLine}";
        return localizerInjection + razorContent;
    }

    public string ProcessCustomActions(CustomActions customActions, string razorContent, string className, string fileType = null)
    {
        customActions.SetVariable("className", className);

        foreach (var customAction in customActions.Actions.Where(a => fileType == null || a.FileType == fileType))
        {
            var componentTypePattern = customAction.Regex();
            var componentTypeRegex = new Regex(componentTypePattern, RegexOptions.Singleline);
            razorContent = componentTypeRegex.Replace(razorContent, match =>
            {
                var originalValue = match.Value;
                // Call custom action to modify attribute dictionary
                var modifiedTag = customAction.Action(match);
                // check if the tag has been modified
                if (modifiedTag != originalValue)
                    _logger.LogDebug($"Replace: {originalValue} -> {modifiedTag}");
                return modifiedTag;
            });
        }

        return razorContent;
    }

    private static string GetProjectNamespace(ConfigurationData config)
    {
        var csprojFile = XDocument.Load(config.Project);
        XNamespace msbuild = "http://schemas.microsoft.com/developer/msbuild/2003";
        var rootNamespaceElement = csprojFile.Descendants(msbuild + "RootNamespace").FirstOrDefault();

        //Construct relative project namespace from project file folder and resource file folder
        var projectFolder = Path.GetDirectoryName(config.Project);
        var resourceFolder = Path.GetDirectoryName(config.ResourcePath);
        var relativeFolder = Path.GetRelativePath(projectFolder, resourceFolder);
        var nameSpace = Path.GetFileNameWithoutExtension(config.Project);
        if (rootNamespaceElement != null) nameSpace = rootNamespaceElement.Value;
        if (!string.IsNullOrEmpty(relativeFolder)) nameSpace += "." + relativeFolder.Replace(Path.DirectorySeparatorChar, '.');
        return nameSpace;
    }

    private void GenerateCsFile()
    {
        //change extension to .cs

        var csFilePath = Path.ChangeExtension(_config.ResourcePath, ".cs");
        var className = Path.GetFileNameWithoutExtension(csFilePath);
        var nameSpace = GetProjectNamespace(_config);
        AddUsingDirective(nameSpace);

        if (!File.Exists(csFilePath))
        {
            var sb = new StringBuilder();
            // add namespace    
            sb.AppendLine("namespace " + nameSpace);
            sb.AppendLine("{");
            sb.AppendLine("public class " + className);
            sb.AppendLine("{");
            sb.AppendLine("}");
            sb.AppendLine("}");

            if (!_config.TestMode)
            {
                // write stringbuilder to file
                File.WriteAllText(csFilePath, sb.ToString());
                _logger.LogInformation($"Generated: {csFilePath}:");
            }
            else
            {
                _logger.LogInformation($"Generated: {csFilePath}:");
                _logger.LogDebug(sb.ToString());
            }
        }
    }

    private void AddUsingDirective(string nameSpace)
    {
        //check if using directive already exists in _Imports.razor
        var importsFilePath = Path.Combine(Path.GetDirectoryName(_config.Project), "_Imports.razor");

        //check if _Imports.razor exists,if not, create it
        //check presence of using directive
        //if not exists append it to end of file
        if (!File.Exists(importsFilePath))
        {
            if (!_config.TestMode)
            {
                File.WriteAllText(importsFilePath, "@using " + nameSpace);
                _logger.LogInformation($"Created _Imports.razor and added @using {nameSpace}");
            }
        }
        else
        {
            var importsFileContent = File.ReadAllText(importsFilePath);
            if (!importsFileContent.Contains("@using " + nameSpace))
            {
                importsFileContent += Environment.NewLine + "@using " + nameSpace;
                if (!_config.TestMode)
                {
                    File.WriteAllText(importsFilePath, importsFileContent);
                    _logger.LogInformation($"Added @using {nameSpace} to _Imports.razor");
                }
                else
                {
                    _logger.LogInformation($"Added @using {nameSpace} to _Imports.razor");
                }
            }
        }
    }
}
