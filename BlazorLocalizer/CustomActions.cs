namespace BlazorLocalizer
{
    public class CustomActions
    {
        private readonly ResourceKeys _resourceKeys;
        private Dictionary<string, string> _vars = new();
        public List<CustomAction> Actions { get; }

        public CustomActions(ResourceKeys resourceKeys)
        {
            _resourceKeys = resourceKeys;
            Actions = new()
            {
                new CustomAction
                {
                    // If the tag is existing localizer call, try add the key to the resourceKeys dictionary
                    // and return the tag unchanged
                    ComponentType = "ExistingLocalizerCall",
                    Regex = () => @"(?<=@D\["")[^""]+",
                    Action = tag =>
                    {
                        _resourceKeys.TryAdd(tag, "# " + tag.SplitCamelCase());
                        return tag;
                    }
                },
                
                new CustomAction
                {
                    //Localize the tag content if it is not an existing localizer call
                    ComponentType = "Content",
                    Regex = () => @"(?<=<\w+>(?![^<]*?@)[^<]*?)\b(?:\w+\s*)+\b(?=[^>]*</\w+>)",
                    Action = tag =>
                    {
                        var className = _vars["className"];
                        var key = tag.GenerateResourceKey();
                        if (string.IsNullOrEmpty(key)) return tag;
                        key = $"{className}.{key}";
                        return $"@D[\"{key}\"]";
                    }
                },

                new CustomAction
                {
                    //Take class name from TItem attributre of RadzenTemplateForm
                    ComponentType = "RadzenTemplateForm",
                    Action = tag =>
                    {
                        _vars["TItem"] = tag.GetClassNameFromTag("TItem");
                        return tag;
                    }
                },
                new CustomAction
                {
                    //Localize RadzenDropDownDataGridColumn
                    ComponentType = "RadzenDropDownDataGridColumn",
                    Action = tag => tag.ReplaceGridColumnStrings(_resourceKeys)
                },
                new CustomAction
                {
                    //Localize RadzenDataGridColumn
                    ComponentType = "RadzenDataGridColumn",
                    Action = tag => tag.ReplaceGridColumnStrings(_resourceKeys)
                },
                new CustomAction
                {
                    //Localize RadzenLabel
                    ComponentType = "RadzenLabel",
                    Action = tag =>
                    {
                        var attributeValue = tag.GetAttributeValue("Component");
                        if(string.IsNullOrEmpty(attributeValue)) return tag;
                        var key = $"{_vars["TItem"]}.{attributeValue}";
                        return tag.ReplaceAttributeWithKey(_resourceKeys, "Text", key);
                    }
                },
                new CustomAction
                {
                    //Localize RadzenRequiredValidator
                    ComponentType = "RadzenRequiredValidator",
                    Action = tag =>
                    {
                        var component = tag.GetAttributeValue("Component");
                        var key = $"{_vars["TItem"]}.{component}.RequiredValidator";
                        return tag.ReplaceAttributeWithKey(_resourceKeys, "Text", key);
                    }
                },
                new CustomAction
                {
                    //Localize RadzenButton
                    ComponentType = "RadzenButton",
                    Action = tag =>
                    {
                        var text = tag.GetAttributeValue("Text");
                        if(string.IsNullOrEmpty(text)) return tag;
                        var key = $"Button.{text}";
                        return tag.ReplaceAttributeWithKey(_resourceKeys, "Text", key);
                    }
                },
                new CustomAction
                {
                    ComponentType = "RadzenPanelMenuItem",
                    Action = tag =>
                    {
                        var text = tag.GetAttributeValue("Text");
                        if(string.IsNullOrEmpty(text)) return tag;
                        var key = $"Menu.{text}";
                        return tag.ReplaceAttributeWithKey(_resourceKeys, "Text", key);
                    }
                }
            };
        }

        public void SetVariable(string key, string value)
        {
            _vars[key] = value;
        }
    }
}
