using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Radzen;

namespace LocoMat.Scaffold;

public class ScaffoldService : IScaffoldService
{
    private readonly ConfigurationData _config;
    private readonly ILogger<ScaffoldService> _logger;


    public ScaffoldService(
        ILogger<ScaffoldService> logger,
        ConfigurationData config,
        NamespaceService namespaceService
    )
    {
        _logger = logger;
        _config = config;
        _nameSpace = namespaceService.GetNamespace(_config.RadzenSupport);
        _projectFolder = Path.GetDirectoryName(_config.Project);
        _scaffoldFolder = Path.Combine(_projectFolder ?? string.Empty, _config.RadzenSupport);
        LoadClasses();
    }

    private class ClassInfo
    {
        public string Name { get; set; }
        public string GenericType { get; set; }
        public Dictionary<string, string> Properties { get; set; }
    }

    private List<ClassInfo> _classes = new();
    private readonly string _nameSpace;
    private string _projectFolder;
    private readonly string _scaffoldFolder;

    public void Scaffold()
    {
        var separator = Path.DirectorySeparatorChar;
        Utilities.EnsureFolderExists(_scaffoldFolder + separator);
        ScaffoldComponents();
        var resource = ScaffoldResources();
        ScaffoldExtensions();
        ScaffoldComponentActivator();
        ScaffoldLocalizer();
//        _resourceGenerator.TranslateResourceFile(resource).Wait();
        FinishProgramConfiguration(_projectFolder, _nameSpace);
    }

    private void LoadClasses()
    {
        var rb = typeof(RadzenComponent).Assembly;
        foreach (var type in rb.GetTypes().Where(x =>
                     x.IsSubclassOf(typeof(RadzenComponent)) && x.GetProperties().Any(p => p.IsLocalizable())))
        {
            var ci = new ClassInfo();
            ci.Name = type.Name.Replace("`1", "");
            if (type.IsGenericType)
            {
                var param = type.GetGenericArguments()[0].Name;
                ci.GenericType = param;
            }

            ci.Properties = new Dictionary<string, string>();
            Type closedType;
            // If it's a generic type, look for the constructed type with typeof(string) as generic argument.
            if (type.IsGenericType)
            {
                var genericTypeDef = type.GetGenericTypeDefinition();
                closedType = genericTypeDef.MakeGenericType(typeof(string));
            }
            else
            {
                closedType = type;
            }

            // Create an instance of the closedType
            var instance = Activator.CreateInstance(closedType);
            foreach (var property in closedType.GetProperties().Where(p => p.IsLocalizable()))
            {
                // Get property value
                var value = property.GetValue(instance);
                // check if value is null or empty
                if (value == null || string.IsNullOrEmpty(value.ToString())) continue;
                ci.Properties.Add(property.Name, value.ToString());
            }

            // If there are no properties, skip this class
            if (ci.Properties.Count == 0) continue;
            _classes.Add(ci);
        }
    }

    private string ScaffoldResources()
    {
        var projectFolder = Path.GetDirectoryName(_config.Project) ?? string.Empty;
        var filename = Path.Combine(projectFolder, _config.RadzenSupport, "RadzenLocalizer.resx");
        // concat all _classes.Properties to dictionary
        var dict = new Dictionary<string, string>();
        foreach (var type in _classes)
        foreach (var property in type.Properties)
            dict.TryAdd($"{type.Name}.{property.Key}", property.Value);

        Utilities.WriteResourcesToFile(dict, filename);
        return filename;
    }

    private void ScaffoldComponents()
    {
        var filename = Path.Combine(_projectFolder, _config.RadzenSupport, "RadzenComponents.cs");
        var sb = new StringBuilder();
        sb.AppendLine("using Microsoft.AspNetCore.Components;");
        sb.AppendLine("using Radzen;");
        sb.AppendLine("using Radzen.Blazor;");
        sb.AppendLine();
        sb.AppendLine($"namespace {_nameSpace};");

        foreach (var type in _classes)
        {
            var name = type.Name;
            var nullable = _config.Nullables ? " = null!;" : "";
            
            var classHeader = type.GenericType != null
                ? $"public class {name}Localized<{type.GenericType}> : {name}<{type.GenericType}>"
                : $"public class {name}Localized : {name}";

            sb.AppendLine(classHeader);
            sb.AppendLine("{");
            sb.AppendLine($"    [Inject] RadzenLocalizer L {{ get; set; }}{nullable}");
            sb.AppendLine("    protected override void OnInitialized()");
            sb.AppendLine("    {");
            foreach (var property in type.Properties)
                sb.AppendLine($"      {property.Key} = L[\"{name}.{property.Key}\"] ?? {property.Key};");

            sb.AppendLine("        base.OnInitialized();");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();
            _logger.LogDebug($"Scaffolding {name}");
        }

        _logger.LogInformation($"Writing {filename}");
        File.WriteAllText(filename, sb.ToString());
    }

