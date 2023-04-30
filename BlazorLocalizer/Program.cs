using System.Xml;
using static BlazorRazorLocalizer.RazorProcessor;

namespace BlazorRazorLocalizer
{
    class Program
    {
        static async Task Main(string[] args)
        {
// Default parameters            
            // Project path to update recursively
            var filePath = ".";
            //path to resource file
            var resourcePath = "./Resources/SharedResources.resx";

            // target language
            var targetLanguage = "cs-CZ";
            
            // list of .razor files to process exclude
            var ExcludeFiles = new List<string>
            {
                "App.razor",
                "_Imports.razor",
                "RedirectToLogin.razor", 
                "CulturePicker.razor",
            };
// End of default parameters

            // Gather parameters from command line
            // use syntax BlazorRazorLocalizer command parameters 
            // or BlazorRazorLocalizer -h for help
            // commands :
            // localize [projectPath] [resourcePath] [targetLanguage] [excludeFiles]
            // translate [resourcePath] [targetLanguage]
            // help
            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "localize":
                        if (args.Length > 1)
                        {
                            filePath = args[1];
                        }
                        if (args.Length > 2)
                        {
                            resourcePath = args[2];
                        }
                        if (args.Length > 3)
                        {
                            targetLanguage = args[3];
                        }
                        if (args.Length > 4)
                        {
                            ExcludeFiles = args[4].Split(",").ToList();
                        }
                        break;
                    case "translate":
                        if (args.Length > 1)
                        {
                            resourcePath = args[1];
                        }
                        if (args.Length > 2)
                        {
                            targetLanguage = args[2];
                        }
                        break;
                    case "help":
                        Console.WriteLine("BlazorRazorLocalizer command parameters");
                        Console.WriteLine("localize [projectPath] [resourcePath] [targetLanguage] [excludeFiles]");
                        Console.WriteLine("translate [resourcePath] [targetLanguage]");
                        Console.WriteLine("help");
                        return;
                    default:
                        Console.WriteLine("Unknown command");
                        Console.WriteLine("BlazorRazorLocalizer command parameters");
                        Console.WriteLine("localize [projectPath] [resourcePath] [targetLanguage] [excludeFiles]");
                        Console.WriteLine("translate [resourcePath] [targetLanguage]");
                        Console.WriteLine("help");
                        return;
                }
            }

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

            if (args.Length == 1)
            {
                filePath = args[0];
            }
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

                return;
            }
            await ProcessRazorFile(modelKeys, filePath, resourcePath, customActions);
        }
    }
}
