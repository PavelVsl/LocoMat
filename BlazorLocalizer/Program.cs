using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BlazorLocalizer.Translation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlazorLocalizer
{
    class Program
    {
        private readonly ILogger<Program> _logger;
        private readonly ConfigurationData _configData;
        private readonly RazorProcessor _razorProcessor;
        private readonly ResourceGenerator _resourceGenerator;

        public Program(ILogger<Program> logger, ConfigurationData configData, RazorProcessor razorProcessor, ResourceGenerator resourceGenerator)
        {
            _logger = logger;
            _configData = configData;
            _razorProcessor = razorProcessor;
            _resourceGenerator = resourceGenerator;
        }

        static void Main(string[] args)
        {
            var config = BuildConfiguration(args);
            var configData = GetParametersFromArgs(args, config);
            
            var services = new ServiceCollection()
                .AddCustomLogging(configData.LogLevel)
                .AddSingleton(config)
                .AddSingleton(configData)
                .AddSingleton<RazorProcessor>()
                .AddSingleton<ResourceGenerator>()
                .AddSingleton<ResourceKeys>()
                .AddSingleton<CustomActions>()
                .AddSingleton<Translator>()
                .AddTransient<Program>()
                .BuildServiceProvider();

            var program = services.GetRequiredService<Program>();
            program.Run();
        }

        private void Run()
        {
            _logger.LogDebug($"Starting BlazorLocalizer v{Assembly.GetExecutingAssembly().GetName().Version}");
            switch (_configData.Command)
            {
                case "localize":
                case "l":
                    if (CheckConfiguration(_configData))
                    {
                        _logger.LogDebug("Localizing...");
                        _razorProcessor.Localize().Wait();
                        _logger.LogDebug("Localization complete.");
                    }
                    break;
                case "translate":
                case "t":
                    if (CheckConfiguration(_configData))
                    {
                        _logger.LogDebug("Translating resources...");
                        _resourceGenerator.TranslateResourceFile().Wait();
                        _logger.LogDebug("Translation complete.");
                    }
                    break;
                case "settings":
                case "s":
                    HandleSettings();
                    break;
                case "help":
                case "h":
                default:
                    _logger.LogDebug("Help requested.");
                    ConsoleHelp();
                    break;
            }
            _logger.LogDebug("Application exiting.");
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
                {"-s", "save"}
            };
        }

        private static ConfigurationData GetParametersFromArgs(string[] args, IConfigurationRoot configuration)
        {
            var config = new ConfigurationData
            {
                Command = args.Length > 0 ? args[0] : "help",
                Project = configuration["projectPath"] ?? GetProjectFileName() ?? "./",
                ResourcePath = configuration["resourcePath"] ?? "Resources/SharedResources.resx",
                ExcludeFiles = configuration["excludeFiles"]?.Split(",").ToList() ?? "App.razor,_Imports.razor,RedirectToLogin.razor,CulturePicker.razor".Split(",").ToList(),
                TargetLanguages = configuration["targetLanguages"] ?? "",
                Email = configuration["email"],
                IncludeFiles = configuration["includeFiles"] ?? "*.razor",
                TestMode = configuration["testMode"] != null,
                VerboseOutput = configuration["verboseOutput"] != null,
            };

            //fix for relative paths
            if (!string.IsNullOrEmpty(config.Project) && !Path.IsPathRooted(config.Project))
            {
                config.Project = Path.Combine(Directory.GetCurrentDirectory(), config.Project);
            }

            //fix for relative paths
            if (!string.IsNullOrEmpty(config.ResourcePath) && !Path.IsPathRooted(config.ResourcePath))
            {
                config.ResourcePath = Path.Combine(Path.GetDirectoryName(config.Project), config.ResourcePath);
            }

            return config;
        }

        private static string GetProjectFileName()
        {
            //check current dir for csproj file, must be only one, if here more csproj files or does not exists return null
            
            var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csproj");
            return files.Length == 1 ? files[0] : null;
        }
        
        private bool CheckConfiguration(ConfigurationData config)
        {
            var result = true;
            //check if project file exists and is valid
            if (!File.Exists(config.Project))
            {
                _logger.LogError("Project file does not exist: " + config.Project);
                result =  false;
            }

            if (string.IsNullOrEmpty(config.ResourcePath))
            {
                _logger.LogError("ResourcePath is not set");
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
                        _logger.LogError("Invalid language: " + language);
                        result =  false;
                    }
                }
            }

            //if languages is set, check if email is valid
            if (!string.IsNullOrEmpty(config.TargetLanguages) && !IsValidEmail(config.Email))
            {
                _logger.LogError("Email is not set or is not valid");
                result =  false;
            }

            if (config.Save)
            {
                _logger.LogInformation("Save command is not implemented yet");
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


        private void HandleSettings()
        {
            if (!File.Exists("BlazorLocalizerSettings.json"))
            {
                CreateDefaultSettingsFile();
            }
            else
            {
                _logger.LogInformation("Settings file already exists: BlazorLocalizerSettings.json");
                _logger.LogInformation(File.ReadAllText("BlazorLocalizerSettings.json"));
            }
        }

        private void CreateDefaultSettingsFile()
        {
            var appSettings = new Dictionary<string, object>
            {
                { "Project", "./MyProject.csproj" }, // Relative to current directory or absolute
                { "ResourcePath", "Resources/SharedResources.resx" }, // Relative to Project or absolute
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
            _logger.LogInformation("Default settings file created: BlazorLocalizerSettings.json");
            _logger.LogInformation(jsonString);
        }


        private void ConsoleHelp()
        {
            Console.WriteLine($"BlazorLocalizer v{Assembly.GetExecutingAssembly().GetName().Version}");
            Console.WriteLine();
            Console.WriteLine(@"Commands:");
            Console.WriteLine(@"  localize (l)   [--projectPath (-p) <projectPath>] [--resourcePath (-r) <resourcePath>] [--includeFiles (-i) <includeFiles>] [--excludeFiles (-x) <excludeFiles>] [--targetLanguages (-t) <targetLanguages>] [--email (-e) <email>]");
            Console.WriteLine(@"  translate (t)  [--resourcePath (-r) <resourcePath>] [--targetLanguages (-t) <targetLanguages>] [--email (-e) <email>]");
            Console.WriteLine(@"  settings (s) [--projectPath (-p) <projectPath>] [--resourcePath (-r) <resourcePath>] [--includeFiles (-i) <includeFiles>] [--excludeFiles (-x) <excludeFiles>] [--targetLanguages (-t) <targetLanguages>] [--email (-e) <email>]");
            Console.WriteLine(@"  help (h)");
        }
    }
}
