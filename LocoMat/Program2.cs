using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using LocoMat.RadzenComponents;
using LocoMat.Translation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LocoMat;

internal class Program2
{
    private readonly ILogger<Program2> _logger;
    private readonly ConfigurationData _configData;
    private readonly BackupService _backupService;
    private readonly ILocalizationService _localizator;
    private readonly IScaffoldService _componentScaffolder;
    private readonly ITranslationService _translationService;

    public Program2(
        ILogger<Program2> logger,
        ConfigurationData configData,
        RazorProcessor razorProcessor,
        BackupService backupService,
        ILocalizationService localizator,
        IScaffoldService componentScaffolder,
        ITranslationService translationService
    )
    {
        _logger = logger;
        _configData = configData;
        _backupService = backupService;
        _localizator = localizator;
        _componentScaffolder = componentScaffolder;
        _translationService = translationService;
    }

    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    private static void Mainx(string[] args)
    {
        var config = BuildConfiguration(args);
        var configData = GetParametersFromArgs(args, config);

        var services = new ServiceCollection()
            .AddCustomLogging(configData.LogLevel)
            .AddSingleton(config)
            .AddSingleton(configData)
            .AddSingleton<RazorProcessor>()

            .AddSingleton<ResourceKeys>()
            .AddSingleton<CustomActions>()
            .AddSingleton<TranslationService>()
            .AddSingleton<CsProcessor>()
            .AddSingleton<BackupService>()
            .AddSingleton<ILiteralFilter, LiteralFilters>()
            .AddSingleton<LocalizationService>()
            .AddSingleton<ScaffoldService>()
            .AddSingleton<NamespaceService>()
            .AddTransient<Program2>()
            .BuildServiceProvider();

        var program = services.GetRequiredService<Program2>();

        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            program.Stop();
            eventArgs.Cancel = true;
        };
        program.Run();
    }


    public void Stop()
    {
        _backupService.Close();
        _cancellationTokenSource.Cancel();
    }

    private void Run()
    {
        _logger.LogDebug($"Starting LocoMat v{Assembly.GetExecutingAssembly().GetName().Version}");
        switch (_configData.Command)
        {
            case "localize":
            case "l":
                if (CheckConfiguration(_configData))
                {
                    _logger.LogDebug("Localizing...");
                    _localizator.Localize().Wait();
                    _backupService.Close();
                    _logger.LogDebug("Localization complete");
                }

                break;
            case "translate":
            case "t":
                if (CheckConfiguration(_configData))
                {
                    _logger.LogDebug("Translating resources...");
                    _translationService.Translate().Wait();
                    _logger.LogDebug("Translation complete");
                }

                break;
            case "scaffold":
            case "f":
                _componentScaffolder.Scaffold();
                break;
            case "settings":
            case "s":
                HandleSettings();
                break;
            case "restore":
                _backupService.RestoreAsync().Wait();
                break;
            case "help":
            case "h":
            default:
                _logger.LogDebug("Help requested");
                ConsoleHelp();
                break;
        }

        _logger.LogDebug("Application exiting");
    }

    private static IConfigurationRoot BuildConfiguration(string[] args)
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("LocoMatSettings.json", true)
            .AddCommandLine(args, GetSwitchMappings())
            .Build();
    }

    private static Dictionary<string, string> GetSwitchMappings()
    {
        return new Dictionary<string, string>
        {
            { "-p", "project" },
            { "-r", "resourcePath" },
            { "-o", "outputPath" },
            { "-x", "excludeFiles" },
            { "-t", "targetLanguages" },
            { "-e", "email" },
            { "-i", "includeFiles" },
            { "-test", "testMode" },
            { "-v", "verbose" },
            { "-q", "quiet" },
            { "-b", "backup" },
            { "-s", "save" },
            { "-f", "force" },
        };
    }

    private void CreateDefaultSettingsFile()
    {
        var appSettings = new Dictionary<string, object>
        {
            { "Project", "./MyProject.csproj" }, // Relative to current directory or absolute
            { "ResourcePath", "Resources/SharedResources.resx" }, // Relative to Project or absolute
            { "ExcludeFiles", "App.razor,_Imports.razor,RedirectToLogin.razor,CulturePicker.razor" }, //Comma separated list of files to exclude
            { "IncludeFiles", "*.razor" }, // Default: *.razor
            { "TargetLanguages", "" }, // Comma separated list of languages to translate to
            { "Email", "sample@email.com" }, // Email for translated.net translation service 
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        var jsonString = JsonSerializer.Serialize(appSettings, options);

        File.WriteAllText("LocoMatSettings.json", jsonString);
        _logger.LogInformation("Default settings file created: LocoMatSettings.json");
        _logger.LogInformation(jsonString);
    }


    private static ConfigurationData GetParametersFromArgs(string[] args, IConfigurationRoot configuration)
    {
        var basePath = Directory.GetCurrentDirectory();
        if (configuration["project"] != null)
        {
            var projectPath = configuration["project"];
            if (!Path.IsPathRooted(projectPath))
            {
                projectPath = Path.Combine(basePath, projectPath);
            }
            // if project path is a directory, assume it's the base path
            if (Directory.Exists(projectPath))
            {
                basePath = projectPath;
            }
            else
            {
                basePath = Path.GetDirectoryName(projectPath);
            }
        }
        
        var config = new ConfigurationData
        {
            Command = args.Length > 0 ? args[0] : "help",
            Project = configuration["projectPath"] ?? Utilities.GetProjectFileName(),
            BasePath = basePath,
            Resource = configuration["resourcePath"] ?? "Resources/SharedResources.resx",
            Output = configuration["outputPath"] ?? "",
            Exclude = configuration["exclude"] ?? "App.razor,_Imports.razor,RedirectToLogin.razor,CulturePicker.razor",
            TargetLanguages = configuration["targetLanguages"] ?? "",
            Email = configuration["email"],
            Include = configuration["includeFiles"] ?? "*.razor",
            TestMode = configuration["testMode"] != null,
            VerboseOutput = configuration["verbose"] != null,
            Force = configuration["force"] != null,
            QuietOutput = configuration["quiet"] != null,
            Backup = configuration["backup"] != null,
            Save = configuration["save"] != null
        };

        //fix for relative paths
        if (!string.IsNullOrEmpty(config.Project) && !Path.IsPathRooted(config.Project)) config.Project = Path.Combine(Directory.GetCurrentDirectory(), config.Project);

        //fix for relative paths
        if (!string.IsNullOrEmpty(config.Resource) && !Path.IsPathRooted(config.Resource)) config.Resource = Path.Combine(Path.GetDirectoryName(config.Project), config.Resource);

        return config;
    }

    private bool CheckConfiguration(ConfigurationData config)
    {
        var result = true;
        //check if project file exists and is valid
        if (!File.Exists(config.Project))
        {
            _logger.LogError("Project file does not exist: " + config.Project);
            result = false;
        }

        if (string.IsNullOrEmpty(config.Resource))
        {
            _logger.LogError("ResourcePath is not set");
            result = false;
        }

        //if languages is set, check if it is valid
        if (!string.IsNullOrEmpty(config.TargetLanguages))
        {
            var languages = config.TargetLanguages.Split(",");
            foreach (var language in languages)
                if (!CultureInfo.GetCultures(CultureTypes.AllCultures).Any(c => c.Name == language))
                {
                    _logger.LogError("Invalid language: " + language);
                    result = false;
                }
        }

        //if languages is set, check if email is valid
        if (!string.IsNullOrEmpty(config.TargetLanguages) && !Utilities.IsValidEmail(config.Email))
        {
            _logger.LogError("Email is not set or is not valid");
            result = false;
        }

        if (config.Save) _logger.LogInformation("Save command is not implemented yet");

        return result;
    }


    private void HandleSettings()
    {
        if (!File.Exists("LocoMatSettings.json"))
        {
            CreateDefaultSettingsFile();
        }
        else
        {
            _logger.LogInformation("Settings file already exists: LocoMatSettings.json");
            _logger.LogInformation(File.ReadAllText("LocoMatSettings.json"));
        }
    }


    private static void ConsoleHelp()
    {
        Console.WriteLine("Usage: LocoMat <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("localize, l\tLocalizes the source files.");
        Console.WriteLine("  -p\t\tPath to the project file. Defaults to the first .csproj file in the current directory.");
        Console.WriteLine("  -r\t\tPath to the resource file. Defaults to 'Resources/SharedResources.resx'.");
        Console.WriteLine("  -x\t\tComma-separated list of file names to exclude from localization. Defaults to 'App.razor,_Imports.razor,RedirectToLogin.razor,CulturePicker.razor'.");
        Console.WriteLine("  -i\t\tFile name pattern to include in localization. Defaults to '*.razor'.");
        Console.WriteLine("  -test\t\tRuns in test mode without actually changing any files.");
        Console.WriteLine("  -b\t\tBackup changed files.");
        Console.WriteLine();
        Console.WriteLine("  translate, t\tTranslates the resource files.");
        Console.WriteLine("  -r\t\tPath to the resource file to be translated. Defaults to 'Resources/SharedResources.resx'.");
        Console.WriteLine("  -o\t\tOutput path where translated resources will be generated. If not specified, the translated resources will be generated in the same folder as the original resource file.");
        Console.WriteLine("  -t\t\tComma-separated list of target languages for translation. Defaults to empty (i.e. no translation).");
        Console.WriteLine("  -e\t\tEmail address. Required for translation service.");
        Console.WriteLine();
        Console.WriteLine("  Scaffold, s\tScaffolds localization of Radzen.Blazor components.");
        Console.WriteLine("  -p\t\tPath to the project file. Defaults to the first .csproj file in the current directory.");
        Console.WriteLine("  -t\t\tComma-separated list of target languages for translation. Defaults to empty (i.e. no translation).");
        Console.WriteLine("  -e\t\tEmail address. Required for translation service.");
        Console.WriteLine();
        Console.WriteLine("  restore\tRestores the original source files from the backup.");
        Console.WriteLine("  -f\t\tForces overwrite existing files when restoring from backup.");
        Console.WriteLine();
        Console.WriteLine("  settings, s\tDisplays or changes the application settings.");
        Console.WriteLine();
        Console.WriteLine("  help, h\tDisplays this help message.");
        Console.WriteLine();
        Console.WriteLine("Switches:");
        Console.WriteLine("  -v\t\tEnables verbose output.");
        Console.WriteLine("  -s\t\tSaves the settings to the configuration file.");
    }
}