    // Method to generate a file containing extension methods for Radzen localization
    private void ScaffoldExtensions()
    {
        var filePath = Path.Combine(_scaffoldFolder, "RadzenLocalizationExtensions.cs");

        string content = $@"
namespace {_nameSpace};

using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

public static class RadzenLocalizationExtensions
{{
    public static IServiceCollection AddRadzenLocalization(this IServiceCollection services)
    {{
        var componentActivator = new OverridableComponentActivator();

{(string.Join(Environment.NewLine, _classes.Select(type =>
{
    var name = type.Name;
    var p = type.GenericType != null ? "<>" : "";
    return $"        componentActivator.RegisterOverride(typeof({name}{p}), typeof({name}Localized{p}));";
})))}

        services.AddSingleton<RadzenLocalizer>();
        services.AddSingleton<IComponentActivator>(componentActivator);

        return services;
    }}
}}";

        _logger.LogInformation($"Writing {filePath}");
        File.WriteAllText(filePath, content);
    }


    private void ScaffoldComponentActivator()
    {
        var code = $@"
using Microsoft.AspNetCore.Components;
namespace {_nameSpace};
public class OverridableComponentActivator : IComponentActivator
{{
    private static Dictionary<Type, Type> ReplaceTypes {{ get; }} = new();
    public void RegisterOverride<TOriginal, TOverride>()
    {{
        ReplaceTypes.Add(typeof(TOriginal), typeof(TOverride));
    }}

    public void RegisterOverride(Type original, Type @override)
    {{
        ReplaceTypes.Add(original, @override);
    }}
    public IComponent CreateInstance(Type componentType)
    {{
        if (!typeof(IComponent).IsAssignableFrom(componentType))
        {{
            throw new ArgumentException($""The type {{componentType.FullName}} does not implement {{nameof(IComponent)}}."", nameof(componentType));
        }}

        if (ReplaceTypes.ContainsKey(componentType))
        {{
            componentType = ReplaceTypes[componentType];
        }}
        else
        {{
            if (componentType.IsGenericType)
            {{
                var g = componentType.GetGenericTypeDefinition();
                if(ReplaceTypes.TryGetValue(g, out var type))
                {{
                    componentType = type.MakeGenericType(componentType.GenericTypeArguments);
                }}
            }}
        }}

        return (IComponent)Activator.CreateInstance(componentType)!;
    }}
}}
";
        var filename = Path.Combine(_scaffoldFolder, "OverridableComponentActivator.cs");
        _logger.LogInformation($"Writing {filename}");
        File.WriteAllText(filename, code);
    }

    private void ScaffoldLocalizer()
    {
        var code = $@"
using Microsoft.Extensions.Localization;

namespace {_nameSpace};

public class RadzenLocalizer  :  StringLocalizer<RadzenLocalizer>
{{
    public RadzenLocalizer(IStringLocalizerFactory factory) : base(factory)
    {{
    }}
    public override LocalizedString this[string name] => base[name] == name ? null : base[name];
}}";

        var filename = Path.Combine(_scaffoldFolder, "RadzenLocalizer.cs");
        _logger.LogInformation($"Writing {filename}");
        File.WriteAllText(filename, code);
    }

    private void FinishProgramConfiguration(string projectFolder, string nameSpace)
    {
        var filename = Path.Combine(projectFolder, "Program.cs");
        // check file exists
        if (!File.Exists(filename))
        {
            _logger.LogError($"File {filename} not found");
            return;
        }

        var code = File.ReadAllText(filename);
        //load syntax tree
        var tree = CSharpSyntaxTree.ParseText(code);
        //get root node
        var root = tree.GetRoot();
        //check if already present
        if (root.DescendantNodes().OfType<GlobalStatementSyntax>()
            .All(i => i.Statement.ToString() != "builder.Services.AddRadzenLocalization()"))
        {
            //find line with "var app = builder.Build();" and insert service before
            var app = root.DescendantNodes().OfType<GlobalStatementSyntax>()
                .FirstOrDefault(m => m.Statement.ToString() == "var app = builder.Build();");
            if (app != null)
            {
                // add service
                var service = SyntaxFactory.ParseStatement("builder.Services.AddRadzenLocalization();\n");
                var global = SyntaxFactory.GlobalStatement(service);
                root = root.InsertNodesBefore(app, new[] { global });
            }
        }

        if (root.DescendantNodes().OfType<UsingDirectiveSyntax>().All(u => u.Name!.ToString() != nameSpace))
        {
            // add using if not already present
            var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName($" {nameSpace}"));
            //add empty line after last using
            SyntaxNode lastUsing = root.DescendantNodes().OfType<UsingDirectiveSyntax>().LastOrDefault();
            if (lastUsing != null)
            {
                root = root.InsertNodesAfter(lastUsing, new[] { usingDirective });
            }
            else
            {
                //if there is no using directive, add new line and using directive at the top of the file
                var newLine = SyntaxFactory.ParseStatement("");
                var firstToken = root.DescendantTokens().FirstOrDefault(t => t.IsKind(SyntaxKind.UsingKeyword));
                //add using directive at the top of the file
                root = root.InsertNodesBefore(firstToken != default
                    ?
                    //add using directive after the last using directive
                    firstToken.Parent
                    : root.ChildNodes().First(), new SyntaxNode[] { usingDirective, newLine });
            }
        }

        _logger.LogInformation($"Writing {filename}");
        File.WriteAllText(filename, root.ToFullString());
    }
}
