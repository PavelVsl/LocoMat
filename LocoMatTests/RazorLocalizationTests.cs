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
    [InlineData("<RadzenButton ButtonStyle=\"ButtonStyle.Light\" Variant=\"Variant.Flat\"  Click=\"@CancelButtonClick\" Text=\"Cancel\" Visible=\"false\" />", true)]
    [InlineData("<RadzenButton ButtonStyle=\"ButtonStyle.Light\" Variant=\"Variant.Flat\"  Click=\"@CancelButtonClick\" Text=\"Cancel\" Visible=false />", true)]
    [InlineData("<RadzenButton ButtonStyle=\"ButtonStyle.Light\" Variant=\"Variant.Flat\" Click=\"CancelButtonClick\" Visible=false  Text=\"Cancel\" />", true)]
    [InlineData("<RadzenButton ButtonStyle=\"ButtonStyle.Primary\" ButtonType=\"ButtonType.Submit\" Icon=\"save\" Text=\"@D[\"Button.Save\"]\" Variant=\"Variant.Flat\" />", false)]
    [InlineData("<RadzenButton ButtonStyle=\"ButtonStyle.Primary\" ButtonType=\"ButtonType.Submit\" Icon=\"save\" Text=\"Save\" Variant=\"Variant.Flat\" />", true)]

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
    [InlineData("<RadzenButton ButtonStyle=\"ButtonStyle.Light\" Variant=\"Variant.Flat\"  Click=\"@CancelButtonClick\" Text=\"Cancel\" Visible=\"false\" />", true)]
    [InlineData("<RadzenButton ButtonStyle=\"ButtonStyle.Light\" Variant=\"Variant.Flat\"  Click=\"@CancelButtonClick\" Text=\"Cancel\" Visible=false />", true)]
    [InlineData("<RadzenButton ButtonStyle=\"ButtonStyle.Light\" Variant=\"Variant.Flat\" Click=\"CancelButtonClick\" Visible=false  Text=\"Cancel\" />", true)]
    [InlineData("<RadzenButton ButtonStyle=\"ButtonStyle.Primary\" ButtonType=\"ButtonType.Submit\" Icon=\"save\" Text=\"@D[\"Button.Save\"]\" Variant=\"Variant.Flat\" />", false)]
    [InlineData("<RadzenButton ButtonStyle=\"ButtonStyle.Primary\" ButtonType=\"ButtonType.Submit\" Icon=\"save\" Text=\"Save\" Variant=\"Variant.Flat\" />", true)]

    public async Task ProcessRadzenButtonFragmentFull(string fragment, bool shouldChange)
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
    customActions.Actions.Add(new CustomAction
    {
        ComponentType = "RadzenAlert",
        Regex = () => $@"<(?<tag>RadzenAlert)(\s+\S+)*/?>(?<text>.*?)<\/\k<tag>>",
        Localizer = key => $"@D[\"{key}\"]",
        Action = match =>
        {
            var tag = match.Value;
            var text = match.Groups["text"].Value;
            if (string.IsNullOrEmpty(text)) return tag;
            if (text.StartsWith("@")) return tag;
            var key = $"Alert.{text.GenerateResourceKey()}";
            var localizedName = $"@D[\"{key}\"]";
            return "Matched:" + tag.Replace(text, localizedName);
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
    
    
    
    
}
