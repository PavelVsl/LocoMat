using System.Collections;
using System.Globalization;
using System.IO;
using System.Resources.NetStandard;
using System.Text;

namespace BlazorLocalizer
{
    public static class ResourceGenerator
    {
        public static async Task CreateResxFile(Dictionary<string, string> resourceKeys, string baseFileName)
        {
            EnsureFolderExists(baseFileName);

            string filePath = baseFileName + ".resx";
            var existingResources = GetOrCreateResxFile(filePath);

            foreach (var resource in resourceKeys)
                existingResources.TryAdd(resource.Key, resource.Value);

            WriteResourcesToFile(existingResources, filePath);
            GenerateCsFile(baseFileName);
        }

        public static async Task TranslateResourceFile(string baseFileName, string languages)
        {
            string filePath = baseFileName + ".resx";
            var existingResources = GetExistingResources(filePath);

            foreach (string languageCode in languages.Split(','))
            {
                CultureInfo cultureInfo = CultureInfo.GetCultureInfo(languageCode.Trim());
                string translatedFilePath = baseFileName + "." + languageCode.Trim() + ".resx";

                var translatedResources = GetOrCreateResxFile(translatedFilePath);

                foreach (var resource in existingResources)
                {
                    if (!translatedResources.ContainsKey(resource.Key))
                        translatedResources.Add(resource.Key, await resource.Value.Translate(languageCode));
                }

                WriteResourcesToFile(translatedResources, translatedFilePath);
    	    }
        }

        private static void EnsureFolderExists(string baseFileName)
        {
            string folderPath = Path.GetDirectoryName(baseFileName);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
        }

        private static Dictionary<string, string> GetOrCreateResxFile(string filePath)
        {
            if (!File.Exists(filePath))
                CreateResxFileWithHeaders(filePath);

            return GetExistingResources(filePath);
        }

        private static void CreateResxFileWithHeaders(string filePath)
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

        public static Dictionary<string, string> GetExistingResources(string filePath)
        {
            var existingResources = new Dictionary<string, string>();

            if (File.Exists(filePath))
            {
                using (ResXResourceReader resxReader = new ResXResourceReader(filePath))
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

        private static void WriteResourcesToFile(Dictionary<string, string> resources, string filePath)
        {
            using (ResXResourceWriter resxWriter = new ResXResourceWriter(filePath))
            {
                foreach (var resource in resources)
                    resxWriter.AddResource(resource.Key, resource.Value);

                resxWriter.Generate();
    	    }
        }

        private static void GenerateCsFile(string baseFileName)
        {
            string csFilePath = baseFileName + ".cs";

            if (!File.Exists(csFilePath))
            {
                string className = Path.GetFileNameWithoutExtension(baseFileName);
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("public class " + className);
                sb.AppendLine("{");
                sb.AppendLine("}");

                File.WriteAllText(csFilePath, sb.ToString());
            }
        }
    }
}
