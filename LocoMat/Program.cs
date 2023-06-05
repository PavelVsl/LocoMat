using System;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using LocoMat;
using LocoMat.RadzenComponents;
using LocoMat.Translation;
using Microsoft.Extensions.Logging;

class Program
{
    static int Main(string[] args)
    {
        var projectOption = new Option<string>(
            new[] { "--project", "-p" },
            description: "Path to the project file. Defaults to the first .csproj file in the current directory.",
            isDefault: true,
            parseArgument: result =>
            {
                var value = result.Tokens.Single().Value;
                value = Utilities.GetProjectFileName(value);
                if (value == null)
                {
                    result.ErrorMessage = "No .csproj file found in the current directory.";
                }

                return (value);
            }
        );
        
        var localizeCommand = new Command("localize")
        {
            projectOption,

            new Option<string>(
                new[] { "--resource", "-r" },
                description: "Path to the resource file. Defaults to 'Resources/SharedResources.resx'.",
                getDefaultValue: () => "Resources/SharedResources.resx"
            ),
            new Option<string>(
                new[] { "--exclude", "-x" },
                description: "Comma-separated list of file names to exclude from localization. Defaults to 'App.razor,_Imports.razor,RedirectToLogin.razor,CulturePicker.razor'.",
                getDefaultValue: () => "App.razor,_Imports.razor,RedirectToLogin.razor,CulturePicker.razor"
            ),

            new Option<string>(
                new[] { "--include", "-i" },
                description: "File name pattern to include in localization. Defaults to '*.razor'.",
                getDefaultValue: () => "*.razor"
            ),

            new Option<bool>(new[] { "--test-mode", "-t" }, description: "Runs in test mode without actually changing any files.")
        };
        var scaffoldCommand = new Command("scaffold", "Scaffolds localization of Radzen.Blazor components.")
        {
            projectOption,
        };
        var translateCommand = new Command("translate")
        {
            new Option<string>(
                new[] { "--source", "-s" },
                description: "Optional source directory for locating the resource files. Defaults to the current directory."
            ),
            new Option<string>(new[] { "--output", "-o" }, description: "Optional output directory for storing translated files. Defaults to the current directory."),
            new Option<string>(new[] { "--target-languages", "-t" }, description: "Comma-separated list of target languages for translation. Defaults to empty (i.e., no translation)."),
            new Option<string>(new[] { "--email", "-e" }, description: "Email address. Required for translation service."),
            //new Option<string>(new[] { "--file-pattern" }, description: "Optional file pattern for selecting specific files to translate. Defaults to '*.resx' (all .resx files)."),
        };
        var restoreCommand = new Command("restore")
        {
            new Option<bool>(new[] { "--force", "-f" }, description: "Forces overwrite existing files when restoring from backup.")
        };
        // var settingsCommand = new Command("settings");
        // var helpCommand = new Command("help");

        var rootCommand = new RootCommand
        {
            localizeCommand,
            translateCommand,
            scaffoldCommand,
            restoreCommand,
            // settingsCommand,
            // helpCommand

            //Test mode switch
            new Option<bool>(new[] { "--test-mode", "-t" }, description: "Runs in test mode without actually changing any files."),
            //Verbosity switch like dotnet CLI, implement parsing
            new Option<LogLevel>(
                new[] { "--verbosity", "-v" },
                getDefaultValue: () => LogLevel.Information,
                description: "Sets the verbosity level. Supported values: Trace, Debug, Information, Warning, Error, Critical, None."
            ),
        };

        rootCommand.Description = "Localization Tool";
       // rootCommand.Handler = CommandHandler.Create<ConfigurationData>((config) => { Console.WriteLine("Please specify a command. Use 'help' for more information."); });

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

        // settingsCommand.Handler = CommandHandler.Create(() =>
        // {
        //     // Handle the settings command
        // });
        //
        // helpCommand.Handler = CommandHandler.Create(() =>
        // {
        //     // Handle the help command
        // });

        return rootCommand.InvokeAsync(args).Result;
    }
}
