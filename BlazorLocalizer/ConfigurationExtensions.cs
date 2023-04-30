namespace RazorLocalizerTests;

using Microsoft.Extensions.Configuration;

public static class ConfigurationExtensions
{
    public static T GetValue<T>(this IConfigurationRoot? configuration, string key)
    {
        if (configuration != null)
        {
            var value = configuration[key];
            if (value == null)
            {
                return default(T);
            }
            return (T)Convert.ChangeType(value, typeof(T));
        }

        return default(T);
    }
}
