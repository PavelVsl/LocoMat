namespace BlazorLocalizer;

public class CustomAction
{
    public string ComponentType { get; set; }
    public Func<string> Regex { get; set; }
    public Func<string,string> Localizer { get; set; }
    public Func<string, string> Action { get; set; }

    public CustomAction()
    {
        Regex = () => $@"<(?<tag>{ComponentType})(\s+(?<attr>\S+?)(=""(?<value>.*?)""|$))+\s*/?>";
        Localizer = key => $"@D[\"{key}\"]";
    }
}