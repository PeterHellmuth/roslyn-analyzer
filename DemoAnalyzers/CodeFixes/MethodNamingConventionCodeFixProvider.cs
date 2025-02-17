using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DemoAnalyzers.Analyzers;

namespace DemoAnalyzers.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MethodNamingConventionCodeFixProvider)), Shared]
    public class MethodNamingConventionCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => [MethodNamingConventionAnalyzer.DiagnosticId];

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var tokenParent = root.FindToken(diagnosticSpan.Start).Parent;
            var methodDeclaration = tokenParent?.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            var invocationExpression = tokenParent?.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();

            if (methodDeclaration != null)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Rename method to PascalCase",
                        createChangedDocument: c => RenameMethodAsync(context.Document, methodDeclaration, c),
                        equivalenceKey: "RenameMethodToPascalCase"),
                    diagnostic);
            }
            else if (invocationExpression != null)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Rename method to PascalCase",
                        createChangedDocument: c => RenameInvocationAsync(context.Document, invocationExpression, c),
                        equivalenceKey: "RenameMethodToPascalCase"),
                    diagnostic);
            }
        }

        private async Task<Document> RenameMethodAsync(Document document, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
        {
            var identifierToken = methodDeclaration.Identifier;
            var newName = char.ToUpper(identifierToken.Text[0]) + identifierToken.Text.Substring(1);
            var newIdentifier = SyntaxFactory.Identifier(newName).WithTriviaFrom(identifierToken);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root?.ReplaceToken(identifierToken, newIdentifier);

            if (newRoot == null)
                return document;

            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> RenameInvocationAsync(Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
        {
            var memberAccess = invocationExpression.Expression as MemberAccessExpressionSyntax;
            if (memberAccess == null)
                return document;

            var identifierToken = memberAccess.Name.Identifier;
            var newName = char.ToUpper(identifierToken.Text[0]) + identifierToken.Text.Substring(1);
            var newIdentifier = SyntaxFactory.IdentifierName(newName).WithTriviaFrom(memberAccess.Name);

            var newMemberAccess = memberAccess.WithName(newIdentifier);
            var newInvocation = invocationExpression.WithExpression(newMemberAccess);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root?.ReplaceNode(invocationExpression, newInvocation);
            if (newRoot == null)
                return document;

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
