using System.Text.RegularExpressions;

namespace BlazorLocalizer;

public class CustomAction
{
    public string ComponentType { get; set; }
    public string FileType { get; set; }
    public Func<string> Regex { get; set; }
    public Func<string, string> Localizer { get; set; }
    public Func<Match, string> Action { get; set; }


    public CustomAction()
    {
        FileType = ".razor";
        Regex = () => $@"<(?<tag>{ComponentType})(\s+(?<attr>\S+?)(=""(?<value>.*?)""|$))+\s*/?>";
        Localizer = key => $"@D[\"{key}\"]";
    }
}