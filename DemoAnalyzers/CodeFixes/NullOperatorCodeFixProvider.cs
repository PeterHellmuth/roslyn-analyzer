using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

using DemoAnalyzers.Analyzers;

namespace DemoAnalyzers.CodeFixes
    {
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public class NullOperatorCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => [NullOperatorsAnalyzer.DiagnosticId];

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            // Get the syntax root of the document
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);

            // Get the first diagnostic from the context
            var diagnostic = context.Diagnostics.First();

            // Find the node that triggered the diagnostic
            if (diagnostic == null || root == null) return;
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var node = root.FindNode(diagnosticSpan);
            
            // Register a code fix for the null operator
            switch (node)
            {
                case BinaryExpressionSyntax coalesceExpression:
                    await RegisterVariableFix(context, coalesceExpression.Left, coalesceExpression);
                    break;
                    
                case AssignmentExpressionSyntax coalesceAssignment 
                    when coalesceAssignment.IsKind(SyntaxKind.CoalesceAssignmentExpression):
                    await RegisterVariableFix(context, coalesceAssignment.Left, coalesceAssignment);
                    break;
                    
                case PostfixUnaryExpressionSyntax nullForgiving 
                    when nullForgiving.OperatorToken.IsKind(SyntaxKind.ExclamationToken):
                    await RegisterVariableFix(context, nullForgiving.Operand, nullForgiving);
                    break;
            }
        }

        private async Task RegisterVariableFix(
            CodeFixContext context,
            ExpressionSyntax targetExpression,
            ExpressionSyntax operationNode)
        {
            // Find the variable declaration for the target expression
            var document = context.Document;
            var semanticModel = await document.GetSemanticModelAsync(context.CancellationToken);
            
            // Check if the target expression is a local variable
            if (targetExpression is IdentifierNameSyntax identifier &&
                semanticModel.GetSymbolInfo(identifier).Symbol is ILocalSymbol localSymbol)
            {
                // Find the variable declaration and check if it's a nullable reference type
                var declaration = await FindVariableDeclarationAsync(document, localSymbol);
                
                // Register a code fix if the variable is a nullable reference type
                if (declaration != null && IsNullableReferenceType(declaration))
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: GetFixTitle(operationNode),
                            createChangedDocument: c => ApplyCompleteFix(
                                document, 
                                declaration, 
                                operationNode, 
                                c),
                            equivalenceKey: GetEquivalenceKey(operationNode)),
                        context.Diagnostics);
                }
            }
        }

        private string GetFixTitle(SyntaxNode node) => node switch
        {
            // Use specific titles for each operator
            BinaryExpressionSyntax => "Fix null coalescing (??)",
            AssignmentExpressionSyntax => "Fix null coalescing assignment (??=)",
            PostfixUnaryExpressionSyntax => "Fix null forgiving operator (!)",
            _ => "Fix null operator"
        };

        private string GetEquivalenceKey(SyntaxNode node) => node switch
        {
            // Use specific equivalence keys for each operator
            BinaryExpressionSyntax => "FixNullCoalesce",
            AssignmentExpressionSyntax => "FixNullCoalesceAssignment",
            PostfixUnaryExpressionSyntax => "FixNullForgiving",
            _ => "FixNullOperator"
        };

        private async Task<VariableDeclaratorSyntax?> FindVariableDeclarationAsync(Document document, ILocalSymbol symbol)
        {
            // Find the variable declaration for the local symbol
            var root = await document.GetSyntaxRootAsync();
            return root?.DescendantNodes()
                .OfType<VariableDeclaratorSyntax>()
                .FirstOrDefault(v => v.Identifier.Text == symbol.Name);
        }

        private bool IsNullableReferenceType(VariableDeclaratorSyntax declaration)
        {
            // Check if the variable declaration is a nullable reference type
            return declaration.Parent is VariableDeclarationSyntax variableDeclaration &&
                variableDeclaration.Type is NullableTypeSyntax;
        }

        private async Task<Document> ApplyCompleteFix(
            Document document,
            VariableDeclaratorSyntax declaration,
            SyntaxNode operationNode,
            CancellationToken cancellationToken)
        {
            // Apply the complete fix for the null operator
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            if (root == null) return document;
            
            // Create a syntax editor to modify the syntax tree
            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace.Services);

            // 1. Fix variable declaration
            var variableDeclaration = (VariableDeclarationSyntax)declaration.Parent!;
            var nullableType = (NullableTypeSyntax)variableDeclaration.Type;
            
            // Create new type with proper trailing trivia
            var newType = nullableType.ElementType
                .WithTrailingTrivia(nullableType.QuestionToken.TrailingTrivia);
            
            // Create new initializer with proper default value
            var defaultValue = GetDefaultValueExpression(nullableType.ElementType, operationNode);
            var newInitializer = SyntaxFactory.EqualsValueClause(defaultValue)
                .WithEqualsToken(SyntaxFactory.Token(SyntaxKind.EqualsToken))
                .WithLeadingTrivia(SyntaxFactory.Space);

            // Preserve existing identifier trivia
            var newDeclarator = declaration
                .WithInitializer(newInitializer)
                .WithIdentifier(declaration.Identifier.WithTrailingTrivia(SyntaxFactory.Space));
            
            // Replace the variable declaration with the new type and initializer
            var newVariableDeclaration = variableDeclaration
                .WithType(newType)
                .WithVariables(SyntaxFactory.SeparatedList(new[] { newDeclarator }));
            
            // Apply the changes to the syntax tree
            editor.ReplaceNode(variableDeclaration, newVariableDeclaration);

            // 2. Fix operator usage
            var replacement = GetReplacementNode(operationNode);
            if (replacement is IfStatementSyntax ifStatement)
            {
                // Replace the operator with the new if statement
                editor.ReplaceNode(operationNode.Parent!, ifStatement);
            }
            else
            {
                // Replace the operator with the new expression
                editor.ReplaceNode(operationNode, replacement);
            }

            // Apply the changes to the syntax tree
            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        private ExpressionSyntax GetDefaultValueExpression(TypeSyntax type, SyntaxNode operationNode)
        {
            return operationNode switch
            {
                // Use right-hand value from ?? operator
                BinaryExpressionSyntax binary => binary.Right,
                
                // Use string.Empty for ??= operator
                AssignmentExpressionSyntax assignment => SyntaxFactory.ParseExpression("string.Empty"),
                
                // For null forgiving operator, use type-specific default
                PostfixUnaryExpressionSyntax => type switch
                {
                    PredefinedTypeSyntax predefined when predefined.Keyword.IsKind(SyntaxKind.StringKeyword) 
                        => SyntaxFactory.ParseExpression("string.Empty"),
                    _ => SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression)
                },
                
                // Fallback for other cases
                _ => SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression)
            };
        }

        private SyntaxNode GetReplacementNode(SyntaxNode originalNode)
        {
            return originalNode switch
            {
                // For ?? operator: keep left side
                BinaryExpressionSyntax coalesce => coalesce.Left
                    .WithTriviaFrom(coalesce),
                
                // For ??= operator: replace with if statement
                AssignmentExpressionSyntax assignment => SyntaxFactory.IfStatement(
                    condition: SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("string"),
                            SyntaxFactory.IdentifierName("IsNullOrEmpty")),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(assignment.Left)))),
                    statement: SyntaxFactory.Block(
                        SyntaxFactory.ExpressionStatement(
                            SyntaxFactory.AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                assignment.Left,
                                assignment.Right))))
                    .WithLeadingTrivia(originalNode.GetLeadingTrivia())
                    .WithTrailingTrivia(originalNode.GetTrailingTrivia()),
                
                // For ! operator: keep operand
                PostfixUnaryExpressionSyntax postfix => postfix.Operand
                    .WithTriviaFrom(postfix),
                
                _ => originalNode
            };
        }

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;
    }
}