using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace DemoAnalyzers.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class StructuredLoggingAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DEMO002";
        private static readonly LocalizableString Title = "Structured logging required";
        private static readonly LocalizableString MessageFormat = "Use structured logging templates instead of string interpolation";

        private static readonly string HelpLinkUri = "https://learn.microsoft.com/en-us/dotnet/core/extensions/logging-library-authors#avoid-string-interpolation-in-logging";
        private const string Category = "Logging";

        private static readonly DiagnosticDescriptor Rule = new(
            DiagnosticId, Title, MessageFormat, Category,
            DiagnosticSeverity.Error, isEnabledByDefault: true,
            helpLinkUri: HelpLinkUri);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            [Rule];

        public override void Initialize(AnalysisContext context)
        {
            // Configure the analyzer to ignore generated code and enable concurrent execution
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            // Register a syntax node action to analyze invocation expressions
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            
            // Only analyze method calls with member access (e.g., logger.LogInformation)
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                return;

            var methodName = memberAccess.Name.Identifier.Text;
            if (!methodName.StartsWith("Log"))
                return;

            // Verify it's a method symbol
            if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol)
                return;

            // Iterate through the arguments of the method call
            foreach (var argument in invocation.ArgumentList.Arguments)
            {
                // Get the type information of the argument
                var typeInfo = context.SemanticModel.GetTypeInfo(argument.Expression);

                // Check if the argument is a string and an interpolated string expression
                if (typeInfo.Type is { SpecialType: SpecialType.System_String } && 
                    argument.Expression is InterpolatedStringExpressionSyntax)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(Rule, argument.Expression.GetLocation()));
                    break;
                }
            }
        }
    }
}