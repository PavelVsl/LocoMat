using Microsoft.Extensions.Configuration;

namespace LocoMat;

public static class ConfigurationExtensions
{
    public static T GetValue<T>(this IConfigurationRoot configuration, string key)
    {
        if (configuration != null)
        {
            var value = configuration[key];
            if (value == null) return default;
            return (T)Convert.ChangeType(value, typeof(T));
        }

        return default;
    }
}