using System.Collections;
using System.Globalization;
using System.Resources.NetStandard;

namespace BlazorLocalizer;

public static class ResourceGenerator
{
    // Create a RESX file with the specified file name and add the resource keys and values to it.
    // Ignore existing keys and add only new keys.
    // Create a valid RESX file with correct headers in a separate method.
    public static async Task CreateResxFile(Dictionary<string, string> resourceKeys, string baseFileName)
    {
        string filePath = baseFileName + ".resx";
        if (!File.Exists(filePath))
        {
            CreateResxFile(filePath);
        }

        // Get existing resources from the file
        Dictionary<string, string> existingResources = GetExistingResources(filePath);

        // Add new resources to the existing resources
        foreach (KeyValuePair<string, string> resource in resourceKeys)
        {
            existingResources.TryAdd(resource.Key, resource.Value);
        }

        // Write the updated resources to the file
        WriteResourcesToFile(existingResources, filePath);
    }

    // Translate a RESX file and save the translated file.
    // The languages parameter is a comma-delimited list of languages to translate.
    // Check if target file exists and if it does, load it and translate only missing keys.
    // If it doesn't exist, create it and translate all keys.
    // The Translate extension method is assumed to exist and to take a string and a CultureInfo object as arguments.
    public static async Task TranslateResourceFile(string baseFileName, string languages)
    {
        string filePath = baseFileName + ".resx";
        Dictionary<string, string> existingResources = GetExistingResources(filePath);

        foreach (string languageCode in languages.Split(','))
        {
            CultureInfo cultureInfo = CultureInfo.GetCultureInfo(languageCode.Trim());
            string translatedFilePath = baseFileName + "." + languageCode.Trim() + ".resx";

            if (!File.Exists(translatedFilePath))
            {
                // If the translated file does not exist, create it and translate all keys
                Dictionary<string, string> translatedResources = new Dictionary<string, string>();
                foreach (KeyValuePair<string, string> resource in existingResources)
                {
                    translatedResources.Add(resource.Key, await resource.Value.Translate(languageCode));
                }

                WriteResourcesToFile(translatedResources, translatedFilePath);
            }
            else
            {
                // If the translated file exists, load it and translate only missing keys
                Dictionary<string, string> translatedResources = GetExistingResources(translatedFilePath);
                foreach (KeyValuePair<string, string> resource in existingResources)
                {
                    if (!translatedResources.ContainsKey(resource.Key))
                    {
                        translatedResources.Add(resource.Key, await resource.Value.Translate(languageCode));
                    }
                }

                WriteResourcesToFile(translatedResources, translatedFilePath);
            }
        }
    }

    private static void CreateResxFile(string filePath)
    {
        // Create a new, empty RESX file.
        using (ResXResourceWriter resxWriter = new ResXResourceWriter(filePath))
        {
            // Add the default headers to the RESX file.
            resxWriter.AddMetadata("Version", "2.0");
            resxWriter.AddMetadata("FileType", "System.Resources.ResXFileRef, System.Windows.Forms");
            resxWriter.AddMetadata("Writer", "System.Resources.ResXResourceWriter, System.Windows.Forms");
            resxWriter.AddMetadata("Reader", "System.Resources.ResXResourceReader, System.Windows.Forms");

            // Save the RESX file.
            resxWriter.Generate();
        }
    }

// Get the existing resources from a RESX file and return them as a dictionary.
    public static Dictionary<string, string> GetExistingResources(string filePath)
    {
        Dictionary<string, string> existingResources = new Dictionary<string, string>();
        if (File.Exists(filePath))
        {
            using (ResXResourceReader resxReader = new ResXResourceReader(filePath))
            {
                foreach (DictionaryEntry entry in resxReader)
                {
                    if (entry.Value != null && !existingResources.ContainsKey(entry.Key.ToString()))
                    {
                        existingResources.Add(entry.Key.ToString(), entry.Value.ToString());
                    }
                }
            }
        }
        return existingResources;
    }

// Write the specified resources to a RESX file.
    private static void WriteResourcesToFile(Dictionary<string, string> resources, string filePath)
    {
        using (ResXResourceWriter resxWriter = new ResXResourceWriter(filePath))
        {
            foreach (KeyValuePair<string, string> resource in resources)
            {
                resxWriter.AddResource(resource.Key, resource.Value);
            }

            resxWriter.Generate();
        }
    }
}
