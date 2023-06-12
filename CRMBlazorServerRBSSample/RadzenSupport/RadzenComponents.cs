using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace CRMBlazorServerRBS.RadzenSupport;
public class PagedDataBoundComponentLocalized<T> : PagedDataBoundComponent<T>
{
    [Inject] RadzenLocalizer L { get; set; }
    protected override void OnInitialized()
    {
      PageSizeText = L["PagedDataBoundComponent.PageSizeText"] ?? PageSizeText;
      PagingSummaryFormat = L["PagedDataBoundComponent.PagingSummaryFormat"] ?? PagingSummaryFormat;
        base.OnInitialized();
    }
}

public class RadzenColorPickerLocalized : RadzenColorPicker
{
    [Inject] RadzenLocalizer L { get; set; }
    protected override void OnInitialized()
    {
      HexText = L["RadzenColorPicker.HexText"] ?? HexText;
      RedText = L["RadzenColorPicker.RedText"] ?? RedText;
      GreenText = L["RadzenColorPicker.GreenText"] ?? GreenText;
      BlueText = L["RadzenColorPicker.BlueText"] ?? BlueText;
      AlphaText = L["RadzenColorPicker.AlphaText"] ?? AlphaText;
      ButtonText = L["RadzenColorPicker.ButtonText"] ?? ButtonText;
        base.OnInitialized();
    }
}

public class RadzenDataFilterLocalized<TItem> : RadzenDataFilter<TItem>
{
    [Inject] RadzenLocalizer L { get; set; }
    protected override void OnInitialized()
    {
      FilterText = L["RadzenDataFilter.FilterText"] ?? FilterText;
      EnumFilterSelectText = L["RadzenDataFilter.EnumFilterSelectText"] ?? EnumFilterSelectText;
      AndOperatorText = L["RadzenDataFilter.AndOperatorText"] ?? AndOperatorText;
      OrOperatorText = L["RadzenDataFilter.OrOperatorText"] ?? OrOperatorText;
      ApplyFilterText = L["RadzenDataFilter.ApplyFilterText"] ?? ApplyFilterText;
      ClearFilterText = L["RadzenDataFilter.ClearFilterText"] ?? ClearFilterText;
      AddFilterText = L["RadzenDataFilter.AddFilterText"] ?? AddFilterText;
      RemoveFilterText = L["RadzenDataFilter.RemoveFilterText"] ?? RemoveFilterText;
      AddFilterGroupText = L["RadzenDataFilter.AddFilterGroupText"] ?? AddFilterGroupText;
      EqualsText = L["RadzenDataFilter.EqualsText"] ?? EqualsText;
      NotEqualsText = L["RadzenDataFilter.NotEqualsText"] ?? NotEqualsText;
      LessThanText = L["RadzenDataFilter.LessThanText"] ?? LessThanText;
      LessThanOrEqualsText = L["RadzenDataFilter.LessThanOrEqualsText"] ?? LessThanOrEqualsText;
      GreaterThanText = L["RadzenDataFilter.GreaterThanText"] ?? GreaterThanText;
      GreaterThanOrEqualsText = L["RadzenDataFilter.GreaterThanOrEqualsText"] ?? GreaterThanOrEqualsText;
      EndsWithText = L["RadzenDataFilter.EndsWithText"] ?? EndsWithText;
      ContainsText = L["RadzenDataFilter.ContainsText"] ?? ContainsText;
      DoesNotContainText = L["RadzenDataFilter.DoesNotContainText"] ?? DoesNotContainText;
      StartsWithText = L["RadzenDataFilter.StartsWithText"] ?? StartsWithText;
      IsNotNullText = L["RadzenDataFilter.IsNotNullText"] ?? IsNotNullText;
      IsNullText = L["RadzenDataFilter.IsNullText"] ?? IsNullText;
      IsEmptyText = L["RadzenDataFilter.IsEmptyText"] ?? IsEmptyText;
      IsNotEmptyText = L["RadzenDataFilter.IsNotEmptyText"] ?? IsNotEmptyText;
        base.OnInitialized();
    }
}

