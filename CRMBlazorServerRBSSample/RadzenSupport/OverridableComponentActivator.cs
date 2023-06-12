
using Microsoft.AspNetCore.Components;
namespace CRMBlazorServerRBS.RadzenSupport;
public class OverridableComponentActivator : IComponentActivator
{
    private static Dictionary<Type, Type> ReplaceTypes { get; } = new();
    public void RegisterOverride<TOriginal, TOverride>()
    {
        ReplaceTypes.Add(typeof(TOriginal), typeof(TOverride));
    }

    public void RegisterOverride(Type original, Type @override)
    {
        ReplaceTypes.Add(original, @override);
    }
    public IComponent CreateInstance(Type componentType)
    {
        if (!typeof(IComponent).IsAssignableFrom(componentType))
        {
            throw new ArgumentException($"The type {componentType.FullName} does not implement {nameof(IComponent)}.", nameof(componentType));
        }

        if (ReplaceTypes.ContainsKey(componentType))
        {
            componentType = ReplaceTypes[componentType];
        }
        else
        {
            if (componentType.IsGenericType)
            {
                var g = componentType.GetGenericTypeDefinition();
                if(ReplaceTypes.TryGetValue(g, out var type))
                {
                    componentType = type.MakeGenericType(componentType.GenericTypeArguments);
                }
            }
        }

        return (IComponent)Activator.CreateInstance(componentType)!;
    }
}
