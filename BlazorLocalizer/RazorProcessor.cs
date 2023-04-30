using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace BlazorRazorLocalizer;

public static class RazorProcessor
{
    //public const string attributePattern = @"(?<name>\S+)\s*=\s*""(?<value>(?:(?<!\\)""[^""]*?(?<!\\)"")|(?<bracket>@@)|(?<at>@@?@[^""\s>]+?(?<!\\))|(?<plain>[^""]+))""";
    //public const string attributePattern = @"(?<name>\S+)\s*=\s*""(?<value>[^""]*)""";
    
    // public const string attributePattern = @"(?<name>\S+)\s*=\s*""(?<value>[^""]*|[^""]*=>[^""]*)""";
    // public const string attributePattern = @"(?<name>\S+)\s*=\s*""(?<value>[^""\\]*(?:\\.[^""\\]*)*)""";
     //public const string attributePattern = @"(?<name>\S+)\s*=\s*""(?<value>[^""]*|@""[^""]*""|[^""]*=>[^""]*)""";
    public const string attributePattern = @"(?<name>\S+)\s*=\s*""(?<value>(?:\\.|(?!\\"").)*)""";
    // public const string attributePattern = @"(?<name>\S+)\s*=\s*(""(?<value>(?:\\.|(?!\\"").)*)""|(?<value>@\(.*\)))";
    //public const string attributePattern = @"(?<name>\S+)\s*=\s*(""(?<value>(?:\\.|(?!\\"").)*)""|'(?<value>(?:\\.|(?!\\').)*))";
    // public const string attributePattern = @"(?<name>\S+)\s*=\s*""(?<value>[^""]*)""";
    // public const string attributePattern = @"(?<name>\S+)\s*=\s*""(?<value>[^""]*|[^""]*=>[^""]*)""";
    // public const string attributePattern = @"(?<name>\S+)\s*=\s*""(?<value>[^""\\]*(?:\\.[^""\\]*)*)""";
    // public const string attributePattern = @"(?<name>\S+)\s*=\s*""(?<value>[^""]*|@""[^""]*""|[^""]*=>[^""]*)""";


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
    
    public static async Task ProcessRazorFile(Dictionary<string, string> modelKeys, string razorFileName, string modelPath, List<(string ComponentType, Func<string, string> CustomAction)> customActions)
    {
        // Step 1: Open razor file
        string razorContent = File.ReadAllText(razorFileName);
        string className = Path.GetFileNameWithoutExtension(razorFileName);

        // Step 2: Add localizer injection                                                                                                                      
        string newRazorContent = AddLocalizerInjection(razorContent, razorFileName);

        // Step 3: Search localizable strings and replace them with localizer calls
        newRazorContent = ReplaceTagAttributes(customActions, newRazorContent);
        newRazorContent = ReplaceLocalizableStrings(modelKeys, newRazorContent, className);


        // Step 4: Write the modified razor content back to the file
        File.WriteAllText(razorFileName, newRazorContent);

        // Step 5: Make model keys and create resx file
        await ResourceGenerator.CreateResxFile(modelKeys, modelPath);
        //await CreateResxFile(modelKeys, modelPath, "cs-CZ");
    }

    public static string  ReplaceGridColumnStrings(string tag, Dictionary<string, string> modelKeys)
    {
        var attributeName= "Title";
        if (DoNotReplace(tag, attributeName)) return tag;
        string className = GetClassNameFromTag(tag,"TItem");
        string property = GetAttributeValue(tag,"Property");
        string key = $"{className}.{property}";
        modelKeys.TryAdd(key, GetAttributeValue(tag,attributeName));
        return SetAttributeValue(tag, attributeName, key);
    }

    public static string ReplaceAttributeWithKey(this string tag,Dictionary<string, string> modelKeys,  string attributeName, string key)
    {
        if (DoNotReplace(tag, attributeName)) return tag;
        var value = GetAttributeValue(tag, attributeName);
        modelKeys.TryAdd(key, value);
        return  SetAttributeValue(tag, attributeName, key);
    }

    private static bool DoNotReplace(string tag,string attributeName)
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
        string pattern = $@"@inject\s+IStringLocalizer<SharedResources>\s+D\b";
        Regex regex = new Regex(pattern);

        if (regex.IsMatch(razorContent))
        {
            return razorContent;
        }

        // Prepend the localizer injection directive
        string localizerInjection = $"@inject IStringLocalizer<SharedResources> D{Environment.NewLine}";
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

    private static string GenerateResourceKey(string value)
    {
        // Implement your resource key generation logic here.
        // For simplicity, we'll just return the same value without spaces.
        return value.Replace(" ", "");
    }

    public static string ReplaceTagAttributes(List<(string ComponentType, Func<string,string> CustomAction)> customActions, string razorContent)
    {
        foreach (var customAction in customActions)
        {
            string componentTypePattern = $@"<(?<tag>{customAction.ComponentType})(\s+(?<attr>\S+?)(=""(?<value>.*?)""|$))+\s*/?>";
            Regex componentTypeRegex = new Regex(componentTypePattern, RegexOptions.Singleline);
            razorContent = componentTypeRegex.Replace(razorContent, match =>
            {
                Console.WriteLine($"ComponentMatch: {match.Groups[0].Value}");
                // Call custom action to modify attribute dictionary
                string modifiedTag = customAction.CustomAction(match.Value);
                return modifiedTag;
            });
        }
        return razorContent;
    }
}
