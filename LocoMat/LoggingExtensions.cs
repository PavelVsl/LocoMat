using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;

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

            // log to debug window
            configure.AddDebug();

            configure.SetMinimumLevel(logLevel);
        });
        return services;
    }
}