public class RadzenDataGridLocalized<TItem> : RadzenDataGrid<TItem>
{
    [Inject] RadzenLocalizer L { get; set; }
    protected override void OnInitialized()
    {
      FilterText = L["RadzenDataGrid.FilterText"] ?? FilterText;
      EnumFilterSelectText = L["RadzenDataGrid.EnumFilterSelectText"] ?? EnumFilterSelectText;
      AndOperatorText = L["RadzenDataGrid.AndOperatorText"] ?? AndOperatorText;
      OrOperatorText = L["RadzenDataGrid.OrOperatorText"] ?? OrOperatorText;
      ApplyFilterText = L["RadzenDataGrid.ApplyFilterText"] ?? ApplyFilterText;
      ClearFilterText = L["RadzenDataGrid.ClearFilterText"] ?? ClearFilterText;
      EqualsText = L["RadzenDataGrid.EqualsText"] ?? EqualsText;
      NotEqualsText = L["RadzenDataGrid.NotEqualsText"] ?? NotEqualsText;
      LessThanText = L["RadzenDataGrid.LessThanText"] ?? LessThanText;
      LessThanOrEqualsText = L["RadzenDataGrid.LessThanOrEqualsText"] ?? LessThanOrEqualsText;
      GreaterThanText = L["RadzenDataGrid.GreaterThanText"] ?? GreaterThanText;
      GreaterThanOrEqualsText = L["RadzenDataGrid.GreaterThanOrEqualsText"] ?? GreaterThanOrEqualsText;
      EndsWithText = L["RadzenDataGrid.EndsWithText"] ?? EndsWithText;
      ContainsText = L["RadzenDataGrid.ContainsText"] ?? ContainsText;
      DoesNotContainText = L["RadzenDataGrid.DoesNotContainText"] ?? DoesNotContainText;
      StartsWithText = L["RadzenDataGrid.StartsWithText"] ?? StartsWithText;
      IsNotNullText = L["RadzenDataGrid.IsNotNullText"] ?? IsNotNullText;
      IsNullText = L["RadzenDataGrid.IsNullText"] ?? IsNullText;
      IsEmptyText = L["RadzenDataGrid.IsEmptyText"] ?? IsEmptyText;
      IsNotEmptyText = L["RadzenDataGrid.IsNotEmptyText"] ?? IsNotEmptyText;
      EmptyText = L["RadzenDataGrid.EmptyText"] ?? EmptyText;
      ColumnsShowingText = L["RadzenDataGrid.ColumnsShowingText"] ?? ColumnsShowingText;
      AllColumnsText = L["RadzenDataGrid.AllColumnsText"] ?? AllColumnsText;
      ColumnsText = L["RadzenDataGrid.ColumnsText"] ?? ColumnsText;
      GroupPanelText = L["RadzenDataGrid.GroupPanelText"] ?? GroupPanelText;
      PageSizeText = L["RadzenDataGrid.PageSizeText"] ?? PageSizeText;
      PagingSummaryFormat = L["RadzenDataGrid.PagingSummaryFormat"] ?? PagingSummaryFormat;
        base.OnInitialized();
    }
}

public class RadzenDataListLocalized<TItem> : RadzenDataList<TItem>
{
    [Inject] RadzenLocalizer L { get; set; }
    protected override void OnInitialized()
    {
      PageSizeText = L["RadzenDataList.PageSizeText"] ?? PageSizeText;
      PagingSummaryFormat = L["RadzenDataList.PagingSummaryFormat"] ?? PagingSummaryFormat;
        base.OnInitialized();
    }
}

public class RadzenDropDownLocalized<TValue> : RadzenDropDown<TValue>
{
    [Inject] RadzenLocalizer L { get; set; }
    protected override void OnInitialized()
    {
      SelectedItemsText = L["RadzenDropDown.SelectedItemsText"] ?? SelectedItemsText;
        base.OnInitialized();
    }
}

public class RadzenDropDownDataGridLocalized<TValue> : RadzenDropDownDataGrid<TValue>
{
    [Inject] RadzenLocalizer L { get; set; }
    protected override void OnInitialized()
    {
      PagingSummaryFormat = L["RadzenDropDownDataGrid.PagingSummaryFormat"] ?? PagingSummaryFormat;
      EmptyText = L["RadzenDropDownDataGrid.EmptyText"] ?? EmptyText;
      SelectedItemsText = L["RadzenDropDownDataGrid.SelectedItemsText"] ?? SelectedItemsText;
        base.OnInitialized();
    }
}

