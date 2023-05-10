using System.Collections;
using System.Globalization;
using System.Resources.NetStandard;
using System.Text;
using System.Text.RegularExpressions;

namespace BlazorLocalizer;

public static class Utilities
{
    public static string  SetAttributeValue(this string tag, string attributeName, string key)
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
     
    public static string ReplaceGridColumnStrings(this string tag, ResourceKeys modelKeys)
    {
        var attributeName = "Title";
        if (DoNotReplace(tag, attributeName)) return tag;
        string className = GetClassNameFromTag(tag, "TItem");
        string property = tag.GetAttributeValue("Property");
        string key = $"{className}.{property}";
        var attributeValue = tag.GetAttributeValue(attributeName);
        modelKeys.TryAdd(key, attributeValue);
        return tag.SetAttributeValue(attributeName, key);
    }

    public static string ReplaceAttributeWithKey(this string tag, ResourceKeys modelKeys, string attributeName, string key)
    {
        if (DoNotReplace(tag, attributeName)) return tag;
        var value = GetAttributeValue(tag, attributeName);
        modelKeys.TryAdd(key, value);
        return SetAttributeValue(tag, attributeName, key);
    }

    private static bool DoNotReplace(this string tag, string attributeName)
    {
        return GetAttributeValue(tag, attributeName).StartsWith("@");
    }

    public static string GetClassNameFromTag(this string tag, string attributeName)
    {
        var value = GetAttributeValue(tag, attributeName);
        string[] parts = value.Split('.');
        return parts[parts.Length - 1];
    }

    private static string RemoveDiacritics(this string text)
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

    private static string RemoveNonAsciiCharacters(this string text)
    {
        return Regex.Replace(text, @"[^\u0000-\u007F]", string.Empty);
    }

    public static string GenerateResourceKey(this string value)
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


    public static void EnsureFolderExists(string baseFileName)
    {
        string folderPath = Path.GetDirectoryName(baseFileName);
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);
    }

    public static void CreateResxFileWithHeaders(string filePath)
    {
        using (ResXResourceWriter resxWriter = new ResXResourceWriter(filePath))
        {
            resxWriter.AddMetadata("Version", "2.0");
            resxWriter.AddMetadata("FileType", "System.Resources.ResXFileRef, System.Windows.Forms");
            resxWriter.AddMetadata("Writer", "System.Resources.ResXResourceWriter, System.Windows.Forms");
            resxWriter.AddMetadata("Reader", "System.Resources.ResXResourceReader, System.Windows.Forms");

            resxWriter.Generate();
        }
    }

    public static Dictionary<string, string> GetExistingResources(string fileName)
    {
        var existingResources = new Dictionary<string, string>();
        fileName = Path.ChangeExtension(fileName,".resx");
        if (File.Exists(fileName))
        {
            using (ResXResourceReader resxReader = new ResXResourceReader(fileName))
            {
                foreach (DictionaryEntry entry in resxReader)
                {
                    if (entry.Value != null && !existingResources.ContainsKey(entry.Key.ToString()))
                        existingResources.Add(entry.Key.ToString(), entry.Value.ToString());
                }
            }
        }
        return existingResources;
    }

    public static void WriteResourcesToFile(Dictionary<string, string> resources, string fileName, string language = "")
    {
        fileName = Path.ChangeExtension(fileName, language == "" ? ".resx" : $".{language}.resx");
        using (ResXResourceWriter resxWriter = new ResXResourceWriter(fileName))
        {
            foreach (var resource in resources)
                resxWriter.AddResource(resource.Key, resource.Value);
            resxWriter.Generate();
        }
    }
    
    // if text contains one word split it by CamelCase
    public static string SplitCamelCase(this string text)
    {
        if (text.Contains(" ")) return text;
        return Regex.Replace(text, "(?<=[a-z])([A-Z])", " $1", RegexOptions.Compiled).Trim();
    }
}
