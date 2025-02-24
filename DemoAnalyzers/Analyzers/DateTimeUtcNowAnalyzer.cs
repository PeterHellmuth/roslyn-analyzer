using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace DemoAnalyzers.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DateTimeUtcNowAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DEMO002";
        private static readonly LocalizableString Title = "Use UtcNow instead of Now";
        private static readonly LocalizableString MessageFormat = "Replace DateTime.Now with DateTime.UtcNow";
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new(
            DiagnosticId, Title, MessageFormat, Category,
            DiagnosticSeverity.Error, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            [Rule];

        public override void Initialize(AnalysisContext context)
        {
            // Configure the analyzer to ignore generated code and enable concurrent execution
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            
            // Register a syntax node action to analyze member access expressions
            // This will check for DateTime.Now usage
            context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
        }

        private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
        {
            // Cast to member access expression (e.g., DateTime.Now)
            var memberAccess = (MemberAccessExpressionSyntax)context.Node;
            // Check if the member access is for DateTime.Now
            if (memberAccess.Expression is IdentifierNameSyntax identifier &&
                identifier.Identifier.Text == "DateTime" &&
                memberAccess.Name.Identifier.Text == "Now")
            {
                // Report a diagnostic if DateTime.Now is found
                var diagnostic = Diagnostic.Create(Rule, memberAccess.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

}