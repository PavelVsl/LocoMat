using System.Collections;
using System.Globalization;
using System.IO;
using System.Resources.NetStandard;
using System.Text;
using System.Xml.Linq;

namespace BlazorLocalizer
{
    public static class ResourceGenerator
    {
        public static async Task CreateResxFile(Dictionary<string, string> resourceKeys, ConfigurationData config)
        {
            EnsureFolderExists(config.ResourcePath);

            string filePath = config.ResourcePath;
            var existingResources = GetOrCreateResxFile(filePath);

            foreach (var resource in resourceKeys)
                existingResources.TryAdd(resource.Key, resource.Value);

            if (!config.TestMode)
            {
                WriteResourcesToFile(existingResources, filePath);
                Console.WriteLine($"Created resource: {filePath}");
            }                                                          
        }

        public static async Task TranslateResourceFile(ConfigurationData config)
        {
            //Check if languages are not empty
            if (string.IsNullOrEmpty(config.TargetLanguages)) return;

            //Check if languages are valid, if not write warning and return
            foreach (string languageCode in config.TargetLanguages.Split(','))
            {
                if (CultureInfo.GetCultures(CultureTypes.AllCultures).All(c => c.Name != languageCode.Trim()))
                {
                    Console.WriteLine($"Warning: {languageCode.Trim()} is not a valid language code.");
                    return;
                }
            }

            string baseFileName = config.ResourcePath;
            string filePath = baseFileName + ".resx";
            var existingResources = GetExistingResources(filePath);

            foreach (string languageCode in config.TargetLanguages.Split(','))
            {
                string translatedFilePath = baseFileName + "." + languageCode.Trim() + ".resx";

                var translatedResources = GetOrCreateResxFile(translatedFilePath);

                foreach (var resource in existingResources)
                {
                    if (!translatedResources.ContainsKey(resource.Key))
                    {
                        string translate;
                        if (config.TestMode)
                            translate = resource.Value;
                        else
                            translate = await resource.Value.Translate(languageCode);
                        translatedResources.Add(resource.Key, translate);
                        if (config.VerboseOutput)
                        {
                            Console.WriteLine($"Translated {resource.Key}: {resource.Value} -> {translate}");
                        }
                    }
                }

                if (!config.TestMode)
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

        static string GetProjectNamespace(ConfigurationData config)
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

        public static void GenerateCsFile(ConfigurationData config)
        {
            //change extension to .cs

            string csFilePath = Path.ChangeExtension(config.ResourcePath, ".cs");

            if (!File.Exists(csFilePath))
            {
                string className = Path.GetFileNameWithoutExtension(csFilePath);
                string nameSpace = GetProjectNamespace(config);

                StringBuilder sb = new StringBuilder();
                // add namespace    
                sb.AppendLine("namespace " + nameSpace);
                sb.AppendLine("{");
                sb.AppendLine("public class " + className);
                sb.AppendLine("{");
                sb.AppendLine("}");
                sb.AppendLine("}");

                if (!config.TestMode)
                {
                    // write stringbuilder to file
                    File.WriteAllText(csFilePath, sb.ToString());
                    Console.WriteLine($"Generated: {csFilePath}:");
                }
                else
                {
                    Console.WriteLine($"Generated: {csFilePath}:");
                    Console.WriteLine(sb.ToString());
                }
                AddUsingDirective(config,nameSpace);
            }
        }

        public static void AddUsingDirective(ConfigurationData config, string nameSpace)
        {
            //check if using directive already exists in _Imports.razor
            string importsFilePath = Path.Combine(Path.GetDirectoryName(config.ProjectPath), "_Imports.razor");

            //check if _Imports.razor exists,if not, create it
            //check presence of using directive
            //if not exists append it to end of file
            if (!File.Exists(importsFilePath))
            {
                if (!config.TestMode)
                {
                    File.WriteAllText(importsFilePath, "@using " + nameSpace);
                    Console.WriteLine($"Created _Imports.razor and added @using {nameSpace}");
                }
            }
            else
            {
                string importsFileContent = File.ReadAllText(importsFilePath);
                if (!importsFileContent.Contains("@using " + nameSpace))
                {
                    importsFileContent += Environment.NewLine + "@using " + nameSpace;
                    if (!config.TestMode)
                    {
                        File.WriteAllText(importsFilePath, importsFileContent);
                        Console.WriteLine($"Added @using {nameSpace} to _Imports.razor");
                    }
                    else
                    {
                        Console.WriteLine($"Added @using {nameSpace} to _Imports.razor");
                    }
                }
            }
        }
    }
}
