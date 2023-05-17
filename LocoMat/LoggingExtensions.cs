using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LocoMat;

public static class LoggingExtensions
{
    public static IServiceCollection AddCustomLogging(this IServiceCollection services, LogLevel logLevel)
    {
        services.AddLogging(configure =>
        {
            configure
                .AddConsole(options => { options.FormatterName = nameof(MyCustomFormatter); })
                .AddConsoleFormatter<MyCustomFormatter, MyCustomFormatterOptions>();
            configure.SetMinimumLevel(logLevel);
        });
        return services;
    }
}
