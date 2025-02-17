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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(StructuredLoggingCodeFixProvider)), Shared]
    public class StructuredLoggingCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => [StructuredLoggingAnalyzer.DiagnosticId];

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
            // This is the location of the string interpolation in the code
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the token at the start of the diagnostic span
            // and get its parent syntax node
            var parent = root.FindToken(diagnosticSpan.Start).Parent;
            if (parent == null)
                return;

            // Find the first InterpolatedStringExpressionSyntax ancestor of the parent node
            // This is the string interpolation that needs to be converted
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
            // Get the syntax root of the document
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            // Find the first InvocationExpressionSyntax ancestor of the interpolated string
            // This is the method call that contains the string interpolation
            var invocation = interpolatedString.Ancestors().OfType<InvocationExpressionSyntax>().First();
            // Get the original arguments of the invocation
            var originalArguments = invocation.ArgumentList.Arguments;

            // Extract placeholders from the interpolated string
            var structuredTemplate = string.Join("", interpolatedString.Contents.Select(content =>
            {
                // Use the expression itself as the placeholder
                return content is InterpolationSyntax interp ? $"{{{((IdentifierNameSyntax)interp.Expression).Identifier.Text}}}" : content.ToString();
            }));

            // Create a new argument list with the structured logging template and the original parameters
            var newArguments = originalArguments.Replace(originalArguments[0], SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(structuredTemplate))));
            // Add the original parameters as arguments to the new argument list
            // The first argument is the template, so we start from the second argument
            newArguments = newArguments.AddRange(interpolatedString.Contents.OfType<InterpolationSyntax>().Select(interp => SyntaxFactory.Argument(interp.Expression)));

            // Create a new invocation expression with the updated argument list
            var newInvocation = invocation.WithArgumentList(SyntaxFactory.ArgumentList(newArguments));
            if (root != null)
            {
                // Replace the old invocation with the new one in the syntax tree
                // and return the updated document
                var newRoot = root.ReplaceNode(invocation, newInvocation);
                return document.WithSyntaxRoot(newRoot);
            }
            return document;
        }
    }
}