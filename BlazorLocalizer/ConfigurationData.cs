using Microsoft.Extensions.Logging;

namespace BlazorLocalizer;

public class ConfigurationData
{
    public string Command { get; set; }
    public string ProjectPath { get; set; }
    public string ResourcePath { get; set; }
    public List<string> ExcludeFiles { get; set; }
    public string TargetLanguages { get; set; }
    public string Email { get; set; }
    public string IncludeFiles { get; set; }
    public bool TestMode { get; set; }
    public bool VerboseOutput { get; set; }
    public LogLevel LogLevel => VerboseOutput ? LogLevel.Debug : LogLevel.Information;
}
