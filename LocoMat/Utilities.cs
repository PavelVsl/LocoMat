using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources.NetStandard;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using LocoMat.Translation;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LocoMat;

public static class Utilities
{
    public static string SetAttributeValue(this string tag, string attributeName, string key)
    {
        // Find attributes ending with Text or Title
        var attributePattern = @$"(?<=({attributeName})\s*=\s*""(?![^""]*@))([^""]*)";
        var attributeRegex = new Regex(attributePattern);
        // Handle attribute values
        var newTag = attributeRegex.Replace(tag, match => $"@D[\"{key}\"]");
        return newTag;
    }

    public static string GetAttributeValue(this string tag, string attributeName)
    {
        // Find attributes ending with Text or Title
        var attributePattern = @$"(?<={attributeName}\s*=\s*"")([^""]*)";
        var attributeRegex = new Regex(attributePattern);
        // Handle attribute values
        var newTag = attributeRegex.Match(tag).Value;
        return newTag;
    }

    public static string ReplaceGridColumnStrings(this string tag, ResourceKeys modelKeys)
    {
        var attributeName = "Title";
        if (DoNotReplace(tag, attributeName)) return tag;
        var className = GetClassNameFromTag(tag, "TItem");
        var property = tag.GetAttributeValue("Property");
        var key = $"{className}.{property}";
        var attributeValue = tag.GetAttributeValue(attributeName);
        modelKeys.TryAdd(key, attributeValue);
        return tag.SetAttributeValue(attributeName, key);
    }

    public static string ReplaceAttributeWithKey(
        this string tag,
        ResourceKeys modelKeys,
        string attributeName,
        string key
    )
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
        var parts = value.Split('.');
        return parts[parts.Length - 1];
    }

    private static string RemoveDiacritics(this string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark) stringBuilder.Append(c);
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
        // Convert to lowercase
        value = value.ToLowerInvariant();
        // UpperCase the first character in every word
        value = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value);
        // Replace any remaining whitespace with an underscore
        value = value.Replace(" ", "");
        // Remove any remaining non-word characters
        value = Regex.Replace(value, @"[^\w]", "");
        // Remove any remaining underscores
        value = value.Replace("_", "");
        // Remove leading and trailing underscores
        value = value.Trim('_');
        // Truncate to 40 characters
        value = value.Substring(0, Math.Min(value.Length, 40));
        // Return the resulting string as the resource key.
        return value;
    }

    public static void EnsureFolderExists(string baseFileName)
    {
        var folderPath = Path.GetDirectoryName(baseFileName);
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);
    }

    public static void CreateResxFileWithHeaders(string filePath)
    {
        using (var resxWriter = new ResXResourceWriter(filePath))
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
        fileName = Path.ChangeExtension(fileName, ".resx");
        if (File.Exists(fileName))
            using (var resxReader = new ResXResourceReader(fileName))
            {
                foreach (DictionaryEntry entry in resxReader)
                    if (entry.Value != null && !existingResources.ContainsKey(entry.Key.ToString()))
                        existingResources.Add(entry.Key.ToString(), entry.Value.ToString());
            }

        return existingResources;
    }

    public static void WriteResourcesToFile(Dictionary<string, string> resources, string fileName, string language = "")
    {
        fileName = Path.ChangeExtension(fileName, language == "" ? ".resx" : $".{language}.resx");
        using (var resxWriter = new ResXResourceWriter(fileName))
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

    public static bool IsLocalizable(this PropertyInfo p)
    {
        return (p.Name.EndsWith("Text") && p.Name != "Text" && p.Name != "SearchText") ||
               p.Name == "PagingSummaryFormat";
    }

    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return false;
        // RFC 2822 compliant regex pattern for email validation
        var pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        return Regex.IsMatch(email, pattern);
    }

    public static string GetProjectFileName(string path = null)
    {
        // if path is null use current dir
        path = path ?? Directory.GetCurrentDirectory();
        //get full path
        path = Path.GetFullPath(path);
        if (Directory.Exists(path))
        {
            //if path is a directory get all csproj files in it
            var files = Directory.GetFiles(path, "*.csproj");
            if (files.Length == 1) path = files[0];
        }

        if (File.Exists(path) && IsValidCSharpProject(path)) return path;
        //if path is not a file or directory return null
        return null;
    }

    public static bool IsDirectory(string path)
    {
        //check if path is a existing directory
        if (Directory.Exists(path)) return true;

        var attr = File.GetAttributes(path);
        return (attr & FileAttributes.Directory) == FileAttributes.Directory;
    }

    private static bool IsValidCSharpProject(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        if (extension != ".csproj") return false;
        //check if file is a valid xml file and root element is Project
        var doc = XDocument.Load(fileName);
        return doc.Root?.Name.LocalName == "Project";
    }

    public static Dictionary<string, string> GetOrCreateResxFile(string fileName, string language = "")
    {
        //Ensure than filename has correct extension
        fileName = Path.ChangeExtension(fileName, string.IsNullOrEmpty(language) ? ".resx" : $".{language}.resx");
        if (!File.Exists(fileName)) CreateResxFileWithHeaders(fileName);
        return GetExistingResources(fileName);
    }

    public static string GetResourceKey(this LiteralExpressionSyntax node)
    {
        var text = node.Token.ValueText;
        var key = text.GenerateResourceKey();
        var invocationExpression = node.Ancestors().OfType<InvocationExpressionSyntax>().FirstOrDefault();
        var genericArgumentList = invocationExpression?.DescendantNodes().OfType<TypeArgumentListSyntax>().FirstOrDefault();
        var genericParameterName = genericArgumentList?.Arguments.FirstOrDefault()?.ToString();
        var classDeclaration = node.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        var className = classDeclaration?.Identifier.ToString();
        if (!string.IsNullOrEmpty(genericParameterName)) return $"{genericParameterName}.{key}";
        return $"{className}.{key}";
    }

    public static string GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
        return fvi.FileVersion;
    }
}
