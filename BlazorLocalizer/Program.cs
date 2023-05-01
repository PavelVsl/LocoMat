using System.Text.Json;
using System.Xml;
using Microsoft.Extensions.Configuration;
using static BlazorLocalizer.RazorProcessor;

namespace BlazorLocalizer
{
    class Program
    {
        static async Task Main(string[] args)
        {
// Create a configuration object and configure it with the command line arguments
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("BlazorLocalizerSettings.json", optional: true)
                .AddCommandLine(args);
            var configuration = configurationBuilder.Build();

// Get the values of the command line arguments using the configuration object
            var command = args[0] ?? configuration.GetValue<string>("command");
            var projectPath = configuration.GetValue<string>("projectPath");
            var resourcePath = configuration.GetValue<string>("resourcePath");
            var excludeFiles = configuration.GetValue<string>("excludeFiles")?.Split(",").ToList();
            var targetLanguage = configuration.GetValue<string>("targetLanguage");
            Translator.Email = configuration.GetValue<string>("email");

            switch (command)
            {
                case "localize":
                    await Localize(projectPath, resourcePath, excludeFiles);
                    break;

                case "translate":
                    await ResourceGenerator.TranslateResourceFile(resourcePath, targetLanguage);
                    break;
                case "settings":
                    // Create default settings file in current directory if it doesn't exist
                    if (!File.Exists("BlazorLocalizerSettings.json"))
                    {
                        var appSettings = new Dictionary<string, object>
                        {
                            { "Command", "help" },
                            { "ProjectPath", "./" },
                            { "ResourcePath", "./Resources/SharedResources.resx" },
                            { "ExcludeFiles", "App.razor,_Imports.razor,RedirectToLogin.razor,CulturePicker.razor" },
                            { "TargetLanguage", "cs-CZ" },
                            { "Email", "sample@email.com" }
                        };

                        var options = new JsonSerializerOptions
                        {
                            WriteIndented = true
                        };

                        var jsonString = JsonSerializer.Serialize(appSettings, options);

                        File.WriteAllText("BlazorLocalizerSettings.json", jsonString);
                        Console.WriteLine("Default settings file created: BlazorLocalizerSettings.json");
                        Console.WriteLine(jsonString);
                    }
                    else
                    {
                        Console.WriteLine("Settings file already exists: BlazorLocalizerSettings.json");
                        Console.WriteLine(File.ReadAllText("BlazorLocalizerSettings.json"));
                    }

                    break;

                case "help":
                    ConsoleHelp();
                    break;

                default:
                    Console.WriteLine("Unknown command");
                    ConsoleHelp();
                    break;
            }
        }

        private static void ConsoleHelp()
        {
            Console.WriteLine("Usage: BlazorLocalizer command parameters");
            Console.WriteLine("Commands:");
            Console.WriteLine("localize --projectPath <projectPath> --resourcePath <resourcePath> [--excludeFiles <excludeFiles>] --targetLanguage <targetLanguage> [--email <email>]");
            Console.WriteLine("translate --resourcePath <resourcePath> --targetLanguage <targetLanguage> [--email <email>]");
            Console.WriteLine("help");
        }

        private static async Task Localize(string filePath, string resourcePath, List<string> ExcludeFiles)
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
            if (File.Exists(resourcePath))
            {
                var doc = new XmlDocument();
                doc.Load(resourcePath);
                var elemList = doc.GetElementsByTagName("data");
                foreach (XmlNode node in elemList)
                {
                    modelKeys.TryAdd(node.Attributes["name"].Value, node.InnerText);
                }
            }

            // recurse through the directory filePath   
            if (Directory.Exists(filePath))
            {
                string[] files = Directory.GetFiles(filePath, "*.razor", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    if (ExcludeFiles.Contains(Path.GetFileName(file)))
                    {
                        continue;
                    }

                    formClassTItem = null;
                    Console.WriteLine("Processing file: " + file);
                    await ProcessRazorFile(modelKeys, file, resourcePath, customActions);
                }
            }
            else
            {
                await ProcessRazorFile(modelKeys, filePath, resourcePath, customActions);
            }
            if (modelKeys.Count > 0)
            {
                await ResourceGenerator.GenerateResourceFile(resourcePath, modelKeys);
            }
        }
    }
}