public class RadzenFileInputLocalized<TValue> : RadzenFileInput<TValue>
{
    [Inject] RadzenLocalizer L { get; set; }
    protected override void OnInitialized()
    {
      ChooseText = L["RadzenFileInput.ChooseText"] ?? ChooseText;
        base.OnInitialized();
    }
}

public class RadzenGridLocalized<TItem> : RadzenGrid<TItem>
{
    [Inject] RadzenLocalizer L { get; set; }
    protected override void OnInitialized()
    {
      FilterText = L["RadzenGrid.FilterText"] ?? FilterText;
      AndOperatorText = L["RadzenGrid.AndOperatorText"] ?? AndOperatorText;
      OrOperatorText = L["RadzenGrid.OrOperatorText"] ?? OrOperatorText;
      ApplyFilterText = L["RadzenGrid.ApplyFilterText"] ?? ApplyFilterText;
      ClearFilterText = L["RadzenGrid.ClearFilterText"] ?? ClearFilterText;
      EqualsText = L["RadzenGrid.EqualsText"] ?? EqualsText;
      NotEqualsText = L["RadzenGrid.NotEqualsText"] ?? NotEqualsText;
      LessThanText = L["RadzenGrid.LessThanText"] ?? LessThanText;
      LessThanOrEqualsText = L["RadzenGrid.LessThanOrEqualsText"] ?? LessThanOrEqualsText;
      GreaterThanText = L["RadzenGrid.GreaterThanText"] ?? GreaterThanText;
      GreaterThanOrEqualsText = L["RadzenGrid.GreaterThanOrEqualsText"] ?? GreaterThanOrEqualsText;
      EndsWithText = L["RadzenGrid.EndsWithText"] ?? EndsWithText;
      ContainsText = L["RadzenGrid.ContainsText"] ?? ContainsText;
      StartsWithText = L["RadzenGrid.StartsWithText"] ?? StartsWithText;
      EmptyText = L["RadzenGrid.EmptyText"] ?? EmptyText;
      PageSizeText = L["RadzenGrid.PageSizeText"] ?? PageSizeText;
      PagingSummaryFormat = L["RadzenGrid.PagingSummaryFormat"] ?? PagingSummaryFormat;
        base.OnInitialized();
    }
}

public class RadzenLoginLocalized : RadzenLogin
{
    [Inject] RadzenLocalizer L { get; set; }
    protected override void OnInitialized()
    {
      LoginText = L["RadzenLogin.LoginText"] ?? LoginText;
      RegisterText = L["RadzenLogin.RegisterText"] ?? RegisterText;
      RememberMeText = L["RadzenLogin.RememberMeText"] ?? RememberMeText;
      RegisterMessageText = L["RadzenLogin.RegisterMessageText"] ?? RegisterMessageText;
      ResetPasswordText = L["RadzenLogin.ResetPasswordText"] ?? ResetPasswordText;
      UserText = L["RadzenLogin.UserText"] ?? UserText;
      PasswordText = L["RadzenLogin.PasswordText"] ?? PasswordText;
        base.OnInitialized();
    }
}

public class RadzenPagerLocalized : RadzenPager
{
    [Inject] RadzenLocalizer L { get; set; }
    protected override void OnInitialized()
    {
      PageSizeText = L["RadzenPager.PageSizeText"] ?? PageSizeText;
      PagingSummaryFormat = L["RadzenPager.PagingSummaryFormat"] ?? PagingSummaryFormat;
        base.OnInitialized();
    }
}

public class RadzenSchedulerLocalized<TItem> : RadzenScheduler<TItem>
{
    [Inject] RadzenLocalizer L { get; set; }
    protected override void OnInitialized()
    {
      TodayText = L["RadzenScheduler.TodayText"] ?? TodayText;
        base.OnInitialized();
    }
}

public class RadzenStepsLocalized : RadzenSteps
{
    [Inject] RadzenLocalizer L { get; set; }
    protected override void OnInitialized()
    {
      NextText = L["RadzenSteps.NextText"] ?? NextText;
      PreviousText = L["RadzenSteps.PreviousText"] ?? PreviousText;
        base.OnInitialized();
    }
}

public class RadzenUploadLocalized : RadzenUpload
{
    [Inject] RadzenLocalizer L { get; set; }
    protected override void OnInitialized()
    {
      ChooseText = L["RadzenUpload.ChooseText"] ?? ChooseText;
        base.OnInitialized();
    }
}

