using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;
using LocoMat.Localization;
using LocoMat.Localization.Filters;
using LocoMat.Scaffold;
using LocoMat.Translation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LocoMat;

internal class Program
{
    private static int Main(string[] args)
    {
        var projectOption = new Option<string>(
            new[] { "--project", "-p" },
            description: "Path to the project file. Defaults to the first .csproj file in the current directory.",
            isDefault: true,
            parseArgument: result =>
            {
                var value = result.Tokens.SingleOrDefault()?.Value;
                value = Utilities.GetProjectFileName(value);
                if (value == null) result.ErrorMessage = $"No .csproj file found {value}.";
                return value;
            }
        );

        var resourceOption = new Option<string>(
            new[] { "--resource", "-r" },
            description: "Path to the resource file.",
            getDefaultValue: () => "Resources/SharedResources.resx"
        );
        var excludeOption = new Option<string>(
            new[] { "--exclude", "-x" },
            description: "Comma-separated list of file names to exclude from localization.",
            getDefaultValue: () => "App.razor,_Imports.razor,RedirectToLogin.razor,CulturePicker.razor"
        );
        var includeOption = new Option<string>(
            new[] { "--include", "-i" },
            description: "File name pattern to include in localization.",
            getDefaultValue: () => "*.razor"
        );
        var testOption = new Option<bool>(new[] { "--test-mode", "-t" }, "Runs in test mode without actually changing any files.");
        
        var backupOption = new Option<bool>(new[] { "--backup", "-b" }, "Creates a backup of the original files before modifying them.");
        
        var sourceOption = new Option<string>(
            new[] { "--source", "-s" },
            description: "Optional source directory for locating the resource files. Defaults to the current directory.",
            isDefault: true,
            parseArgument: result =>
                {
                    var value = result.Tokens.SingleOrDefault()?.Value;
                    // if parameter is not specified, use current directory, else check if directory exists
                    if (value == null) value = Directory.GetCurrentDirectory();
                    else if (!Directory.Exists(value)) result.ErrorMessage = $"Directory {value} does not exist.";
                    return value;
                }
        );
        
        var outputOption = new Option<string>(new[] { "--output", "-o" }, "Optional output directory for storing translated files. Defaults to the current directory.");
        var targetLangOption = new Option<string>(new[] { "--target-languages", "-t" }, "Comma-separated list of target languages for translation. Defaults to empty (i.e., no translation).");
        var emailOption = new Option<string>(new[] { "--email", "-e" }, "Email address. Required for translation service.");
        var forceOption = new Option<bool>(new[] { "--force", "-f" }, "Forces overwrite existing files when restoring from backup.");
        var nullablesOption = new Option<bool>(new[] { "--nullable", "-n" }, "Generates nullable reference types code in scaffolded code.");

        var verbosityOption = new Option<LogLevel>(
            new[] { "--verbosity", "-v" },
            () => LogLevel.Information,
            "Sets the verbosity level."
        );
        var localizeCommand = new Command("localize", "Localizes the project.")
        {
            projectOption,
            resourceOption,
            excludeOption,
            includeOption,
            testOption,
            verbosityOption,
            backupOption,
        };

        var scaffoldCommand = new Command("scaffold", "Scaffolds localization of Radzen.Blazor components.")
        {
            projectOption,
            verbosityOption,
            nullablesOption,
        };
        var translateCommand = new Command("translate", "Translates resource files.")
        {
            sourceOption,
            outputOption,
            targetLangOption,
            emailOption,
            verbosityOption,
        };
        var restoreCommand = new Command("restore", "Restores the original files from backup.")
        {
            forceOption,
            verbosityOption,
        };

        var rootCommand = new RootCommand
        {
            localizeCommand,
            translateCommand,
            scaffoldCommand,
            restoreCommand,
        };

        rootCommand.Description = $"LocoMat version {Utilities.GetVersion()}";

        // Register command handlers
        localizeCommand.Handler = CommandHandler.Create<ConfigurationData>(async (configData) =>
        {
            var services = new ServiceCollection()
                .AddCustomLogging(configData.Verbosity)
                .AddSingleton(configData)
                .AddSingleton<ILocalizationService, LocalizationService>()
                .AddSingleton<RazorProcessor>()
                .AddSingleton<ResourceKeys>()
                .AddSingleton<CustomActions>()
                .AddSingleton<CsProcessor>()
                .AddSingleton<BackupService>()
                .AddSingleton<ILiteralFilter, LiteralFilters>()
                .AddSingleton<LocalizeStringLiteralsRewriter>()
                .AddSingleton<NamespaceService>()
                .BuildServiceProvider();

            var localizationService = services.GetRequiredService<ILocalizationService>();
            await localizationService.Localize();
        });

        translateCommand.Handler = CommandHandler.Create<ConfigurationData>(async (configData) =>
        {
            var services = new ServiceCollection()
                .AddCustomLogging(configData.Verbosity)
                .AddSingleton(configData)
                .AddSingleton<ITranslationService, TranslationService>()
                .AddSingleton<ResourceKeys>()
                .BuildServiceProvider();
            var translationService = services.GetRequiredService<ITranslationService>();
            await translationService.Translate();
        });

        scaffoldCommand.Handler = CommandHandler.Create<ConfigurationData>((configData) =>
        {
            var services = new ServiceCollection()
                .AddCustomLogging(configData.Verbosity)
                .AddSingleton(configData)
                .AddSingleton<IScaffoldService, ScaffoldService>()
                .AddSingleton<NamespaceService>()
                .BuildServiceProvider();
            var scaffoldService = services.GetRequiredService<IScaffoldService>();
            scaffoldService.Scaffold();
        });

        restoreCommand.Handler = CommandHandler.Create<ConfigurationData>((configData) =>
        {
            var services = new ServiceCollection()
                .AddCustomLogging(configData.Verbosity)
                .AddSingleton(configData)
                .AddSingleton<BackupService>()
                .BuildServiceProvider();
            var backupService = services.GetRequiredService<BackupService>();
            backupService.RestoreAsync().Wait();
        });

        return rootCommand.InvokeAsync(args).Result;
    }
}
