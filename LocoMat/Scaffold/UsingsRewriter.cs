using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LocoMat.Scaffold;

public class UsingsRewriter : CSharpSyntaxRewriter
{
    private readonly string _nameSpace;

    public UsingsRewriter(string nameSpace) : base()
    {
        _nameSpace = nameSpace;
    }


    //add Using statement if not present
    public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
    {
        var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(_nameSpace));
        var usingDirectives = node.Usings.Add(usingDirective);
        return node.WithUsings(usingDirectives);
    }
}
/*
public class AddBuilderServicesCall : CSharpSyntaxRewriter
{
 private readonly string _nameSpace;

 public AddBuilderServicesCall(string nameSpace) : base()
 {
     _nameSpace = nameSpace;
 }
 public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
 {
     //Check if builder.Services.AddRadzenLocalization(); call is present
     var builderServicesCall = node.DescendantNodes().OfType<InvocationExpressionSyntax>().FirstOrDefault(x => x.Expression.ToString() == "builder.Services.AddRadzenLocalization()");
     if (builderServicesCall != null) return node;
     
     //Add builder.Services.AddRadzenLocalization(); call to GlobalStatements
     // just before builder.Build() call
     var builderServicesCallStatement = SyntaxFactory.ParseStatement("builder.Services.AddRadzenLocalization();");
     var globalStatements = node.DescendantNodes().OfType<GlobalStatementSyntax>().ToList();
     var builderBuildCall = node.DescendantNodes().OfType<InvocationExpressionSyntax>().FirstOrDefault(x => x.Expression.ToString() == "builder.Build()");
     // var index = globalStatements.IndexOf(builderBuildCall.Parent as GlobalStatementSyntax);
     // globalStatements.Insert(index, builderServicesCallStatement);
     // return node.WithGlobalStatements(SyntaxFactory.List(globalStatements));
 }
        

}
*/