using System.Text.RegularExpressions;
using BlazorLocalizer;

namespace BlazorLocalizerTests;

public class DialogServiceCustomActionTests
{
    private readonly CustomAction _customAction;
    private Dictionary<string, string> _vars;
    private string originalValue;

    public DialogServiceCustomActionTests()
    {
        _customAction = new CustomAction
        {
            ComponentType = "DialogService.OpenAsync",
            //Regex = () => (@"DialogService\.OpenAsync<[^\s""]+>\(""[^""\\]*(?:\\.[^""\\]*)*""\)"),
            Regex = () => @"DialogService\.OpenAsync<[^\s""]+>\(\""(?<value>[^""\\]*(?:\\.[^""\\]*)*)\""\)",
            FileType = ".cs",
            Action = tag =>
            {
                originalValue = tag.Value;
                var className = "ClassName";
                var key = tag.Groups["value"].Value;
                if (string.IsNullOrEmpty(key)) return tag.Value;
                key = $"{className}.{key}";
                return $"D[\"{key}\"]";
            },
        };
    }

    [Theory]
    [InlineData("1 await DialogService.OpenAsync<AddMeasurement>(\"Add Measurement\", null);", "1 await DialogService.OpenAsync<AddMeasurement>(D[\"AddMeasurement.Key\"], null);")]
    [InlineData("2 DialogService.OpenAsync<AddMeasurement>(\"Add Measurement\", null);", "2 DialogService.OpenAsync<AddMeasurement>(D[\"AddMeasurement.Key\"], null);")]
    [InlineData("3 DialogService.OpenAsync<MyComponent>(\"This is a message\")", "3 DialogService.OpenAsync<MyComponent>(D[\"MyComponent.Key\"])")]
    [InlineData("4 SomeOtherClass.OpenAsync<MyComponent>(\"This is a message\")", "4 SomeOtherClass.OpenAsync<MyComponent>(\"This is a message\")")]
    [InlineData("5 await DialogService.OpenAsync<AddMeasurement>(D[\"OtherClass.OtherKey\"], null);", "5 await DialogService.OpenAsync<AddMeasurement>(D[\"OtherClass.OtherKey\"], null);")]
    [InlineData("6 DialogService.Confirm(\"Are you sure you want to delete this record?\")", "6 DialogService.Confirm(D[\"Confirm.Key\"])")]
    public void Action_ReplaceLiteralWithResourceKey(string input, string expectedOutput)
    {
        // Arrange
//     var regexPattern = @"DialogService\.(?<method>[A-Za-z]+)<*(?<generic>[A-Za-z, ]*)>*\((?<literal>""[^""]*"")[A-Za-z<>(),]*\)";
        // var regexPattern = @"DialogService\.(?<method>[A-Za-z]+)<(?<generic>[A-Za-z, ]*)?>\((?<literal>""[^""]*"")[A-Za-z<>(),]*\)";
        var regexPattern = @"DialogService\.(?<method>[A-Za-z]+)<*(?<generic>[A-Za-z, ]*)>*\((?<literal>""[^""]*"")";


        var regex = new Regex(regexPattern);

        var key = "Resource.Key";
        // Act
        var result = regex.Replace(input, match =>
        {
            var className = !string.IsNullOrEmpty(match.Groups["generic"].Value) ? match.Groups["generic"].Value : match.Groups["method"].Value;
            var key = "Key";
            key = $"{className}.{key}";
            var value = match.Groups["literal"].Value;
            var localizer = $"D[\"{key}\"]";

            return match.Value.Replace(value, localizer);
        });

        // Assert
        Assert.Equal(expectedOutput, result);
    }
}