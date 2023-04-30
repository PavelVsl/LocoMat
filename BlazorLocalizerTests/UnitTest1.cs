using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using BlazorRazorLocalizer;
using static BlazorRazorLocalizer.RazorProcessor;
namespace RazorLocalizerTests
{
 

    public class ReplaceTagAttributesTests
    {
        // Arrange
        static Dictionary<string, string> modelKeys = new Dictionary<string, string>();
        static string formClassTItem = null;
        private static string ComponentType = "TestComponent";

        List<(string ComponentType, Func<string, string> CustomAction)> customActions = new List<(string ComponentType, Func<string, string> CustomAction)>
        {
            ("RadzenTemplateForm", (tag) =>
            {
                formClassTItem = GetClassNameFromTag(tag, "TItem");
                return tag;
            }),
            ("RadzenDropDownDataGridColumn", tag => ReplaceGridColumnStrings(tag,modelKeys)),
            ("RadzenDataGridColumn", tag => ReplaceGridColumnStrings(tag,modelKeys)),
            ("RadzenLabel", tag =>
            {
                var key = $"{formClassTItem}.{tag.GetAttributeValue("Component")}";
                return tag.ReplaceAttributeWithKey(modelKeys,"Text", key);
            }),
            ("RadzenRequiredValidator", tag =>
            {
                var component = tag.GetAttributeValue("Component");                    
                var key = $"{formClassTItem}.{component}.RequiredValidator";
                return tag.ReplaceAttributeWithKey(modelKeys,"Text", key);
            }),
            ("RadzenButton", tag =>
            {
                var text = tag.GetAttributeValue("Text");
                var key = $"Button.{text}";
                return tag.ReplaceAttributeWithKey(modelKeys,"Text", key);
            }),
        };

        [Fact]
        public void TestAttributePattern()
        {

            foreach (var example in attributes)
            {
                // Act
                var match = Regex.Match($"<RadzenButton {example.name}=\"{example.value}\" />", RazorProcessor.attributePattern);

                // Assert
                Assert.True(match.Success);
                Assert.Equal(example.name, match.Groups["name"].Value);
                Assert.Equal(example.value, match.Groups["value"].Value);
                Debug.WriteLine($"{example.name} : {example.value}    =>    {match.Groups["name"].Value} : {match.Groups["value"].Value}");
            }
        }
                    List<(string name, string value)> attributes = new List<(string name, string value)>
                    {
                        ("Text1", "Some text"),
                        
                        ("Data1", "@user"),
                        ("Visible1", "@(user != null)"),
                        ("Visible2", "@(user > 1)"),
                        ("Visible3", "@(user < 1)"),
                        ("Visible4", "@(user >= 1)"),
                        ("Visible5", "@(user <= 1)"),
                        ("Click1", "@(async () => await SaveProject())"),
                        ("Click2", "@(async () => await SaveProject(\"user\"))"),
                        ("@onclick:stopPropagation", "true"),
                        ("@onclick1:stopPropagation", "@true"),
                        ("Action", "@($\"account/login?redirectUrl={redirectUrl}\")"),
                        ("Data2", "@(\"login\")"),
                        ("Text2", @"@D[""Button.Save""]"),
                    };

        [Fact]
        public void ReplaceTagAttributesTest()
        {
            // Arrange
            var razorContent = @"<RadzenTemplateForm TItem=""Project"" Data=""@projects"" Submit=""@(async (Project project) => await SaveProject(project))"">";
            var expectedFormClassTItem = "Project";
            var expectedRazorContent = @"<RadzenTemplateForm TItem=""Project"" Data=""@projects"" Submit=""@(async (Project project) => await SaveProject(project))"">";
            
            //Act
            var newRazorContent = RazorProcessor.ReplaceTagAttributes(customActions, razorContent);
            
            //Assert
            Assert.Equal(expectedFormClassTItem, formClassTItem);
            Assert.Equal(expectedRazorContent, newRazorContent);
        }
        [Fact]
        //test RadzenTemplateForm tag
        public void ReplaceFormTagAttributes()
        {
            // Arrange
            var razorContent = @"<RadzenTemplateForm Data=""@user"" TItem=""Igp.Models.ApplicationUser"" Visible=""@(user != null)"" Submit=""@FormSubmit"">";
            var expectedRazorContent = @"<RadzenTemplateForm Data=""@user"" TItem=""Igp.Models.ApplicationUser"" Visible=""@(user != null)"" Submit=""@FormSubmit"">";
            var expectedFormClassTItem = "ApplicationUser";    
            //Act
            var newRazorContent = RazorProcessor.ReplaceTagAttributes(customActions, razorContent);
            
            
            //Assert
            Assert.Equal(expectedFormClassTItem, formClassTItem);
            Assert.Equal(expectedRazorContent, newRazorContent);
        }
        
        [Fact]
        //test button tag
        public void ReplaceButtonTagAttributes()
        {
            // Arrange
            var razorContent = @"<RadzenButton Text=""Save"" ButtonStyle=""ButtonStyle.Primary"" Click=""@(async () => await SaveProject())"" />";
            var expectedRazorContent = @"<RadzenButton Text=""@D[""Button.Save""]"" ButtonStyle=""ButtonStyle.Primary"" Click=""@(async () => await SaveProject())"" />";
            
            //Act
            var newRazorContent = RazorProcessor.ReplaceTagAttributes(customActions, razorContent);
            
            //Assert
            Assert.Equal(expectedRazorContent, newRazorContent);
        }
        
        [Fact]
        //test allready localized tag
        public void ReplaceLocalizedTagAttributes()
        {
            // Arrange
            var razorContent = @"<RadzenButton Text=""@D[""Button.Save""]"" ButtonStyle=""ButtonStyle.Primary"" Click=""@(async () => await SaveProject())"" />";
            var expectedRazorContent = @"<RadzenButton Text=""@D[""Button.Save""]"" ButtonStyle=""ButtonStyle.Primary"" Click=""@(async () => await SaveProject())"" />";
            
            //Act
            var newRazorContent = RazorProcessor.ReplaceTagAttributes(customActions, razorContent);
            
            //Assert
            Assert.Equal(expectedRazorContent, newRazorContent);
        }
        
        [Fact]
        //Translate Resource File test
        public async Task TranslateResourceFileTest()
        {
            // Arrange
            var SourceFile = "/Users/pavel/projects/sg/igp/src/igp/Resources/Resources.resx";
            Translator.targetLanguage = "cs-CZ";
            //Act
            await ResourceGenerator.TranslateResourceFile(SourceFile);
            

        }
        
    }
}
