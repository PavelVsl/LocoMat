
using Microsoft.Extensions.Localization;

namespace CRMBlazorServerRBS.RadzenSupport;

public class RadzenLocalizer  :  StringLocalizer<RadzenLocalizer>
{
    public RadzenLocalizer(IStringLocalizerFactory factory) : base(factory)
    {
    }
    public override LocalizedString this[string name] => base[name] == name ? null : base[name];
}