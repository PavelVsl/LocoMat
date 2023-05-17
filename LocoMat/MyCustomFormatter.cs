using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace LocoMat;

public class MyCustomFormatter : ConsoleFormatter
{
    public MyCustomFormatter() : base(nameof(MyCustomFormatter))
    {
    }

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider,
        TextWriter consoleOutput)
    {
        if (logEntry.State is IReadOnlyList<KeyValuePair<string, object>> values)
        {
            //get the message from the values without logging the category
            var message = values.FirstOrDefault(kvp => kvp.Key == "{OriginalFormat}").Value?.ToString();
            consoleOutput.Write(message);
            consoleOutput.Write(Environment.NewLine);
        }
    }
}
