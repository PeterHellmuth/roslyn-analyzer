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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(StructuredLoggingCodeFixProvider)), Shared]
    public class StructuredLoggingCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(StructuredLoggingAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var parent = root.FindToken(diagnosticSpan.Start).Parent;
            if (parent == null)
                return;

            var interpolatedString = parent.AncestorsAndSelf().OfType<InterpolatedStringExpressionSyntax>().First();

            // Register a code fix action to convert the string interpolation to structured logging
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Convert to structured logging",
                    createChangedDocument: c => ConvertToStructuredLoggingAsync(context.Document, interpolatedString, c),
                    equivalenceKey: "ConvertToStructuredLogging"),
                diagnostic);
        }

        private async Task<Document> ConvertToStructuredLoggingAsync(Document document, InterpolatedStringExpressionSyntax interpolatedString, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var invocation = interpolatedString.Ancestors().OfType<InvocationExpressionSyntax>().First();
            var originalArguments = invocation.ArgumentList.Arguments;

            // Extract placeholders from the interpolated string
            var structuredTemplate = string.Join("", interpolatedString.Contents.Select(content =>
            {
                // Use the expression itself as the placeholder
                return content is InterpolationSyntax interp ? $"{{{((IdentifierNameSyntax)interp.Expression).Identifier.Text}}}" : content.ToString();
            }));

            // Create a new argument list with the structured logging template and the original parameters
            var newArguments = originalArguments.Replace(originalArguments[0], SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(structuredTemplate))));
            newArguments = newArguments.AddRange(interpolatedString.Contents.OfType<InterpolationSyntax>().Select(interp => SyntaxFactory.Argument(interp.Expression)));

            var newInvocation = invocation.WithArgumentList(SyntaxFactory.ArgumentList(newArguments));
            if (root != null)
            {
                var newRoot = root.ReplaceNode(invocation, newInvocation);
                return document.WithSyntaxRoot(newRoot);
            }
            return document;
        }
    }
}