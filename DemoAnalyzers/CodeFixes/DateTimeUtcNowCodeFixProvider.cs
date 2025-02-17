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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DateTimeUtcNowCodeFixProvider)), Shared]
    public class DateTimeUtcNowCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => [DateTimeUtcNowAnalyzer.DiagnosticId];

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            // Get the syntax root of the document
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            // Get the first diagnostic from the context
            var diagnostic = context.Diagnostics.First();
            // Get the span of the diagnostic location
            // This is the location of the DateTime.Now usage in the code
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the token at the start of the diagnostic span and get its parent syntax node
            var tokenParent = root.FindToken(diagnosticSpan.Start).Parent;
            if (tokenParent == null)
                return;

            // Find the first MemberAccessExpressionSyntax ancestor of the parent node
            // This is the DateTime.Now usage that needs to be replaced
            var memberAccess = tokenParent.AncestorsAndSelf().OfType<MemberAccessExpressionSyntax>().First();

            // Register a code fix action to replace DateTime.Now with DateTime.UtcNow
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Replace with DateTime.UtcNow",
                    createChangedDocument: c => ReplaceWithUtcNowAsync(context.Document, memberAccess, c),
                    equivalenceKey: "ReplaceWithUtcNow"),
                diagnostic);
        }

        private async Task<Document> ReplaceWithUtcNowAsync(Document document, MemberAccessExpressionSyntax memberAccess, CancellationToken cancellationToken)
        {
            // Get the syntax root of the document
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Create a new member access expression for DateTime.UtcNow
            var utcNowMemberAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("DateTime"),
                SyntaxFactory.IdentifierName("UtcNow"));

            // Replace the old member access expression with the new one
            var newRoot = root?.ReplaceNode(memberAccess, utcNowMemberAccess);
            if (newRoot == null)
                return document;

            // Return the updated document with the new syntax root
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
