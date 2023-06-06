using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace LocoMat;

public class ConfigurationData
{
    private string _resource;
    [JsonIgnore] public string Command { get; set; }
    public string Project { get; set; }

    public string Resource
    {
        get
        {
            if (Path.IsPathRooted(_resource)) return _resource;
            var folderPath = Path.GetDirectoryName(Project);
            return Path.Combine(folderPath, _resource);
        }
        set => _resource = value;
    }

    public string Exclude { get; set; }
    public List<string> ExcludeFiles => Exclude?.Split(',').ToList() ?? new List<string>();
    public string TargetLanguages { get; set; }
    public string Email { get; set; }
    public string Include { get; set; }
    [JsonIgnore] public bool TestMode { get; set; }
    [JsonIgnore] public LogLevel Verbosity { get; set; }
    [JsonIgnore] public bool Save { get; set; }
    [JsonIgnore] public bool Force { get; set; }
    [JsonIgnore] public bool Backup { get; set; }

    public string ExpressionFilter { get; set; }
    public string RadzenSupport { get; set; } = "RadzenSupport";
    public string Source { get; set; }
    public string Output { get; set; }
    public string BasePath { get; set; }

    public ConfigurationData()
    {
    }

    public void SaveToJson()
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText("LocoMat.json", json);
    }
}
