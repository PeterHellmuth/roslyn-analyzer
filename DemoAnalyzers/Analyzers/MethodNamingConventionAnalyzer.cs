using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace DemoAnalyzers.Analyzers
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
            DiagnosticSeverity.Error, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            [Rule];

        public override void Initialize(AnalysisContext context)
        {
            // Configure the analyzer to ignore generated code and enable concurrent execution
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            
            // Register a single syntax node action for method declarations and invocation expressions
            context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.MethodDeclaration, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
        {
            string? methodName = null;
            Location? location = null;

            // Check if the node is a method declaration or an invocation expression
            // and extract the method name and location accordingly
            if (context.Node is MethodDeclarationSyntax methodDeclaration)
            {
                methodName = methodDeclaration.Identifier.Text;
                location = methodDeclaration.Identifier.GetLocation();
            }
            else if (context.Node is InvocationExpressionSyntax invocation)
            {
                methodName = (invocation.Expression as MemberAccessExpressionSyntax)?.Name.Identifier.Text;
                location = invocation.GetLocation();
            }

            // If the method name is not null and starts with a lowercase letter, report a diagnostic
            // This checks for PascalCase naming convention
            if (methodName != null && char.IsLower(methodName[0]))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, location, methodName));
            }
        }
    }
}