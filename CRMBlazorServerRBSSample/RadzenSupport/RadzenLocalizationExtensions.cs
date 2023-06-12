
namespace CRMBlazorServerRBS.RadzenSupport;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

public static class RadzenLocalizationExtensions
{
    public static IServiceCollection AddRadzenLocalization(this IServiceCollection services)
    {
        var componentActivator = new OverridableComponentActivator();

        componentActivator.RegisterOverride(typeof(PagedDataBoundComponent<>), typeof(PagedDataBoundComponentLocalized<>));
        componentActivator.RegisterOverride(typeof(RadzenColorPicker), typeof(RadzenColorPickerLocalized));
        componentActivator.RegisterOverride(typeof(RadzenDataFilter<>), typeof(RadzenDataFilterLocalized<>));
        componentActivator.RegisterOverride(typeof(RadzenDataGrid<>), typeof(RadzenDataGridLocalized<>));
        componentActivator.RegisterOverride(typeof(RadzenDataList<>), typeof(RadzenDataListLocalized<>));
        componentActivator.RegisterOverride(typeof(RadzenDropDown<>), typeof(RadzenDropDownLocalized<>));
        componentActivator.RegisterOverride(typeof(RadzenDropDownDataGrid<>), typeof(RadzenDropDownDataGridLocalized<>));
        componentActivator.RegisterOverride(typeof(RadzenFileInput<>), typeof(RadzenFileInputLocalized<>));
        componentActivator.RegisterOverride(typeof(RadzenGrid<>), typeof(RadzenGridLocalized<>));
        componentActivator.RegisterOverride(typeof(RadzenLogin), typeof(RadzenLoginLocalized));
        componentActivator.RegisterOverride(typeof(RadzenPager), typeof(RadzenPagerLocalized));
        componentActivator.RegisterOverride(typeof(RadzenScheduler<>), typeof(RadzenSchedulerLocalized<>));
        componentActivator.RegisterOverride(typeof(RadzenSteps), typeof(RadzenStepsLocalized));
        componentActivator.RegisterOverride(typeof(RadzenUpload), typeof(RadzenUploadLocalized));

        services.AddSingleton<RadzenLocalizer>();
        services.AddSingleton<IComponentActivator>(componentActivator);

        return services;
    }
}