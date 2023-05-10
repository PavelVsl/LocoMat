using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace BlazorLocalizer;

public class ConfigurationData
{
    [JsonIgnore]
    public string Command { get; set; }
    public string Project { get; set; }
    public string ResourcePath { get; set; }
    public List<string> ExcludeFiles { get; set; }
    public string TargetLanguages { get; set; }
    public string Email { get; set; }
    public string IncludeFiles { get; set; }
    [JsonIgnore]
    public bool TestMode { get; set; }
    [JsonIgnore]
    public bool VerboseOutput { get; set; }
    [JsonIgnore]
    public bool Save { get; set; }
    [JsonIgnore]
    public LogLevel LogLevel => VerboseOutput ? LogLevel.Debug : LogLevel.Information;
    
    public ConfigurationData()
    {
        ExcludeFiles = new List<string>();
    }
    
    public void SaveToJson()
    {
        string json = JsonSerializer.Serialize(this, new JsonSerializerOptions {WriteIndented = true});
        File.WriteAllText("BlazorLocalizer.json", json);
    }
}
