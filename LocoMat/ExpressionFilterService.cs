using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace LocoMat
{
    public class ExpressionFilterService
    {
        private readonly ConfigurationData _config;
        private readonly ILogger<ExpressionFilterService> _logger;
        private readonly ILiteralFilter _literalFilter;

        public ExpressionFilterService(ConfigurationData config, ILogger<ExpressionFilterService> logger, ILiteralFilter literalFilter)
        {
            _config = config;
            _logger = logger;
            _literalFilter = literalFilter;
        }

        public bool IsLocalizable(LiteralExpressionSyntax literal)
        {
            if (literal == null) return false;
            return !_literalFilter.IsProhibited(literal);
        }
    }

    public interface ILiteralFilter
    {
        string Name { get; }
        string Description { get; }
        bool IsProhibited(LiteralExpressionSyntax literal);
    }

    public class LiteralFilters : List<ILiteralFilter>, ILiteralFilter
    {
        private readonly ILogger<LiteralFilters> _logger;

        public LiteralFilters(ILogger<LiteralFilters> logger)
        {
            _logger = logger;
            Add(new AttributeDeclarationArgumentFilter());
            Add(new InterpolatedStringFilter());
            Add(new IndexerFilter());
            Add(new MemberAccessExpressionFilter());
            Add(new SwitchStatementLabelFilter());
            Add(new NamedArgumentFilter());
            Add(new VariableDeclarationFilter());
            Add(new TooShortLiteralFilter());
            Add(new TooLongLiteralFilter());
            Add(new VerbatimStringFilter());
            Add(new StringsWithEscapeSequencesFilter());
            Add(new MethodCallFilter()); 
            Add(new MethodCallFilter("Query")); 
            Add(new MethodCallFilter("NavigateTo")); 
            Add(new EmptyOrWhitespaceLiteralFilter());
            Add(new InitializerExpressionFilter());
            Add(new StringInDictionaryFilter()); 
            Add(new BinaryExpressionFilter());
            Add( new StringsWithSlashFilter());
        }


        public string Name => "Composite Filter";
        public string Description => "Composite filter containing several individual filters";

        public bool IsProhibited(LiteralExpressionSyntax literal)
        {
            foreach (var filter in this)
            {
                if (filter.IsProhibited(literal))
                {
                    _logger.LogDebug($"Literal '{literal}' is not localizable because of filter '{filter.Name}'");
                    return true;
                }
            }

            return false;
        }
    }
  
    public abstract class BaseLiteralFilter : ILiteralFilter
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract bool IsProhibited(LiteralExpressionSyntax literal);
    }
    
    // filter literal in cases like if (args.Value == "Logout") {}
    public class BinaryExpressionFilter : BaseLiteralFilter
    {
        public override string Name => "Logical expression";
        public override string Description => "Literals used in logical expressions";

        public override bool IsProhibited(LiteralExpressionSyntax literal)
        {
            return literal.Parent is BinaryExpressionSyntax binaryExpression;
        }
    }
    // filter literal in initializers like new Dictionary<string, object> { {"Id", args.Id} }
    public class InitializerExpressionFilter : BaseLiteralFilter
    {
        public override string Name => "Initializer expression";
        public override string Description => "Literals used in initializer expressions";

        public override bool IsProhibited(LiteralExpressionSyntax literal)
        {
            return literal.Parent is InitializerExpressionSyntax;
        }
    }
    
    

    public class StringInDictionaryFilter : BaseLiteralFilter
    {
        public override string Name => "String in dictionary access";
        public override string Description => "String literals used in dictionary access";

        public override bool IsProhibited(LiteralExpressionSyntax literal)
        {
            return literal.Parent is ArgumentSyntax &&
                   literal.Parent.Parent is BracketedArgumentListSyntax &&
                   //literal.Parent.Parent.Parent is ElementAccessExpressionSyntax &&
                   literal.Token.Value is string;
        }
    }
    public class AttributeDeclarationArgumentFilter : BaseLiteralFilter
    {
        public override string Name => "Attribute declarations arguments";
        public override string Description => "Literals used as arguments in attribute declarations";

        public override bool IsProhibited(LiteralExpressionSyntax literal)
        {
            return literal.Parent is AttributeArgumentSyntax;
        }
    }

    // Interpolation and Indexer Related Filters
    public class InterpolatedStringFilter : BaseLiteralFilter
    {
        public override string Name => "Interpolated strings";
        public override string Description => "Literals used in interpolated strings";

        public override bool IsProhibited(LiteralExpressionSyntax literal)
        {
            return literal.Parent is InterpolatedStringExpressionSyntax
                   || literal.Parent is InterpolatedStringTextSyntax
                   || literal.Parent is InterpolationSyntax
                   || literal.Parent is InterpolationAlignmentClauseSyntax;
        }
    }

    public class IndexerFilter : BaseLiteralFilter
    {
        public override string Name => "Indexer";
        public override string Description => "Literals used in indexer";

        public override bool IsProhibited(LiteralExpressionSyntax literal)
        {
            return literal.Parent is ElementAccessExpressionSyntax;
        }
    }

    // Syntax Related Filters
    public class MemberAccessExpressionFilter : BaseLiteralFilter
    {
        public override string Name => "Member access expressions";
        public override string Description => "Literals used in member access expressions";

        public override bool IsProhibited(LiteralExpressionSyntax literal)
        {
            return literal.Parent is MemberAccessExpressionSyntax;
        }
    }

    public class SwitchStatementLabelFilter : BaseLiteralFilter
    {
        public override string Name => "Switch statements labels";
        public override string Description => "Literals used as labels in switch statements (excluding case pattern switch labels)";

        public override bool IsProhibited(LiteralExpressionSyntax literal)
        {
            return literal.Parent is SwitchLabelSyntax && !(literal.Parent is CasePatternSwitchLabelSyntax);
        }
    }

    public class NamedArgumentFilter : BaseLiteralFilter
    {
        public override string Name => "Named arguments";
        public override string Description => "Literals used as names in named arguments";

        public override bool IsProhibited(LiteralExpressionSyntax literal)
        {
            return literal.Parent is NameColonSyntax || literal.Parent is NameEqualsSyntax;
        }
    }

    public class VariableDeclarationFilter : BaseLiteralFilter
    {
        public override string Name => "Variable declarations";
        public override string Description => "Literals used as names in variable declarations";

        public override bool IsProhibited(LiteralExpressionSyntax literal)
        {
            return literal.Parent is VariableDeclaratorSyntax;
        }
    }

