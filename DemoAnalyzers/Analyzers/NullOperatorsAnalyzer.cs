using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace DemoAnalyzers.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NullOperatorsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DEMO001";

        private static readonly LocalizableString Title = "Null operator usage detected";
        private static readonly LocalizableString MessageFormat = "Null operator '{0}' is used. Use a non-nullable value instead.";
        private const string Category = "NullHandling";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            [Rule];

        public override void Initialize(AnalysisContext context)
        {
            // Configure the analyzer to ignore generated code and enable concurrent execution
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Register the syntax node action for the null operators
            context.RegisterSyntaxNodeAction(AnalyzeNullCoalescing, SyntaxKind.CoalesceExpression);
            context.RegisterSyntaxNodeAction(AnalyzeNullCoalescingAssignment, SyntaxKind.CoalesceAssignmentExpression);
            context.RegisterSyntaxNodeAction(AnalyzeNullForgivingOperator, SyntaxKind.SuppressNullableWarningExpression);
        }

        private static void AnalyzeNullCoalescing(SyntaxNodeAnalysisContext context)
        {
            // Get the syntax node that triggered the diagnostic
            var node = (BinaryExpressionSyntax)context.Node;

            // Report a diagnostic for the null-coalescing operator
            context.ReportDiagnostic(
                Diagnostic.Create(Rule, node.OperatorToken.GetLocation(), "??"));
        }

        private static void AnalyzeNullCoalescingAssignment(SyntaxNodeAnalysisContext context)
        {
            // Get the syntax node that triggered the diagnostic
            var node = (AssignmentExpressionSyntax)context.Node;

            // Report a diagnostic for the null-coalescing assignment operator
            context.ReportDiagnostic(
                Diagnostic.Create(Rule, node.OperatorToken.GetLocation(), "??="));
        }

        private static void AnalyzeNullForgivingOperator(SyntaxNodeAnalysisContext context)
        {
            // Get the syntax node that triggered the diagnostic
            var node = (PostfixUnaryExpressionSyntax)context.Node;

            // Report a diagnostic for the null-forgiving operator
            if (node.OperatorToken.IsKind(SyntaxKind.ExclamationToken))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rule, node.OperatorToken.GetLocation(), "!"));
            }
        }
    }
}