using System.Diagnostics;
using System.Text.RegularExpressions;
using BlazorLocalizer;
using static BlazorLocalizer.RazorProcessor;

namespace BlazorLocalizerTests;

public class ReplaceTagAttributesTests
{
    // Arrange
    private static Dictionary<string, string> modelKeys = new();
    private static string formClassTItem = null;
    private static string ComponentType = "TestComponent";


    [Fact]
    //Translate Resource File test
    public async Task TranslateResourceFileTest()
    {
        // Arrange
        var SourceFile = "/Users/pavel/projects/sg/igp/src/igp/Resources/Resources.resx";
        //Translator.targetLanguage = "cs-CZ";
        //Act
        //await ResourceGenerator.TranslateResourceFile(SourceFile, "cs-CZ");
    }
}