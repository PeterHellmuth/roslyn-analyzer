using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public class NullOperatorCodeFixProvider : CodeFixProvider
{
    private const string DiagnosticId = "DEMO002";
    
    public override ImmutableArray<string> FixableDiagnosticIds => 
        ImmutableArray.Create(DiagnosticId);

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        var diagnostic = context.Diagnostics.FirstOrDefault(d => d.Id == DiagnosticId);

        if (diagnostic == null || root == null) return;

        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var node = root.FindNode(diagnosticSpan);
        
        if (node is BinaryExpressionSyntax coalesceExpression)
        {
            await RegisterCompleteFix(context, coalesceExpression);
        }
    }

    private async Task RegisterCompleteFix(CodeFixContext context, BinaryExpressionSyntax coalesceExpression)
    {
        var document = context.Document;
        var semanticModel = await document.GetSemanticModelAsync(context.CancellationToken);
        
        if (coalesceExpression.Left is IdentifierNameSyntax identifier &&
            semanticModel.GetSymbolInfo(identifier).Symbol is ILocalSymbol localSymbol)
        {
            var declaration = await FindVariableDeclarationAsync(document, localSymbol);
            
            if (declaration != null && IsNullableReferenceType(declaration))
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Make non-nullable and remove coalescing",
                        createChangedDocument: c => ApplyCompleteFix(document, declaration, coalesceExpression, c),
                        equivalenceKey: "FullNullFix"),
                    context.Diagnostics);
            }
        }
    }

    private async Task<VariableDeclaratorSyntax?> FindVariableDeclarationAsync(Document document, ILocalSymbol symbol)
    {
        var root = await document.GetSyntaxRootAsync();
        return root?.DescendantNodes()
            .OfType<VariableDeclaratorSyntax>()
            .FirstOrDefault(v => v.Identifier.Text == symbol.Name);
    }

    private bool IsNullableReferenceType(VariableDeclaratorSyntax declaration)
    {
        return declaration.Parent is VariableDeclarationSyntax variableDeclaration &&
               variableDeclaration.Type is NullableTypeSyntax;
    }

    private async Task<Document> ApplyCompleteFix(
        Document document,
        VariableDeclaratorSyntax declaration,
        BinaryExpressionSyntax coalesceExpression,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken);
        if (root == null) throw new ArgumentNullException(nameof(root));
        var editor = new SyntaxEditor(root, document.Project.Solution.Services);

        // 1. Fix variable declaration
        var variableDeclaration = (VariableDeclarationSyntax)declaration.Parent!;
        var nullableType = (NullableTypeSyntax)variableDeclaration.Type;
        
        var newType = nullableType.ElementType
            .WithTrailingTrivia(nullableType.GetTrailingTrivia());
        
        var defaultValue = GetDefaultValueExpression(nullableType.ElementType);
        var newInitializer = SyntaxFactory.EqualsValueClause(defaultValue)
            .WithEqualsToken(SyntaxFactory.Token(SyntaxKind.EqualsToken))
            .WithLeadingTrivia(SyntaxFactory.Space);
        
        var newDeclarator = declaration
            .WithInitializer(declaration.Initializer ?? newInitializer);
        
        var newVariableDeclaration = variableDeclaration
            .WithType(newType)
            .WithVariables(SyntaxFactory.SeparatedList(new[] { newDeclarator }));
        
        editor.ReplaceNode(variableDeclaration, newVariableDeclaration);

        // 2. Remove null coalescing operator
        editor.ReplaceNode(
            coalesceExpression,
            coalesceExpression.Left
                .WithTriviaFrom(coalesceExpression)
                .WithAdditionalAnnotations(Simplifier.Annotation)
        );

        var newRoot = editor.GetChangedRoot();
        return document.WithSyntaxRoot(newRoot);
    }

    private ExpressionSyntax GetDefaultValueExpression(TypeSyntax type)
    {
        return type switch
        {
            PredefinedTypeSyntax predefined when predefined.Keyword.IsKind(SyntaxKind.StringKeyword) 
                => SyntaxFactory.ParseExpression("string.Empty"),
            _ => SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression)
        };
    }

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;
}