using LocoMat;
using LocoMat.Localization;
using Microsoft.Extensions.Logging;
using Moq;

namespace LocoMatTests;

using System.Text.RegularExpressions;
using Xunit;

public class RazorProcessorTests
{
    private readonly Mock<ILogger<RazorProcessor>> _loggerMock;
    private readonly ConfigurationData _config;
    private readonly CustomActions _customActions;
    private readonly Mock<BackupService> _backupService;
    private readonly ResourceKeys _resourceKeys;

    public RazorProcessorTests()
    {
        // Arrange
        _loggerMock = new Mock<ILogger<RazorProcessor>>();
        _config = new ConfigurationData();
        _config.TestMode = true;
        _config.Project = "test";
        _resourceKeys = new ResourceKeys(_config);
        _customActions = new CustomActions(_resourceKeys);
        _backupService = new Mock<BackupService>(_loggerMock.Object, _config);
    }

    [Theory]
    [InlineData(
        "<RadzenButton ButtonStyle=\"ButtonStyle.Light\" Variant=\"Variant.Flat\"  Click=\"@CancelButtonClick\" Text=\"Cancel\" Visible=\"false\" />",
        true)]
    [InlineData(
        "<RadzenButton ButtonStyle=\"ButtonStyle.Light\" Variant=\"Variant.Flat\"  Click=\"@CancelButtonClick\" Text=\"Cancel\" Visible=false />",
        true)]
    [InlineData(
        "<RadzenButton ButtonStyle=\"ButtonStyle.Light\" Variant=\"Variant.Flat\" Click=\"CancelButtonClick\" Visible=false  Text=\"Cancel\" />",
        true)]
    [InlineData(
        "<RadzenButton ButtonStyle=\"ButtonStyle.Primary\" ButtonType=\"ButtonType.Submit\" Icon=\"save\" Text=\"@D[\"Button.Save\"]\" Variant=\"Variant.Flat\" />",
        false)]
    [InlineData(
        "<RadzenButton ButtonStyle=\"ButtonStyle.Primary\" ButtonType=\"ButtonType.Submit\" Icon=\"save\" Text=\"Save\" Variant=\"Variant.Flat\" />",
        true)]
    public async Task ProcessRadzenButtonFragment(string fragment, bool shouldChange)
    {
        var customActions = new CustomActions(_resourceKeys);
        customActions.Actions.Clear();
        customActions.Actions.Add(new CustomAction
        {
            ComponentType = "RadzenButton",
            Regex = () => $@"<(?<tag>RadzenButton)(\s+\S+)*/?>",
            Localizer = key => $"@D[\"{key}\"]",
            Action = match =>
            {
                var tag = match.Value;
                var text = tag.GetAttributeValue("Text");
                if (string.IsNullOrEmpty(text)) return tag;
                var key = $"Button.{text}";
                return "Matched:" + tag.ReplaceAttributeWithKey(_resourceKeys, "Text", key);
            },
        });

        // Arrange
        var razorProcessor = new RazorProcessor(_loggerMock.Object, _config, customActions, _backupService.Object);
        var result = razorProcessor.ProcessCustomActions(customActions, fragment, "test");

        // Act
        var changed = result != "Matched:" + fragment;
        var isMatch = result.StartsWith("Matched:");

        // Assert
        Assert.True(isMatch);
        Assert.Equal(shouldChange, changed);
    }

