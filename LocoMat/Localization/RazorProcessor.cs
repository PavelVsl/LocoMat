using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace LocoMat;

public class RazorProcessor
{
    private readonly ConfigurationData _config;
    private readonly CustomActions _customActions;
    private readonly ILogger<RazorProcessor> _logger;
    private readonly bool testMode;
    private BackupService _backupService;

    public RazorProcessor(
        ILogger<RazorProcessor> logger,
        ConfigurationData config,
        CustomActions customActions,
        BackupService backupService
    )
    {
        _logger = logger;
        _config = config;
        _customActions = customActions;
        _backupService = backupService;
        testMode = config.TestMode;
    }

    public async Task ProcessRazorFile(string razorFileName)
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
            _logger.LogDebug("No changes to {RazorFileName}", razorFileName);
            return;
        }

        if (testMode)
        {
            _logger.LogInformation("Changes to {RazorFileName} not written to disk in test mode", razorFileName);
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

    public string ProcessCustomActions(
        CustomActions customActions,
        string razorContent,
        string className,
        string fileType = null
    )
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
                    _logger.LogDebug("Replace: {OriginalValue} -> {ModifiedTag}", originalValue, modifiedTag);
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
        var resourceFolder = Path.GetDirectoryName(config.Resource);
        var relativeFolder = Path.GetRelativePath(projectFolder, resourceFolder);
        var nameSpace = Path.GetFileNameWithoutExtension(config.Project);
        if (rootNamespaceElement != null) nameSpace = rootNamespaceElement.Value;
        if (!string.IsNullOrEmpty(relativeFolder)) nameSpace += "." + relativeFolder.Replace(Path.DirectorySeparatorChar, '.');
        return nameSpace;
    }

    internal void GenerateResourceStubFile()
    {
        //change extension to .cs

        var csFilePath = Path.ChangeExtension(_config.Resource, ".cs");
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
                    _logger.LogInformation("Added @using {NameSpace} to _Imports.razor", nameSpace);
                }
                else
                {
                    _logger.LogInformation("Added @using {NameSpace} to _Imports.razor", nameSpace);
                }
            }
        }
    }
}
