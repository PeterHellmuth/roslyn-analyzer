using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace DemoAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MethodNamingConventionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DEMO001";
        private static readonly LocalizableString Title = "Method naming convention";
        private static readonly LocalizableString MessageFormat = "Method name '{0}' should start with uppercase";
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new(
            DiagnosticId, Title, MessageFormat, Category,
            DiagnosticSeverity.Error, isEnabledByDefault: true,
            description: "Method names should follow PascalCase convention.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            [Rule];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;
            var methodName = method.Identifier.Text;

            if (!string.IsNullOrEmpty(methodName) && char.IsLower(methodName?[0] ?? '\0'))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rule, method.Identifier.GetLocation(), methodName));
            }
        }

        private static void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var methodName = (invocation.Expression as MemberAccessExpressionSyntax)?.Name.Identifier.Text;

            if (methodName != null && char.IsLower(methodName[0]))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rule, invocation.GetLocation(), methodName));
            }
        }
    }
}