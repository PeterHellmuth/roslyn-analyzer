using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NullOperatorsAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DEMO002";

    private static readonly LocalizableString Title = "Null operator usage detected";
    private static readonly LocalizableString MessageFormat = "Null operator '{0}' is used";
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

        context.RegisterSyntaxNodeAction(AnalyzeNullCoalescing, SyntaxKind.CoalesceExpression);
        context.RegisterSyntaxNodeAction(AnalyzeNullCoalescingAssignment, SyntaxKind.CoalesceAssignmentExpression);
        context.RegisterSyntaxNodeAction(AnalyzeNullForgivingOperator, SyntaxKind.SuppressNullableWarningExpression);
    }

    private static void AnalyzeNullCoalescing(SyntaxNodeAnalysisContext context)
    {
        var node = (BinaryExpressionSyntax)context.Node;
        context.ReportDiagnostic(
            Diagnostic.Create(Rule, node.OperatorToken.GetLocation(), "??"));
    }

    private static void AnalyzeNullCoalescingAssignment(SyntaxNodeAnalysisContext context)
    {
        var node = (AssignmentExpressionSyntax)context.Node;
        context.ReportDiagnostic(
            Diagnostic.Create(Rule, node.OperatorToken.GetLocation(), "??="));
    }

    private static void AnalyzeNullForgivingOperator(SyntaxNodeAnalysisContext context)
    {
        var node = (PostfixUnaryExpressionSyntax)context.Node;
        if (node.OperatorToken.IsKind(SyntaxKind.ExclamationToken))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rule, node.OperatorToken.GetLocation(), "!"));
        }
    }
}