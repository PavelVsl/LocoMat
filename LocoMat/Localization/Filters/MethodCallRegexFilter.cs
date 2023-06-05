using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LocoMat;

public class MethodCallRegexFilter : BaseLiteralFilter
{
    private readonly Regex _regEx;

    public MethodCallRegexFilter(string regEx)
    {
        _regEx = new Regex(regEx, RegexOptions.Compiled);
    }
        
    public override string Name => "Method calls with regex";
    public override string Description => "Literals used as arguments in specific method calls with regex";

    public override bool IsProhibited(LiteralExpressionSyntax literal)
    {
        var invocation = literal.Ancestors().OfType<InvocationExpressionSyntax>().FirstOrDefault();
        var memberAccess = invocation?.Expression as MemberAccessExpressionSyntax;
        var text = memberAccess?.Name.Identifier.Text;
        var isProhibited = text != null &&  _regEx.IsMatch(text);
        return isProhibited;
    }
}