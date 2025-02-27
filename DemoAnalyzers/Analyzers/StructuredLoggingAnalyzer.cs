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
            var methodName = (invocation.Expression as MemberAccessExpressionSyntax)?.Name.Identifier.Text;

            // Check if the method name starts with "Log" (e.g., LogInformation, LogError)
            if (methodName?.StartsWith("Log") == true && 
            context.SemanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol)
            {
            // Iterate through all arguments to check for string interpolation
            foreach (var argument in invocation.ArgumentList.Arguments)
            {
                var argumentType = context.SemanticModel.GetTypeInfo(argument.Expression).Type;
                if (argumentType?.SpecialType == SpecialType.System_String &&
                argument.Expression is InterpolatedStringExpressionSyntax)
                {
                // Report a diagnostic if string interpolation is used
                context.ReportDiagnostic(
                    Diagnostic.Create(Rule, argument.Expression.GetLocation()));
                }
            }
            }
        }
    }
}