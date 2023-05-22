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

    // filter literal in cases like if (args.Value == "Logout") {}
    // filter literal in initializers like new Dictionary<string, object> { {"Id", args.Id} }


    // Interpolation and Indexer Related Filters

    // Syntax Related Filters

    // Length Related Filters

    // Verbatim String and Escape Sequence Filters

    //string with / characters


    // Whitespace Filter
}