// Length Related Filters
    public class TooShortLiteralFilter : BaseLiteralFilter
    {
        public override string Name => "Too short literals";
        public override string Description => "Literals that are too short (less than 3 characters)";

        public override bool IsProhibited(LiteralExpressionSyntax literal)
        {
            return literal.Token.ValueText.Length < 3;
        }
    }

    public class TooLongLiteralFilter : BaseLiteralFilter
    {
        public override string Name => "Too long literals";
        public override string Description => "Literals that are too long (more than 150 characters)";

        public override bool IsProhibited(LiteralExpressionSyntax literal)
        {
            return literal.Token.ValueText.Length > 150;
        }
    }

// Verbatim String and Escape Sequence Filters
    public class VerbatimStringFilter : BaseLiteralFilter
    {
        public override string Name => "Verbatim strings";
        public override string Description => "Literals that are verbatim strings";

        public override bool IsProhibited(LiteralExpressionSyntax literal)
        {
            return literal.Token.Text.StartsWith("@");
        }
    }

    public class StringsWithEscapeSequencesFilter : BaseLiteralFilter
    {
        public override string Name => "Strings with escape sequences";
        public override string Description => "Literals that are strings with escape sequences";

        public override bool IsProhibited(LiteralExpressionSyntax literal)
        {
            return literal.Token.ValueText.Contains("\\");
        }
    }
    //string with / characters
    public class StringsWithSlashFilter : BaseLiteralFilter
    {
        public override string Name => "Strings with slash";
        public override string Description => "Literals that are strings with slash";

        public override bool IsProhibited(LiteralExpressionSyntax literal)
        {
            return literal.Token.ValueText.Contains("/");
        }
    }
    

    public class MethodCallFilter : BaseLiteralFilter
    {
        private readonly string[] _methodNames = { "ToString", "Format" };

        public MethodCallFilter(string[] methodNames = null)
        {
            if (methodNames != null)
            {
                _methodNames = methodNames;    
            }
        }
        public MethodCallFilter(string methodName)
        {
            if (methodName != null) _methodNames = new string[] { methodName };
        } 
        
        public override string Name => "Method calls";
        public override string Description => "Literals used as arguments in specific method calls";

        public override bool IsProhibited(LiteralExpressionSyntax literal)
        {
            var invocation = literal?.Parent?.Parent?.Parent as InvocationExpressionSyntax;
            var memberAccess = invocation?.Expression as MemberAccessExpressionSyntax;
            var text = memberAccess?.Name.Identifier.Text;
            var isProhibited = text != null && _methodNames.Contains(text);
            return isProhibited;
        }
    }


// Whitespace Filter
    public class EmptyOrWhitespaceLiteralFilter : BaseLiteralFilter
    {
        public override string Name => "Empty or whitespace literals";
        public override string Description => "Literals that are empty or whitespace";

        public override bool IsProhibited(LiteralExpressionSyntax literal)
        {
            return string.IsNullOrWhiteSpace(literal.Token.ValueText);
        }
    }
}
