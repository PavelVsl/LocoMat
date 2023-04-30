# BlazorLocalizer

BlazorRazorLocalizer is a tool to help localize Blazor Razor components. This tool can be used to automate the process of localizing Blazor Razor components by processing Razor files and updating the localized resources file.

This program is focused on providing localization support for applications built with Radzen Blazor Studio.

Supported Radzen Blazor components:
RadzenTemplateForm, RadzenDropDownDataGridColumn, RadzenDataGridColumn, RadzenLabel, RadzenRequiredValidator, RadzenButton, and RadzenPanelMenuItem

## Installation

1. Clone the repository:

```sh
git clone https://github.com/username/BlazorRazorLocalizer.git
```

2. Build the project:

```sh
dotnet build
```

3. Run the program:

```sh
dotnet run --project BlazorRazorLocalizer.csproj
```

## Usage

### Command line parameters

BlazorRazorLocalizer supports the following command line parameters:

- `localize [projectPath] [resourcePath] [targetLanguage] [excludeFiles]` - localize Blazor Razor components in a project recursively.
    - `projectPath` - (optional) The path to the project to update recursively. Default is the current directory.
    - `resourcePath` - (optional) The path to the resource file to update. Default is `./Resources/SharedResources.resx`.
    - `targetLanguage` - (optional) The target language to localize. Default is `cs-CZ`.
    - `excludeFiles` - (optional) A comma-separated list of files to exclude from processing. Default is `App.razor,_Imports.razor,RedirectToLogin.razor,CulturePicker.razor`.

- `translate [resourcePath] [targetLanguage]` - translate an existing resource file to a target language.
    - `resourcePath` - (optional) The path to the resource file to translate. Default is `./Resources/SharedResources.resx`.
    - `targetLanguage` - (optional) The target language to translate to. Default is `cs-CZ`.

- `help` - display help information.

### Examples

Localize Blazor Razor components in the current directory:

```sh
BlazorRazorLocalizer localize
```

Localize Blazor Razor components in a specific project:

```sh
BlazorRazorLocalizer localize /path/to/project
```

Localize Blazor Razor components in a specific project, using a custom resource file and target language:

```sh
BlazorRazorLocalizer localize /path/to/project ./Resources/Resources.resx fr-FR
```

Translate an existing resource file to a target language:

```sh
BlazorRazorLocalizer translate ./Resources/Resources.resx de-DE
```

## License

This project is licensed under the [MIT License](https://opensource.org/licenses/MIT).
