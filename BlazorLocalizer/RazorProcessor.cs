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
    private readonly bool testMode;

    public RazorProcessor(ILogger<RazorProcessor> logger, ConfigurationData config, CustomActions customActions, ResourceKeys modelKeys, ResourceGenerator resourceGenerator)
    {
        _logger = logger;
        _config = config;
        _customActions = customActions;
        _resourceKeys = modelKeys;
        _resourceGenerator = resourceGenerator;
        testMode = config.TestMode;
    }

    public async Task Localize()
    {
        string formClassTItem = null;

        //if resource at modelPath exists, load it  
        if (File.Exists(_config.ResourcePath))
        {
            var doc = new XmlDocument();
            doc.Load(_config.ResourcePath);
            var elemList = doc.GetElementsByTagName("data");
            foreach (XmlNode node in elemList)
            {
                _resourceKeys.TryAdd(node.Attributes["name"].Value, node.InnerText);
            }
        }

        //get folderPath folder name  from config.ProjectPath project file name
        string folderPath = Path.GetDirectoryName(_config.ProjectPath);

        // recurse through the directory folderPath   
        if (Directory.Exists(folderPath))
        {
            string[] files = Directory.GetFiles(folderPath, _config.IncludeFiles, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (_config.ExcludeFiles.Contains(Path.GetFileName(file)))
                {
                    continue;
                }

                formClassTItem = null;
                _logger.LogInformation("Processing file: " + file);
                await ProcessRazorFile(file);
            }
        }

        if (_resourceKeys.Count > 0)
        {
            _resourceGenerator.CreateResxFile(_resourceKeys);
            GenerateCsFile();
            await _resourceGenerator.TranslateResourceFile();
        }
    }

    private async Task ProcessRazorFile(string razorFileName)
    {
        // Step 1: Open razor file
        string razorContent = await File.ReadAllTextAsync(razorFileName);
        string className = Path.GetFileNameWithoutExtension(razorFileName);

        // Step 2: Add localizer injection                                                                                                                      
        string newRazorContent = AddLocalizerInjection(razorContent, razorFileName);

        // Step 3: Search localizable strings and replace them with localizer calls
        newRazorContent = ProcessCustomActions(_customActions, newRazorContent, className);

        // Step 4: Write the modified razor content back to the file
        if (testMode)
        {
            _logger.LogInformation($"Changes to {razorFileName} not written to disk.");
            _logger.LogInformation(newRazorContent);
        }
        else
        {
            await File.WriteAllTextAsync(razorFileName, newRazorContent);
        }
    }


    private static string AddLocalizerInjection(string razorContent, string razorFileName)
    {
        // Get the class name from the filename
        string className = Path.GetFileNameWithoutExtension(razorFileName);

        // Check if the localizer is already injected
        string pattern = $@"@inject\s+Microsoft.Extensions.Localization.IStringLocalizer<SharedResources>\s+D\b";
        Regex regex = new Regex(pattern);

        if (regex.IsMatch(razorContent))
        {
            return razorContent;
        }

        // Prepend the localizer injection directive
        string localizerInjection = $"@inject Microsoft.Extensions.Localization.IStringLocalizer<SharedResources> D{Environment.NewLine}";
        return localizerInjection + razorContent;
    }


    private static string ReplaceLocalizableStrings(Dictionary<string, string> resourceKeys, string razorContent, string className)
    {
        // Find attributes ending with Text or Title
        string attributePattern = @"(?<=(Text|Title)\s*=\s*""(?![^""]*@))([^""]*)";
        Regex attributeRegex = new Regex(attributePattern);

        // Handle attribute values
        razorContent = attributeRegex.Replace(razorContent, match =>
        {
            string key = $"{className}.{match.Value.GenerateResourceKey()}";
            resourceKeys.TryAdd(key, match.Value);
            return $"@D[\"{key}\"]";
        });

        // Find text content between tags, e.g., <PageTitle>Measurement Files</PageTitle>
        string tagContentPattern = @"(?<=<\w+>(?![^<]*?@)[^<]*?)\b(?:\w+\s*)+\b(?=[^>]*</\w+>)";

        Regex tagContentRegex = new Regex(tagContentPattern);

        // Handle text content between tags
        razorContent = tagContentRegex.Replace(razorContent, match =>
        {
            string trimmedValue = match.Value.Trim();
            if (string.IsNullOrEmpty(trimmedValue))
            {
                return match.Value;
            }

            string key = $"{className}.{trimmedValue.GenerateResourceKey()}";

            if (!resourceKeys.ContainsKey(key))
            {
                resourceKeys.Add(key, trimmedValue);
            }

            return $"@D[\"{key}\"]";
        });

        return razorContent;
    }

    public string ProcessCustomActions(CustomActions customActions, string razorContent, string className)
    {
        customActions.SetVariable("className", className);

        foreach (var customAction in customActions.Actions)
        {
            string componentTypePattern = customAction.Regex();
            Regex componentTypeRegex = new Regex(componentTypePattern, RegexOptions.Singleline);
            razorContent = componentTypeRegex.Replace(razorContent, match =>
            {
                // Call custom action to modify attribute dictionary
                string modifiedTag = customAction.Action(match.Value);
                // check if the tag has been modified
                if (modifiedTag != match.Value)
                    _logger.LogDebug($"Replace: {match.Groups[0].Value} -> {modifiedTag}");
                return modifiedTag;
            });
        }
        return razorContent;
    }


    private static string GetProjectNamespace(ConfigurationData config)
    {
        XDocument csprojFile = XDocument.Load(config.ProjectPath);
        XNamespace msbuild = "http://schemas.microsoft.com/developer/msbuild/2003";
        XElement rootNamespaceElement = csprojFile.Descendants(msbuild + "RootNamespace").FirstOrDefault();

        //Construct relative project namespace from project file folder and resource file folder
        var projectFolder = Path.GetDirectoryName(config.ProjectPath);
        var resourceFolder = Path.GetDirectoryName(config.ResourcePath);
        var relativeFolder = Path.GetRelativePath(projectFolder, resourceFolder);
        string nameSpace = Path.GetFileNameWithoutExtension(config.ProjectPath);
        if (rootNamespaceElement != null)
        {
            nameSpace = rootNamespaceElement.Value;
        }

        if (!string.IsNullOrEmpty(relativeFolder)) nameSpace += "." + relativeFolder.Replace(Path.DirectorySeparatorChar, '.');
        return nameSpace;
    }

    private void GenerateCsFile()
    {
        //change extension to .cs

        string csFilePath = Path.ChangeExtension(_config.ResourcePath, ".cs");

        if (!File.Exists(csFilePath))
        {
            string className = Path.GetFileNameWithoutExtension(csFilePath);
            string nameSpace = GetProjectNamespace(_config);

            StringBuilder sb = new StringBuilder();
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

            AddUsingDirective(nameSpace);
        }
    }

    private void AddUsingDirective(string nameSpace)
    {
        //check if using directive already exists in _Imports.razor
        string importsFilePath = Path.Combine(Path.GetDirectoryName(_config.ProjectPath), "_Imports.razor");

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
            string importsFileContent = File.ReadAllText(importsFilePath);
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
