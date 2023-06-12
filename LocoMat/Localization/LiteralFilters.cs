using System.Reflection;
using LocoMat.Localization.Filters;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace LocoMat.Localization;

public class LiteralFilters : List<ILiteralFilter>, ILiteralFilter
{
    private readonly ILogger<LiteralFilters> _logger;

    public LiteralFilters(ILogger<LiteralFilters> logger)
    {
        _logger = logger;
        Load();
        Add(new MethodCallFilter("Query"));

        Add(new MethodCallFilter("NavigateTo"));
        Add(new MethodCallRegexFilter("Export*"));
        Add(new MethodCallRegexFilter("Invoke*"));
    }

    //Load filters using reflection
    private void Load()
    {
        var types = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetInterfaces().Contains(typeof(ILiteralFilter)) && !t.IsAbstract);
        foreach (var type in types)
        {
            //exclude this
            if (type == typeof(LiteralFilters))
                continue;
            //exclude filter without parameterless constructor
            if (type.GetConstructor(Type.EmptyTypes) == null)
                continue;

            var filter = (ILiteralFilter)Activator.CreateInstance(type);
            Add(filter);
        }
    }

    public string Name => "Composite Filter";
    public string Description => "Composite filter containing several individual filters";

    public bool IsProhibited(LiteralExpressionSyntax literal)
    {
        foreach (var filter in this)
            if (filter.IsProhibited(literal))
            {
                _logger.LogDebug($"Literal '{literal}' is not localizable because of filter '{filter.Name}'");
                return true;
            }

        return false;
    }
}
