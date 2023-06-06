using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace LocoMat;

public class NamespaceService
{
    private readonly ILogger<NamespaceService> _logger;
    private readonly ConfigurationData _config;

    public NamespaceService(ILogger<NamespaceService> logger, ConfigurationData config)
    {
        _logger = logger;
        _config = config;
    }

    public string GetNamespace(string projectPath = null)
    {
        var projectFilePath = _config.Project;
        var csprojFile = XDocument.Load(projectFilePath);
        XNamespace msbuild = "http://schemas.microsoft.com/developer/msbuild/2003";
        var rootNamespaceElement = csprojFile.Descendants(msbuild + "RootNamespace").FirstOrDefault();
        var nameSpace = rootNamespaceElement?.Value ?? Path.GetFileNameWithoutExtension(projectFilePath);
        if (projectPath != null) nameSpace += "." + projectPath.Replace('/', '.').Replace('\\', '.');
        return nameSpace;
    }
}