using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace LocoMat.Localization
{
    public class CustomActions
    {
        private readonly ResourceKeys _resourceKeys;
        private Dictionary<string, string> _vars = new();

        public CustomAction ExistingLocalizerCallAction => new CustomAction
        {
            ComponentType = "ExistingLocalizerCall",
            Regex = () => @"(?<=@D\["")[^""]+",
            Action = match =>
            {
                var tag = match.Value;
                
                //take part of tag after '.'
                var value = tag.Split('.').Last();
                _resourceKeys.TryAdd(tag, value.SplitCamelCase());
                return tag;
            },
        };

        public CustomAction RadzenTextOrHeadingAction => new CustomAction
        {
            //ComponentType = "Radzen[Text|Heading]",
            ComponentType = "Radzen(?:Text|Heading)\\b",
            Action = match => ProcessAttributeReplacement(match, "Text"),
        };

        public CustomAction RadzenFileInputAction => new CustomAction
        {
            ComponentType = "RadzenFileInput",
            Action = match => ProcessAttributeReplacement(match, "ChooseText"),
        };        
        
        public CustomAction RadzenTextBox => new CustomAction
        {
            ComponentType = "RadzenTextBox",
            Action = match => ProcessAttributeReplacement(match, "Placeholder"),
        };

        public CustomAction RadzenAlertTitleAction => new CustomAction
        {
            ComponentType = "RadzenAlert",
            Action = match => ProcessAttributeReplacement(match, "Title"),
        };

        public CustomAction ContentAction => new CustomAction
        {
            ComponentType = "Content",
            //Regex = () => @"(?<=<\w+>[^@<>]*?)\b(?:\w+\s*)+\b(?=[^<>]*?</\w+>)",
            Regex = () => @"<[^/>]+\s*>\s*(?<text>(?:(?!<\/[^>]+>).)*)<\/[^>]+>",
            Action = match =>
            {
                var tag = match.Value;
                var className = _vars["className"];
                var key = tag.GenerateResourceKey();
                if (string.IsNullOrEmpty(key)) return tag;
                key = $"{className}.{key}";
                _resourceKeys.TryAdd(key, tag.SplitCamelCase());
                return $"@D[\"{key}\"]";
            },
        };

        public CustomAction RadzenTemplateFormAction => new CustomAction
        {
            ComponentType = "RadzenTemplateForm",
            Action = match =>
            {
                var tag = match.Value;
                _vars["TItem"] = tag.GetClassNameFromTag("TItem");
                return tag;
            },
        };

        public CustomAction RadzenDropDownDataGridColumnAction => new CustomAction
        {
            ComponentType = "RadzenDropDownDataGridColumn",
            Action = match => match.Value.ReplaceGridColumnStrings(_resourceKeys),
        };

        public CustomAction RadzenDataGridColumnAction => new CustomAction
        {
            ComponentType = "RadzenDataGridColumn",
            Action = match => match.Value.ReplaceGridColumnStrings(_resourceKeys),
        };

        public CustomAction RadzenLabelAction => new CustomAction
        {
            ComponentType = "RadzenLabel",
            Action = match =>
            {
                var tag = match.Value;
                var attributeValue = tag.GetAttributeValue("Component");
                if (string.IsNullOrEmpty(attributeValue)) return tag;
                var key = $"{_vars["TItem"]}.{attributeValue}";
                return tag.ReplaceAttributeWithKey(_resourceKeys, "Text", key);
            },
        };

        public CustomAction RadzenFormFieldAction => new CustomAction
        {
            ComponentType = "RadzenFormField",
            Action = match =>
            {
                var tag = match.Value;
                var attributeValue = tag.GetAttributeValue("Text").GenerateResourceKey();
                if (string.IsNullOrEmpty(attributeValue)) return tag;
                var key = $"{_vars["TItem"]}.{attributeValue}";
                return tag.ReplaceAttributeWithKey(_resourceKeys, "Text", key);
            },
        };

        public CustomAction RadzenValidatorAction => new CustomAction
        {
            ComponentType = "Radzen(?<type>\\w+)Validator",
            Action = match =>
            {
                var tag = match.Value;
                var type = match.Groups["type"].Value;
                var component = tag.GetAttributeValue("Component");
                var key = $"{_vars["TItem"]}.{component}.{type}Validator";
                return tag.ReplaceAttributeWithKey(_resourceKeys, "Text", key);
            },
        };

        public CustomAction RadzenButtonAction => new CustomAction
        {
            ComponentType = "Radzen(?:Button|SplitButton)\\b",
            //Regex = () => $@"<(?<tag>RadzenButton)(\s+\S+)*/?>",
            Action = match =>
            {
                var tag = match.Value;
                var text = tag.GetAttributeValue("Text");
                if (string.IsNullOrEmpty(text)) return tag;
                var key = $"Button.{text}";
                return tag.ReplaceAttributeWithKey(_resourceKeys, "Text", key);
            },
        };

        public CustomAction RadzenPanelMenuItemAction => new CustomAction
        {
            ComponentType = "RadzenPanelMenuItem",
            Action = match =>
            {
                var tag = match.Value;
                var text = tag.GetAttributeValue("Text");
                if (string.IsNullOrEmpty(text)) return tag;
                var key = $"Menu.{text}";
                return tag.ReplaceAttributeWithKey(_resourceKeys, "Text", key);
            },
        };

        public CustomAction RadzenProfileMenuItemAction => new CustomAction
        {
            ComponentType = "RadzenProfileMenuItem",
            Action = match =>
            {
                var tag = match.Value;
                var text = tag.GetAttributeValue("Text");
                if (string.IsNullOrEmpty(text)) return tag;
                var key = $"Menu.{text}";
                return tag.ReplaceAttributeWithKey(_resourceKeys, "Text", key);
            },
        };

        public CustomAction RadzenAlertTextAction => new CustomAction
        {
            ComponentType = "RadzenAlert",
            //Regex = () => $@"<(?<tag>RadzenAlert)(\s+\S+)*(\s*>\n?(?<text>.*?)\n?<\/\k<tag>>|\s+\/>)",
            Regex = () => $@"<RadzenAlert(?:(?!\s*\/>|>)[^>])*>(?<text>(?!\s*$)[^<]*)<",
            Localizer = key => $"@D[\"{key}\"]",
            Action = match =>
            {
                var tag = match.Value;
                var text = match.Groups["text"].Value?.Trim();
                if (string.IsNullOrEmpty(text)) return tag;
                if (text.StartsWith("@")) return tag;
                var key = $"Alert.{text.GenerateResourceKey()}";
                var localizedName = $"@D[\"{key}\"]";
                return tag.Replace(text, localizedName);
            },
        };

        public CustomAction RadzenTextContentAction => new CustomAction
        {
            ComponentType = "RadzenText",
            //Regex = () => $@"<(?<tag>RadzenText)(\s+\S+)*(\s*>\n?(?<text>.*?)\n?<\/\k<tag>>|\s+\/>)",
            //Regex = () => $@"<(?<tag>RadzenText)(\s+\S+)*\s*>(?<text>[^<]*?)<\/\k<tag>>",
            //Regex = () => $@"<(?<tag>RadzenText)(?!\s*\/)(\s+\S+)*\s*>(?<text>(?:(?!<[^>]+>).)*)<\/\k<tag>>",
            //Regex = () => $@"<[^/>]+\s*>\s*(?<text>(?:(?!<\/[^>]+>).)*)<\/[^>]+>",
            Regex = () => $@"<RadzenText(?:(?!\s*\/>|>)[^>])*>(?<text>(?!\s*$)[^<]*)<",

            Localizer = key => $"@D[\"{key}\"]",
            Action = match =>
            {
                var tag = match.Value;
                var text = match.Groups["text"].Value?.Trim();
                if (string.IsNullOrEmpty(text)) return tag;
                if (text.StartsWith("@")) return tag;
                var className = _vars["className"];
                var key = $"{className}.{text.GenerateResourceKey()}";
                var localizedName = $"@D[\"{key}\"]";
                return tag.Replace(text, localizedName);
            },
        };

        public List<CustomAction> Actions { get; }

        public CustomActions(ResourceKeys resourceKeys)
        {
            _resourceKeys = resourceKeys;

            Actions = new List<CustomAction>
            {
                ExistingLocalizerCallAction,
                RadzenTextOrHeadingAction,
                RadzenFileInputAction,
                RadzenAlertTitleAction,
                //ContentAction,
                RadzenTemplateFormAction,
                RadzenDropDownDataGridColumnAction,
                RadzenDataGridColumnAction,
                RadzenLabelAction,
                RadzenFormFieldAction,
                RadzenValidatorAction,
                RadzenButtonAction,
                RadzenPanelMenuItemAction,
                RadzenProfileMenuItemAction,
                RadzenAlertTextAction,
                RadzenTextContentAction,
            };
        }

        public void SetVariable(string key, string value)
        {
            _vars[key] = value;
        }

        private string ProcessAttributeReplacement(Match match, string attributeName)
        {
            var tag = match.Value;
            var attributeValue = tag.GetAttributeValue(attributeName).GenerateResourceKey();
            if (string.IsNullOrEmpty(attributeValue)) return tag;
            var key = $"{_vars["className"]}.{attributeValue}";
            return tag.ReplaceAttributeWithKey(_resourceKeys, attributeName, key);
        }
    }
}       
 
