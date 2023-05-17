namespace LocoMat;

public class ResourceKeys : Dictionary<string, string>
{
    private readonly ConfigurationData _config;

    public ResourceKeys(ConfigurationData config) : base(StringComparer.OrdinalIgnoreCase)
    {
        _config = config;
    }

    public new bool TryAdd(string key, string value)
    {
        if (ContainsKey(key)) return false;
        if (string.IsNullOrEmpty(value)) return false;
        if (key.EndsWith(".")) return false;
        value = value.SplitCamelCase();
        return base.TryAdd(key, value);
    }
}
