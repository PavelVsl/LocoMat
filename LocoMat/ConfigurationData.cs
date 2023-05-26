using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace LocoMat;

public class ConfigurationData
{
    [JsonIgnore] public string Command { get; set; }
    public string Project { get; set; }
    public string ResourcePath { get; set; }
    public List<string> ExcludeFiles { get; set; }
    public string TargetLanguages { get; set; }
    public string Email { get; set; }
    public string IncludeFiles { get; set; }
    [JsonIgnore] public bool TestMode { get; set; }
    [JsonIgnore] public bool VerboseOutput { get; set; }
    [JsonIgnore] public bool QuietOutput { get; set; }  
    [JsonIgnore] public LogLevel LogLevel => QuietOutput ? LogLevel.Error : VerboseOutput ? LogLevel.Debug : LogLevel.Information;
    [JsonIgnore] public bool Save { get; set; }
    [JsonIgnore] public bool Force { get; set; }
    [JsonIgnore] public bool Backup { get; set; }

    public string ExpressionFilter { get; set; }
    public string RadzenSupport { get; set; } = "RadzenSupport";

    public ConfigurationData()
    {
        ExcludeFiles = new List<string>();
    }

    public void SaveToJson()
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText("LocoMat.json", json);
    }
}