    [Theory]
    [InlineData(
        "<RadzenButton ButtonStyle=\"ButtonStyle.Light\" Variant=\"Variant.Flat\"  Click=\"@CancelButtonClick\" Text=\"Cancel\" Visible=\"false\" />",
        true)]
    [InlineData(
        "<RadzenButton ButtonStyle=\"ButtonStyle.Light\" Variant=\"Variant.Flat\"  Click=\"@CancelButtonClick\" Text=\"Cancel\" Visible=false />",
        true)]
    [InlineData(
        "<RadzenButton ButtonStyle=\"ButtonStyle.Light\" Variant=\"Variant.Flat\" Click=\"CancelButtonClick\" Visible=false  Text=\"Cancel\" />",
        true)]
    [InlineData(
        "<RadzenButton ButtonStyle=\"ButtonStyle.Primary\" ButtonType=\"ButtonType.Submit\" Icon=\"save\" Text=\"@D[\"Button.Save\"]\" Variant=\"Variant.Flat\" />",
        false)]
    [InlineData(
        "<RadzenButton ButtonStyle=\"ButtonStyle.Primary\" ButtonType=\"ButtonType.Submit\" Icon=\"save\" Text=\"Save\" Variant=\"Variant.Flat\" />",
        true)]
    [InlineData("<RadzenText Text=\"Welcome!\" TextStyle=\"Radzen.Blazor.TextStyle.H2\" class=\"rz-mt-4 rz-mt-md-12 rz-pt-0 rz-pt-md-12 rz-mb-6 rz-color-white rz-display-none rz-display-sm-block\" />\n",
        true)]
    [InlineData("<RadzenText Text=\"Welcome\" />",
        true)]
    public async Task ProcessFragment(string fragment, bool shouldChange)
    {
        // Arrange
        var razorProcessor = new RazorProcessor(_loggerMock.Object, _config, _customActions, _backupService.Object);
        var result = razorProcessor.ProcessCustomActions(_customActions, fragment, "test");

        // Act
        var changed = result != fragment;

        // Assert

        Assert.Equal(shouldChange, changed);
    }

    [Theory]
    [InlineData("<RadzenAlert Click=\"@CancelButtonClick\" Visible=\"false\" >Some text</RadzenAlert>", true)]
    [InlineData("<RadzenAlert Click=\"@CancelButtonClick\" Visible=false >@D[\"Alert.SomeText\"]</RadzenAlert>", false)]
    public async Task ProcessRadzenAlertFragment(string fragment, bool shouldChange)
    {
        var customActions = new CustomActions(_resourceKeys);
        customActions.Actions.Clear();
        customActions.Actions.Add(customActions.RadzenAlertTextAction);

        // Arrange
        var razorProcessor = new RazorProcessor(_loggerMock.Object, _config, customActions, _backupService.Object);
        var result = razorProcessor.ProcessCustomActions(customActions, fragment, "test");

        // Act
        var changed = result != fragment;

        // Assert
        Assert.Equal(shouldChange, changed);
    }
    
    [Theory]
    [InlineData(@"<RadzenText TextStyle=""Radzen.Blazor.TextStyle.Body1"" class=""rz-mb-12 rz-pb-0 rz-pb-md-12 rz-color-white rz-display-none rz-display-sm-block"" >
                       Fill in your login credentials to proceed.
                  </RadzenText>", true)]
    [InlineData(@"<RadzenText>Fill in your login credentials to proceed.</RadzenText>", true)]
    
    public async Task ProcessRadzenTextFragment(string fragment, bool shouldChange)
    {
        var customActions = new CustomActions(_resourceKeys);
        var action = customActions.RadzenTextContentAction;
     //   action.Regex = () => $@"<(?<tag>RadzenText)(\s+\S+)*(\s+>\n?(?<text>.*?)\n?<\/\k<tag>>|\s+\/>)";
       // action.Regex = () => $@"<(?<tag>RadzenText)(\s+\S+)*(\s+>([\s\S]*?)<\/\k<tag>>|\s+\/>)";

        customActions.Actions.Clear();
        customActions.Actions.Add(action);

        // Arrange
        var razorProcessor = new RazorProcessor(_loggerMock.Object, _config, customActions, _backupService.Object);
        var result = razorProcessor.ProcessCustomActions(customActions, fragment, "test");

        // Act
        var changed = result != fragment;

        // Assert
        Assert.Equal(shouldChange, changed);
    }
    
    
    
    
}
