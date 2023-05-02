using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json.Converters;

namespace BlazorLocalizer;

public static class RazorProcessor
{
    public static string SetAttributeValue(this string tag, string attributeName, string key)
    {
        // Find attributes ending with Text or Title
        string attributePattern = @$"(?<=({attributeName})\s*=\s*""(?![^""]*@))([^""]*)";
        Regex attributeRegex = new Regex(attributePattern);
        // Handle attribute values
        var newTag = attributeRegex.Replace(tag, match => $"@D[\"{key}\"]");
        return newTag;
    }

    public static string GetAttributeValue(this string tag, string attributeName)
    {
        // Find attributes ending with Text or Title
        string attributePattern = @$"(?<={attributeName}\s*=\s*"")([^""]*)";
        Regex attributeRegex = new Regex(attributePattern);
        // Handle attribute values
        var newTag = attributeRegex.Match(tag).Value;
        return newTag;
    }

    public static async Task ProcessRazorFile(string razorFileName, Dictionary<string, string> modelKeys, List<(string ComponentType, Func<string, string> CustomAction)> customActions, string modelPath, bool testMode)
    {
        // Step 1: Open razor file
        string razorContent = await File.ReadAllTextAsync(razorFileName);
        string className = Path.GetFileNameWithoutExtension(razorFileName);

        // Step 2: Add localizer injection                                                                                                                      
        string newRazorContent = AddLocalizerInjection(razorContent, razorFileName);

        // Step 3: Search localizable strings and replace them with localizer calls
        newRazorContent = ReplaceTagAttributes(customActions, newRazorContent);
        newRazorContent = ReplaceLocalizableStrings(modelKeys, newRazorContent, className);

        // Step 4: Write the modified razor content back to the file
        if (testMode)
        {
            Console.WriteLine($"Changes to {razorFileName} not written to disk.");
            Console.WriteLine(newRazorContent);
        }
        else
        {
            await File.WriteAllTextAsync(razorFileName, newRazorContent);
        }
    }

    public static string ReplaceGridColumnStrings(string tag, Dictionary<string, string> modelKeys)
    {
        var attributeName = "Title";
        if (DoNotReplace(tag, attributeName)) return tag;
        string className = GetClassNameFromTag(tag, "TItem");
        string property = GetAttributeValue(tag, "Property");
        string key = $"{className}.{property}";
        modelKeys.TryAdd(key, GetAttributeValue(tag, attributeName));
        return SetAttributeValue(tag, attributeName, key);
    }

    public static string ReplaceAttributeWithKey(this string tag, Dictionary<string, string> modelKeys, string attributeName, string key)
    {
        if (DoNotReplace(tag, attributeName)) return tag;
        var value = GetAttributeValue(tag, attributeName);
        modelKeys.TryAdd(key, value);
        return SetAttributeValue(tag, attributeName, key);
    }

    private static bool DoNotReplace(string tag, string attributeName)
    {
        return GetAttributeValue(tag, attributeName).StartsWith("@");
    }

