namespace LocoMat;

public class ExpressionInfo
{
    public string NameSpaceName { get; set; }
    public string ClassName { get; set; }
    public string MethodName { get; set; }
    public string GenericParameterName { get; set; }
    public string ElementName { get; set; }
    public string MatchString => $"{ClassName}.{MethodName}.{GenericParameterName}.{ElementName}";

    public override string ToString()
    {
        return $"{nameof(NameSpaceName)}: {NameSpaceName}, {nameof(ClassName)}: {ClassName}, {nameof(MethodName)}: {MethodName}, {nameof(GenericParameterName)}: {GenericParameterName}, {nameof(ElementName)}: {ElementName}";
    }
}