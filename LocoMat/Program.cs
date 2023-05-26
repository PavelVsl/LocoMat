using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using LocoMat.RadzenComponents;
using LocoMat.Translation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LocoMat;

/// <summary>
/// # LocoMat
/// LocoMat is a tool to help localize Blazor Razor components. This tool can be used to automate the process of localizing
/// Blazor Razor components by processing Razor files and updating the localized resources file.
///
///    This program is focused on providing localization support for applications built with Radzen Blazor Studio.
///
///Supported Radzen Blazor components:
///  RadzenTemplateForm, RadzenDropDownDataGridColumn, RadzenDataGridColumn, RadzenLabel, RadzenRequiredValidator, RadzenButton, and RadzenPanelMenuItem
///
///## Features
///
///    * Generate resource files from .razor files in your Blazor application
///    * Translate resource files to different languages using Translator API
///    * Exclude specific files from localization
///    * Create default localization settings
///    * Easy-to-use command-line interface with shortcuts for faster input
///
/// </summary>
internal class Program
{
    private readonly ILogger<Program> _logger;
    private readonly ConfigurationData _configData;
    private readonly RazorProcessor _razorProcessor;
    private readonly ResourceGenerator _resourceGenerator;
    private readonly BackupService _backupService;
    private readonly Localizator _localizator;
    private readonly ComponentScaffolder _componentScaffolder;

    public Program(
        ILogger<Program> logger,
        ConfigurationData configData,
        RazorProcessor razorProcessor,
        ResourceGenerator resourceGenerator,
        BackupService backupService,
        ExpressionFilterService expressionFilterService,
        Localizator localizator,
        ComponentScaffolder componentScaffolder
    )
    {
        _logger = logger;
        _configData = configData;
        _razorProcessor = razorProcessor;
        _resourceGenerator = resourceGenerator;
        _backupService = backupService;
        _localizator = localizator;
        _componentScaffolder = componentScaffolder;
    }

    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    private static void Main(string[] args)
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
            .AddSingleton<CsProcessor>()
            .AddSingleton<BackupService>()
            .AddSingleton<ExpressionFilterService>()
            .AddSingleton<ILiteralFilter, LiteralFilters>()
            .AddSingleton<Localizator>()
            .AddSingleton<ComponentScaffolder>()
            .AddSingleton<NamespaceService>()
            .AddTransient<Program>()
            .BuildServiceProvider();

        var program = services.GetRequiredService<Program>();

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
            case "components":
            case "c":
                _componentScaffolder.ScaffoldLocalization();
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
            VerboseOutput = configuration["verbose"] != null,
            Force = configuration["force"] != null,
            QuietOutput = configuration["quiet"] != null,
            Backup = configuration["backup"] != null,
            Save = configuration["save"] != null
        };

        //fix for relative paths
        if (!string.IsNullOrEmpty(config.Project) && !Path.IsPathRooted(config.Project)) config.Project = Path.Combine(Directory.GetCurrentDirectory(), config.Project);

        //fix for relative paths
        if (!string.IsNullOrEmpty(config.ResourcePath) && !Path.IsPathRooted(config.ResourcePath)) config.ResourcePath = Path.Combine(Path.GetDirectoryName(config.Project), config.ResourcePath);

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
            result = false;
        }

        if (string.IsNullOrEmpty(config.ResourcePath))
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
        if (!string.IsNullOrEmpty(config.TargetLanguages) && !IsValidEmail(config.Email))
        {
            _logger.LogError("Email is not set or is not valid");
            result = false;
        }

        if (config.Save) _logger.LogInformation("Save command is not implemented yet");

        return result;
    }

    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return false;

        // RFC 2822 compliant regex pattern for email validation
        var pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

        return Regex.IsMatch(email, pattern);
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