    public static string GetClassNameFromTag(string tag, string attributeName)
    {
        var value = GetAttributeValue(tag, attributeName);
        string[] parts = value.Split('.');
        return parts[parts.Length - 1];
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
            string key = $"{className}.{GenerateResourceKey(match.Value)}";
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

            string key = $"{className}.{GenerateResourceKey(trimmedValue)}";

            if (!resourceKeys.ContainsKey(key))
            {
                resourceKeys.Add(key, trimmedValue);
            }

            return $"@D[\"{key}\"]";
        });

        return razorContent;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string RemoveNonAsciiCharacters(string text)
    {
        return Regex.Replace(text, @"[^\u0000-\u007F]", string.Empty);
    }

    private static string GenerateResourceKey(string value)
    {
        // Remove diacritics (accents)
        value = RemoveDiacritics(value);
        // Remove any remaining non-ASCII characters or invalid characters
        value = RemoveNonAsciiCharacters(value);
        // Replace any remaining whitespace with an underscore
        value = value.Replace(" ", "");
        // Remove any remaining non-word characters
        value = Regex.Replace(value, @"[^\w]", "");
        // Remove any remaining underscores
        value = value.Replace("_", "");
        // Remove leading and trailing underscores
        value = value.Trim('_');
        // Convert to lowercase
        value = value.ToLowerInvariant();
        // Truncate to 40 characters
        value = value.Substring(0, Math.Min(value.Length, 40));
        // Return the resulting string as the resource key.
        return value;
    }

    public static string ReplaceTagAttributes(List<(string ComponentType, Func<string, string> CustomAction)> customActions, string razorContent)
    {
        foreach (var customAction in customActions)
        {
            string componentTypePattern = $@"<(?<tag>{customAction.ComponentType})(\s+(?<attr>\S+?)(=""(?<value>.*?)""|$))+\s*/?>";
            Regex componentTypeRegex = new Regex(componentTypePattern, RegexOptions.Singleline);
            razorContent = componentTypeRegex.Replace(razorContent, match =>
            {
                //Console.WriteLine($"ComponentMatch: {match.Groups[0].Value}");
                // Call custom action to modify attribute dictionary
                string modifiedTag = customAction.CustomAction(match.Value);
                return modifiedTag;
            });
        }

        return razorContent;
    }

    public static async Task Localize(ConfigurationData config)
    {
        Dictionary<string, string> modelKeys = new Dictionary<string, string>();
        string formClassTItem = null;
        List<(string ComponentType, Func<string, string> CustomAction)> customActions = new List<(string ComponentType, Func<string, string> CustomAction)>
        {
            ("RadzenTemplateForm", (tag) =>
            {
                formClassTItem = GetClassNameFromTag(tag, "TItem");
                return tag;
            }),
            ("RadzenDropDownDataGridColumn", tag => ReplaceGridColumnStrings(tag, modelKeys)),
            ("RadzenDataGridColumn", tag => ReplaceGridColumnStrings(tag, modelKeys)),
            ("RadzenLabel", tag =>
            {
                var key = $"{formClassTItem}.{tag.GetAttributeValue("Component")}";
                return tag.ReplaceAttributeWithKey(modelKeys, "Text", key);
            }),
            ("RadzenRequiredValidator", tag =>
            {
                var component = tag.GetAttributeValue("Component");
                var key = $"{formClassTItem}.{component}.RequiredValidator";
                return tag.ReplaceAttributeWithKey(modelKeys, "Text", key);
            }),
            ("RadzenButton", tag =>
            {
                var text = tag.GetAttributeValue("Text");
                var key = $"Button.{text}";
                return tag.ReplaceAttributeWithKey(modelKeys, "Text", key);
            }),
            ("RadzenPanelMenuItem", tag =>
                {
                    var text = tag.GetAttributeValue("Text");
                    var key = $"Menu.{text}";
                    return tag.ReplaceAttributeWithKey(modelKeys, "Text", key);
                }
            ),
        };
        //if resource at modelPath exists, load it  
        if (File.Exists(config.ResourcePath))
        {
            var doc = new XmlDocument();
            doc.Load(config.ResourcePath);
            var elemList = doc.GetElementsByTagName("data");
            foreach (XmlNode node in elemList)
            {
                modelKeys.TryAdd(node.Attributes["name"].Value, node.InnerText);
            }
        }

        //get folderPath folder name  from config.ProjectPath project file name
        string folderPath = Path.GetDirectoryName(config.ProjectPath);

        // recurse through the directory folderPath   
        if (Directory.Exists(folderPath))
        {
            string[] files = Directory.GetFiles(folderPath, config.IncludeFiles, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (config.ExcludeFiles.Contains(Path.GetFileName(file)))
                {
                    continue;
                }

                formClassTItem = null;
                Console.WriteLine("Processing file: " + file);
                await ProcessRazorFile(file, modelKeys, customActions, config.ResourcePath, config.TestMode);
            }
        }

        if (modelKeys.Count > 0)
        {
            ResourceGenerator.GenerateCsFile(config);
            await ResourceGenerator.CreateResxFile(modelKeys, config);
            await ResourceGenerator.TranslateResourceFile(config);
        }
    }
}
