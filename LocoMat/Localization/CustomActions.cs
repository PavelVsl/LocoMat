namespace LocoMat.Localization;

public class CustomActions
{
    private readonly ResourceKeys _resourceKeys;
    private Dictionary<string, string> _vars = new();
    public List<CustomAction> Actions { get; }

    public CustomActions(ResourceKeys resourceKeys)
    {
        _resourceKeys = resourceKeys;

        Actions = new List<CustomAction>
        {
            new()
            {
                // If the tag is existing localizer call, try add the key to the resourceKeys dictionary
                // and return the tag unchanged
                ComponentType = "ExistingLocalizerCall",
                Regex = () => @"(?<=@D\["")[^""]+",
                Action = match =>
                {
                    var tag = match.Value;
                    _resourceKeys.TryAdd(tag, "# " + tag.SplitCamelCase());
                    return tag;
                },
            },

            new()
            {
                //Localize the tag content if it is not an existing localizer call
                ComponentType = "Content",
                Regex = () => @"(?<=<\w+>(?![^<]*?@)[^<]*?)\b(?:\w+\s*)+\b(?=[^>]*</\w+>)",
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
            },

            new()
            {
                //Take class name from TItem attributre of RadzenTemplateForm
                ComponentType = "RadzenTemplateForm",
                Action = match =>
                {
                    var tag = match.Value;
                    _vars["TItem"] = tag.GetClassNameFromTag("TItem");
                    return tag;
                },
            },
            new()
            {
                //Localize RadzenDropDownDataGridColumn
                ComponentType = "RadzenDropDownDataGridColumn",
                Action = match => match.Value.ReplaceGridColumnStrings(_resourceKeys),
            },
            new()
            {
                //Localize RadzenDataGridColumn
                ComponentType = "RadzenDataGridColumn",
                Action = match => match.Value.ReplaceGridColumnStrings(_resourceKeys),
            },
            new()
            {
                //Localize RadzenLabel
                ComponentType = "RadzenLabel",
                Action = match =>
                {
                    var tag = match.Value;
                    var attributeValue = tag.GetAttributeValue("Component");
                    if (string.IsNullOrEmpty(attributeValue)) return tag;
                    var key = $"{_vars["TItem"]}.{attributeValue}";
                    return tag.ReplaceAttributeWithKey(_resourceKeys, "Text", key);
                },
            },
            new()
            {
                //Localize RadzenLabel
                ComponentType = "RadzenFormField",
                Action = match =>
                {
                    var tag = match.Value;
                    var attributeValue = tag.GetAttributeValue("Text").GenerateResourceKey();
                    if (string.IsNullOrEmpty(attributeValue)) return tag;
                    var key = $"{_vars["TItem"]}.{attributeValue}";
                    return tag.ReplaceAttributeWithKey(_resourceKeys, "Text", key);
                },
            },
            new()
            {
                //Localize RadzenLabel
                ComponentType = "RadzenText",
                Action = match =>
                {
                    var tag = match.Value;
                    var attributeValue = tag.GetAttributeValue("Text").GenerateResourceKey();
                    if (string.IsNullOrEmpty(attributeValue)) return tag;

                    var key = $"{_vars["className"]}.{attributeValue}";
                    return tag.ReplaceAttributeWithKey(_resourceKeys, "Text", key);
                },
            },
            new()
            {
                //Localize RadzenRequiredValidator
                ComponentType = "RadzenRequiredValidator",
                Action = match =>
                {
                    var tag = match.Value;
                    var component = tag.GetAttributeValue("Component");
                    var key = $"{_vars["TItem"]}.{component}.RequiredValidator";
                    return tag.ReplaceAttributeWithKey(_resourceKeys, "Text", key);
                },
            },
            new()
            {
                //Localize RadzenButton
                ComponentType = "RadzenButton",
                Action = match =>
                {
                    var tag = match.Value;
                    var text = tag.GetAttributeValue("Text");
                    if (string.IsNullOrEmpty(text)) return tag;
                    var key = $"Button.{text}";
                    return tag.ReplaceAttributeWithKey(_resourceKeys, "Text", key);
                },
            },
            new()
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
            },

            new()
            {
                ComponentType = "RadzenGridColumn",
                Regex = () => @"NotificationService\.Notify\(new NotificationMessage\s*\{\s*Severity\s*=\s*NotificationSeverity\.Error,\s*Summary\s*=\s*`(?<error>[^`]+)`,\s*Detail\s*=\s*`(?<detail>[^`]+)`\s*\}\);",
                Action = match =>
                {
                    var tag = match.Value;
                    var text = tag.GetAttributeValue("Title");
                    if (string.IsNullOrEmpty(text)) return tag;
                    var key = $"{_vars["TItem"]}.{text}";
                    return tag.ReplaceAttributeWithKey(_resourceKeys, "Title", key);
                },
            },
        };
    }

    public void SetVariable(string key, string value)
    {
        _vars[key] = value;
    }
}