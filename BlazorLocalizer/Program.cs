using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace BlazorLocalizer
{
    class Program
    {
static async Task Main(string[] args)
{
    var configuration = BuildConfiguration(args);
    var configData = GetParametersFromArgs(args, configuration);
    Translator.Email = configData.Email;

    // Replace Console.WriteLine with ILogger
    var loggerFactory = CreateLoggerFactory(configData.VerboseOutput);
    var logger = loggerFactory.CreateLogger<Program>();

    logger.LogInformation("Application started.");

    switch (configData.Command)
    {
        case "localize":
        case "l":
            if (CheckConfiguration(configData))
            {
                logger.LogInformation("Localizing...");
                await RazorProcessor.Localize(configData);
                logger.LogInformation("Localization complete.");
            }
            break;
        case "translate":
        case "t":
            if (CheckConfiguration(configData))
            {
                logger.LogInformation("Translating resources...");
                await ResourceGenerator.TranslateResourceFile(configData);
                logger.LogInformation("Translation complete.");
            }
            break;
        case "settings":
        case "s":
            HandleSettings();
            break;
        case "help":
        case "h":
        default:
            logger.LogInformation("Help requested.");
            ConsoleHelp();
            break;
    }

    logger.LogInformation("Application exiting.");
}
private static ILoggerFactory CreateLoggerFactory(bool verboseOutput)
{
    var loggerFactory = LoggerFactory.Create(builder =>
    {
        // Add console logging
        builder.AddConsole();

        // Set minimum log level based on verbose output configuration option
        builder.SetMinimumLevel(verboseOutput ? LogLevel.Debug : LogLevel.Information);
    });

    return loggerFactory;
}

        private static IConfigurationRoot BuildConfiguration(string[] args)
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("BlazorLocalizerSettings.json", optional: true)
                .AddCommandLine(args, GetSwitchMappings())
                .Build();
        }

        private static Dictionary<string, string> GetSwitchMappings()
        {
            return new Dictionary<string, string>
            {
                { "-p", "projectPath" },
                { "-r", "resourcePath" },
                { "-x", "excludeFiles" },
                { "-t", "targetLanguages" },
                { "-e", "email" },
                { "-i", "includeFiles" },
                { "-test", "testMode" },
                { "-v", "verboseOutput" },
            };
        }

        private static ConfigurationData GetParametersFromArgs(string[] args, IConfigurationRoot configuration)
        {
            var config = new ConfigurationData
            {
                Command = args.Length > 0 ? args[0] : "help",
                ProjectPath = configuration["projectPath"] ?? "./",
                ResourcePath = configuration["resourcePath"] ?? "Resources/SharedResources.resx",
                ExcludeFiles = configuration["excludeFiles"]?.Split(",").ToList() ?? "App.razor,_Imports.razor,RedirectToLogin.razor,CulturePicker.razor".Split(",").ToList(),
                TargetLanguages = configuration["targetLanguages"] ?? "",
                Email = configuration["email"],
                IncludeFiles = configuration["includeFiles"] ?? "*.razor",
                TestMode = configuration["testMode"] != null,
                VerboseOutput = configuration["verboseOutput"] != null,
            };

            //fix for relative paths
            if (!string.IsNullOrEmpty(config.ProjectPath) && !Path.IsPathRooted(config.ProjectPath))
            {
                config.ProjectPath = Path.Combine(Directory.GetCurrentDirectory(), config.ProjectPath);
            }

            //fix for relative paths
            if (!string.IsNullOrEmpty(config.ResourcePath) && !Path.IsPathRooted(config.ResourcePath))
            {
                config.ResourcePath = Path.Combine(Path.GetDirectoryName(config.ProjectPath), config.ResourcePath);
            }

            return config;
        }

        private static bool CheckConfiguration(ConfigurationData config)
        {
            var result = true;
            //check if project file exists and is valid
            if (!File.Exists(config.ProjectPath))
            {
                Console.WriteLine("Project file does not exist: " + config.ProjectPath);
                result =  false;
            }

            if (string.IsNullOrEmpty(config.ResourcePath))
            {
                Console.WriteLine("ResourcePath is not set");
                result =  false;
            }

            //if languages is set, check if it is valid
            if (!string.IsNullOrEmpty(config.TargetLanguages))
            {
                var languages = config.TargetLanguages.Split(",");
                foreach (var language in languages)
                {
                    if (!CultureInfo.GetCultures(CultureTypes.AllCultures).Any(c => c.Name == language))
                    {
                        Console.WriteLine("Invalid language: " + language);
                        result =  false;
                    }
                }
            }

            //if languages is set, check if email is valid
            if (!string.IsNullOrEmpty(config.TargetLanguages) && !IsValidEmail(config.Email))
            {
                Console.WriteLine("Email is not set or is not valid");
                result =  false;
            }

            return result;
        }

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            // RFC 2822 compliant regex pattern for email validation
            string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

            return Regex.IsMatch(email, pattern);
        }


        private static void HandleSettings()
        {
            if (!File.Exists("BlazorLocalizerSettings.json"))
            {
                CreateDefaultSettingsFile();
            }
            else
            {
                Console.WriteLine("Settings file already exists: BlazorLocalizerSettings.json");
                Console.WriteLine(File.ReadAllText("BlazorLocalizerSettings.json"));
            }
        }

        private static void CreateDefaultSettingsFile()
        {
            var appSettings = new Dictionary<string, object>
            {
                { "ProjectPath", "./MyProject.csproj" }, // Relative to current directory or absolute
                { "ResourcePath", "Resources/SharedResources.resx" }, // Relative to ProjectPath or absolute
                { "ExcludeFiles", "App.razor,_Imports.razor,RedirectToLogin.razor,CulturePicker.razor" }, //Comma separated list of files to exclude
                { "IncludeFiles", "*.razor" }, // Default: *.razor
                { "TargetLanguages", "cs-CZ" }, // Comma separated list of languages to translate to
                { "Email", "sample@email.com" } // Email for translated.net translation service 
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


        private static void ConsoleHelp()
        {
            Console.WriteLine($"BlazorLocalizer v{Assembly.GetExecutingAssembly().GetName().Version}");
            Console.WriteLine();
            Console.WriteLine(@"Commands:");
            Console.WriteLine(
                @"  localize (l)   --projectPath (-p) <projectPath> --resourcePath (-r) <resourcePath> [--includeFiles (-i) <includeFiles>] [--excludeFiles (-x) <excludeFiles>] --targetLanguages (-t) <targetLanguages> [--email (-e) <email>]");
            Console.WriteLine(@"  translate (t)  --resourcePath (-r) <resourcePath> --targetLanguages (-t) <targetLanguages> [--email (-e) <email>]");
            Console.WriteLine(@"  settings (s)");
            Console.WriteLine(@"  help (h)");
        }
    }
}
