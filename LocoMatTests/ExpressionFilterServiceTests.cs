using LocoMat;
using LocoMat.Localization;
using LocoMat.Localization.Filters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Moq;

namespace LocoMatTests;

public class ExpressionFilterServiceTests
{
    private readonly Mock<ILogger<LiteralFilters>> _loggerMock2;
    private readonly ConfigurationData _config = new() { };
    private readonly LiteralFilters _service;


    public ExpressionFilterServiceTests()
    {
        _loggerMock2 = new Mock<ILogger<LiteralFilters>>();
        _service = new LiteralFilters(_loggerMock2.Object);
    }


    public LiteralExpressionSyntax GetSampleSyntaxTree(string literalUseCaseCode)
    {
        // Arrange
        var code = @"
using System;

namespace MyApp
{
    public class MyClass
    {
        public async Task MyMethod()
        {" + literalUseCaseCode + @"
        }
    }
}
";
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        var literal = root.DescendantNodes()
            .OfType<LiteralExpressionSyntax>()
            .FirstOrDefault(literal => literal.Kind() == SyntaxKind.StringLiteralExpression);

        //write complete tree to file
        File.WriteAllText("/Users/pavel/Library/Application Support/JetBrains/Rider2023.1/scratches/x.cs", root.NormalizeWhitespace().ToFullString());
        return literal;
    }


    [Theory]
    [InlineData("var x = \"apple\";", true)]
    [InlineData("var x = \"apple\" + \"banana\";", false)]
    [InlineData("var x = @\"apple\"", false)]
    [InlineData("var x = \"\"", false)]
    [InlineData("var x = \"   \"", false)]
    [InlineData("var x = \"[apple]\"", true)]
    [InlineData("var x = \"/apple/\"", false)]
    [InlineData("var x = \"\\\\apple\\\\\"", false)]
    [InlineData("var x = D[\"apple\"]", false)]
    [InlineData("var x = \"Hello, world!\"", true)]
    [InlineData("var x = \"You have 1 message.\"", true)]
    [InlineData("var x = \"You have {0} messages.\"", true)]
    [InlineData("var x = \"Do you want to delete this file?\"", true)]
    [InlineData("var x = \"{firstName} {lastName}\"", true)]
    [InlineData("var x = \"Welcome \" + userName", false)]
    [InlineData("var x = \"Hello, {user.FullName}!\"", true)]
    [InlineData("var x = \"You have {0} message(s).\"", true)]
    [InlineData("string.Format(\"Welcome, {0}!\", userName)", false)]
    [InlineData("DialogService.OpenAsync<EditApplicationUser>(\"Edit Application User\", new Dictionary<string, object>{ {\"Id\", user.Id} });", true)]
    [InlineData("DialogService.OpenAsync<EditApplicationUser>(new Dictionary<string, object>{ {\"Id\", user.Id}, {\"Title\", \"Edit Application User\"} });", false)]
    [InlineData("DialogService.OpenAsync<EditApplicationUser>(new Dictionary<string, object>{ {\"Title\", \"Edit Application User\"}, {\"Width\", \"800px\"} });", false)]
    [InlineData("DialogService.OpenAsync<EditApplicationUser>(new Dictionary<string, object>{ {\"Titleeee\", \"Edit Application User\"}, {\"Width\", \"800px\"}, {\"Height\", \"600px\"} });", false)]
    [InlineData("var x = \"The {0} jumped over the {1}.\"", true)]
    public void IsLocalizable_TestLiterals(string inputLiteral, bool expectedResult)
    {
        // Arrange
        var literal = GetSampleSyntaxTree($"{inputLiteral};");
        // Act
        var result = !_service.IsProhibited(literal);
        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("[MyCustomAttribute(\"param\")]", true)]
    [InlineData("\"notAttributeArgument\"", false)]
    public void AttributeDeclarationArgumentFilterTests(string code, bool expectedResult)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetCompilationUnitRoot();
        var literal = root.DescendantNodes().OfType<LiteralExpressionSyntax>().Single();

