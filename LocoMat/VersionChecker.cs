using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace LocoMat;

public class VersionChecker
{
    // Check on nuget.org if the current version is the latest version
    public static async Task<bool> IsLatestVersion()
    {
        var client = new HttpClient();
        var response = await client.GetAsync("https://api.nuget.org/v3-flatcontainer/locomat/index.json");
        var json = await response.Content.ReadAsStringAsync();
        var latestVersion = JsonSerializer.Deserialize<LatestVersion>(json);
        return latestVersion.Versions.Contains(Assembly.GetExecutingAssembly().GetName().Version.ToString());
    }
    public class LatestVersion
    {
        public List<string> Versions { get; set; }
    }
    
    // Self-update the application
    // from dotnet-script:
    //
    public async Task Update()
    {
        var client = new HttpClient();
        var response = await client.GetAsync("https://api.nuget.org/v3-flatcontainer/locomat/index.json");
        var json = await response.Content.ReadAsStringAsync();
        var latestVersions = JsonSerializer.Deserialize<LatestVersion>(json);
        var latestVersion = latestVersions.Versions.Last();
        var currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        if (latestVersion != currentVersion)
        {
            var process = new Process();
            process.StartInfo.FileName = "dotnet";
            process.StartInfo.Arguments = $"tool update -g LocoMat --version {latestVersion}";
            process.Start();
        }
    }
}
