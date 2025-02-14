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

namespace DemoAnalyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DateTimeUtcNowCodeFixProvider)), Shared]
    public class DateTimeUtcNowCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DateTimeUtcNowAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var tokenParent = root.FindToken(diagnosticSpan.Start).Parent;
            if (tokenParent == null)
                return;

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

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