        var filter = new AttributeDeclarationArgumentFilter();

        Assert.Equal(expectedResult, filter.IsProhibited(literal));
    }

    //TooShortLiteralFilterTests
    [Theory]
    [InlineData("var x = \"12\"", true)]
    [InlineData("var x = \"123\"", false)]
    public void TooShortLiteralFilter(string code, bool expectedResult)
    {
        var literal = GetSampleSyntaxTree($"{code}");

        var filter = new TooShortLiteralFilter();

        Assert.Equal(expectedResult, filter.IsProhibited(literal));
    }

    //VerbatimStringFilterTests
    [Theory]
    [InlineData("var x = @\"apple\"", true)]
    [InlineData("var x = \"apple\"", false)]
    public void VerbatimStringFilter(string code, bool expectedResult)
    {
        var literal = GetSampleSyntaxTree($"{code}");

        var filter = new VerbatimStringFilter();

        Assert.Equal(expectedResult, filter.IsProhibited(literal));
    }

    //StringsWithEscapeSequencesFilterTests
    [Theory]
    [InlineData("var x = \"otrhweopthowepr\\n sfahgk\"", false)]
    [InlineData("var x = \"otrhweopthowepr\\ sfahgk\"", false)]
    public void StringsWithEscapeSequencesFilter(string code, bool expectedResult)
    {
        var literal = GetSampleSyntaxTree($"{code}");

        var filter = new StringsWithEscapeSequencesFilter();

        Assert.Equal(expectedResult, filter.IsProhibited(literal));
    }

    //SwitchStatementLabelFilterTests
    [Theory]
    [InlineData(" switch { case \"apple\": break; }", true)]
    public void SwitchStatementLabelFilterTests(string code, bool expectedResult)
    {
        var literal = GetSampleSyntaxTree($"{code}");

        var filter = new SwitchStatementLabelFilter();

        Assert.Equal(expectedResult, filter.IsProhibited(literal));
    }

    //BinaryExpressionFilterTests
    [Theory]
    [InlineData("var x = D[\"apple\"]", false)]
    [InlineData("if (args.Value == \"Logout\"){}", true)]
    [InlineData("var x = D[\"apple\"] + \"banana\"", false)]
    public void BinaryExpressionFilterTest(string code, bool expectedResult)
    {
        var literal = GetSampleSyntaxTree($"{code};");

        var filter = new BinaryExpressionFilter();

        Assert.Equal(expectedResult, filter.IsProhibited(literal));
    }

    //MethodCallFilterTests
    [Theory]
    [InlineData("string.Format(\"Welcome, {0}!\", userName)", "Query", false)]
    [InlineData("SomeClass.Query(\"Welcome, {0}!\", userName)", "Query", true)]
    [InlineData("string.ToString(\"Welcome, {0}!\", userName)", null, true)]
    [InlineData("await DialogService.OpenAsync<AddApplicationUser>(\"Add Application User\");", null, false)]
    public void MethodCallFilterTests(string code, string method, bool expectedResult)
    {
        var literal = GetSampleSyntaxTree($"{code};");

        var filter = new MethodCallFilter(method);

        Assert.Equal(expectedResult, filter.IsProhibited(literal));
    }


    //InitializerExpressionFilter
    [Theory]
    [InlineData("DialogService.OpenAsync<EditApplicationUser>(aaaa, new Dictionary<string, object>{ {\"Id\", user.Id} });", true)]
    [InlineData("DialogService.OpenAsync<EditApplicationUser>(bbbb, new Dictionary<string, object>{ {\"Id\", user.Id}, {\"Title\", \"Edit Application User\"} });", true)]
    public void InitializerExpressionFilter(string code, bool expectedResult)
    {
        var literal = GetSampleSyntaxTree($"{code};");

        var filter = new InitializerExpressionFilter();

        Assert.Equal(expectedResult, filter.IsProhibited(literal));
    }
}