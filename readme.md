# BlazorLocalizer

BlazorLocalizer is a tool to help localize Blazor Razor components. This tool can be used to automate the process of localizing Blazor Razor components by processing Razor files and updating the localized resources file.

This program is focused on providing localization support for applications built with Radzen Blazor Studio.

Supported Radzen Blazor components:
RadzenTemplateForm, RadzenDropDownDataGridColumn, RadzenDataGridColumn, RadzenLabel, RadzenRequiredValidator, RadzenButton, and RadzenPanelMenuItem
## Features

* Generate resource files from .razor files in your Blazor application
* Translate resource files to different languages using Translator API
* Exclude specific files from localization
* Create default localization settings
* Easy-to-use command-line interface with shortcuts for faster input

## Installation

1. Clone the repository:

```sh
git clone https://github.com/chlupac/BlazorLocalizer.git
```

2. Build the project:

```sh
dotnet build
```

3. (Optional) Install the program as dotnet tool:

```sh 
dotnet pack
dotnet tool install --global --add-source ./nupkg BlazorLocalizer
```
4. (Optional) Uninstall the program as dotnet tool:

```sh
dotnet tool uninstall --global BlazorLocalizer
```
5. (Optional) Update the program as dotnet tool:

```sh
dotnet tool update --global --add-source ./nupkg BlazorLocalizer
```

## Usage

Use the command-line interface to perform localization tasks:

```
BlazorLocalizer <command> [parameters]
```

### Commands

| Command       | Shortcut | Description |
| ------------- |:--------:| ----------- |
| localize      | l        | Localize a Blazor project by generating resource files with translations. |
| translate     | t        | Translate an existing resource file to the specified target language. |
| settings      | s        | Manage localization settings. Creates a default settings file if none exists. |
| help          | h        | Display usage information and available commands. |

### Parameters

| Parameter         | Shortcut | Description                                                      |
|-------------------|:--------:|------------------------------------------------------------------|
| --projectPath     |    -p    | Path to the Blazor project.                                      |
| --resourcePath    |    -r    | Path to the main resource file.                                  |
| --excludeFiles    |    -x    | List of files to exclude from localization, separated by commas. |
| --targetLanguages |    -t    | comma delimited list of language codes for translation.          |
| --email           |    -e    | Email address for the Translator API.                            |
                                                                                                         
### Switches

| Switch    | Shortcut | Description                                                      |
|-----------|:--------:|------------------------------------------------------------------|
| --verbose |    -v    | Enables verbose output.                                     |
|--test     |    -t    | Test mode. No files will be modified.                           |
|--save     |    -s    | Save settings to the settings file.                             |

### Examples

Localize a Blazor project and generate resource files:

```
BlazorLocalizer l -p ./MyBlazorProject.csproj -r ./MyBlazorProject/Resources/SharedResources.resx -e App.razor,_Imports.razor -t de-DE -m my@email.com
```

Translate an existing resource file to French:

```
BlazorLocalizer t -r ./MyBlazorProject/Resources/SharedResources.resx -t fr-FR -m my@email.com
```

Create a default localization settings file:

```
BlazorLocalizer s
```

Display help information:

```
BlazorLocalizer h
```

## License

BlazorLocalizer is released under the [MIT License](LICENSE).
