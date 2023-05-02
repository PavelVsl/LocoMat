using System.Diagnostics;
using System.Text.RegularExpressions;
using BlazorLocalizer;
using static BlazorLocalizer.RazorProcessor;
namespace BlazorLocalizerTests
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
            //await ResourceGenerator.TranslateResourceFile(SourceFile, "cs-CZ");
            

        }
        
    }
}
