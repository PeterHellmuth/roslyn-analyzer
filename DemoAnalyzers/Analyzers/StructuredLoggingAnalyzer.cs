using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using System.Composition;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using System;

namespace DemoAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class StructuredLoggingAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DEMO003";
        private static readonly LocalizableString Title = "Structured logging required";
        private static readonly LocalizableString MessageFormat = "Use structured logging templates instead of string interpolation";
        private const string Category = "Logging";

        private static readonly DiagnosticDescriptor Rule = new(
            DiagnosticId, Title, MessageFormat, Category,
            DiagnosticSeverity.Error, isEnabledByDefault: true,
            description: "Log messages should use structured templates instead of string interpolation.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
            ImmutableArray.Create(Rule);

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
                context.SemanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol method &&
                method.Parameters.Length > 0 &&
                method.Parameters[0].Type.SpecialType == SpecialType.System_String)
            {
                // Check if the first argument is a string interpolation
                var firstArg = invocation.ArgumentList.Arguments[0].Expression;
                if (firstArg is InterpolatedStringExpressionSyntax)
                {
// Report a diagnostic if string interpolation is used
                    context.ReportDiagnostic(
                        Diagnostic.Create(Rule, firstArg.GetLocation()));
                }
            }
        }
    }
}