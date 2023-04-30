# BlazorLocalizer

BlazorLocalizer is a tool to help localize Blazor Razor components. This tool can be used to automate the process of localizing Blazor Razor components by processing Razor files and updating the localized resources file.

This program is focused on providing localization support for applications built with Radzen Blazor Studio.

Supported Radzen Blazor components:
RadzenTemplateForm, RadzenDropDownDataGridColumn, RadzenDataGridColumn, RadzenLabel, RadzenRequiredValidator, RadzenButton, and RadzenPanelMenuItem

## Installation

1. Clone the repository:

```sh
git clone https://github.com/chlupac/BlazorLocalizer.git
```

2. Build the project:

```sh
dotnet build
```

3. Run the program:

```sh
dotnet run --project BlazorLocalizer.csproj
```

4. (Optional) Install the program as dotnet tool:

```sh 
dotnet pack
dotnet tool install --global --add-source ./nupkg BlazorLocalizer
```
5. (Optional) Run the program as dotnet tool:

```sh
BlazorLocalizer localize /path/to/project ./Resources/Resources.resx fr-FR  
```
6. (Optional) Uninstall the program as dotnet tool:

```sh
dotnet tool uninstall --global BlazorLocalizer
```
7. (Optional) Update the program as dotnet tool:

```sh
dotnet tool update --global --add-source ./nupkg BlazorLocalizer
```


## Usage

### Command line parameters

BlazorLocalizer supports the following command line parameters:

- `localize [projectPath] [resourcePath] [targetLanguage] [excludeFiles]` - localize Blazor Razor components in a project recursively.
    - `projectPath` - (optional) The path to the project to update recursively. Default is the current directory.
    - `resourcePath` - (optional) The path to the resource file to update. Default is `./Resources/SharedResources.resx`.
    - `targetLanguage` - (optional) The target language to localize. Default is `cs-CZ`.
    - `email` - (optional) The email address to use for the translation service. Default is `null`. When not specified, the translation service will have lower daily quota usage.
    - `excludeFiles` - (optional) A comma-separated list of files to exclude from processing. Default is `App.razor,_Imports.razor,RedirectToLogin.razor,CulturePicker.razor`.

- `translate [resourcePath] [targetLanguage]` - translate an existing resource file to a target language.
    - `resourcePath` - (optional) The path to the resource file to translate. Default is `./Resources/SharedResources.resx`.
    - `targetLanguage` - (optional) The target language to translate to. Default is `cs-CZ`.
    - `email` - (optional) The email address to use for the translation service. Default is `null`. When not specified, the translation service will have lower daily quota usage.

- `help` - display help information.

### Examples

Localize Blazor Razor components in the current directory:

```sh
BlazorLocalizer localize
```

Localize Blazor Razor components in a specific project:

```sh
BlazorLocalizer localize /path/to/project/
```

Localize Blazor Razor components in a specific project, using a custom resource file and target language:

```sh
BlazorLocalizer localize /path/to/project/ ./Resources/Resources.resx fr-FR
```

Translate an existing resource file to a target language:

```sh
BlazorLocalizer translate ./Resources/Resources.resx de-DE
```

## License

This project is licensed under the [MIT License](https://opensource.org/licenses/MIT).
