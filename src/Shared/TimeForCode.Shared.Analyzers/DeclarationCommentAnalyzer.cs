using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TimeForCode.Shared.Analyzers
{
    /// <summary>
    /// Reports a <c>//</c> comment directly above a type or member declaration: such a comment does not show in IDE
    /// tooltips or generated docs, so it belongs in an XML doc (<c>///</c>). Comments inside bodies, TODO markers and
    /// comments separated from the declaration by a blank line are allowed.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DeclarationCommentAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The id of the diagnostic this analyzer reports.
        /// </summary>
        public const string DiagnosticId = "TFC0001";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Use an XML doc above a declaration",
            "Use an XML doc comment (///) instead of // above this declaration; keep // for comments inside bodies",
            "Style",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxTreeAction(AnalyzeTree);
        }

        private static void AnalyzeTree(SyntaxTreeAnalysisContext context)
        {
            foreach (SyntaxNode node in context.Tree.GetRoot(context.CancellationToken).DescendantNodes())
            {
                if (!IsDeclaration(node))
                {
                    continue;
                }

                SyntaxTrivia? comment = FindLeadingComment(node.GetFirstToken().LeadingTrivia);
                if (comment is not null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, comment.Value.GetLocation()));
                }
            }
        }

        private static bool IsDeclaration(SyntaxNode node) =>
            node is EnumMemberDeclarationSyntax
            || (node is MemberDeclarationSyntax
                && node is not BaseNamespaceDeclarationSyntax
                && node is not GlobalStatementSyntax
                && node is not IncompleteMemberSyntax);

        /// <summary>
        /// Finds the first line of the <c>//</c> comment block that touches the declaration, or null when there is none
        /// or the block is a TODO marker.
        /// </summary>
        private static SyntaxTrivia? FindLeadingComment(SyntaxTriviaList trivia)
        {
            SyntaxTrivia? first = null;
            bool lineHasContent = true;

            for (int i = trivia.Count - 1; i >= 0; i--)
            {
                SyntaxTrivia current = trivia[i];

                if (current.IsKind(SyntaxKind.WhitespaceTrivia))
                {
                    continue;
                }

                if (current.IsKind(SyntaxKind.EndOfLineTrivia))
                {
                    if (!lineHasContent)
                    {
                        break;
                    }

                    lineHasContent = false;
                    continue;
                }

                lineHasContent = true;
                if (current.IsKind(SyntaxKind.SingleLineCommentTrivia) && !IsXmlDoc(current))
                {
                    first = current;
                }
            }

            return first is { } comment && !IsTodo(comment) ? first : null;
        }

        /// <summary>
        /// Tells an XML doc line from a plain comment: without GenerateDocumentationFile the compiler lexes <c>///</c>
        /// lines as plain single-line comments.
        /// </summary>
        private static bool IsXmlDoc(SyntaxTrivia comment)
        {
            string text = comment.ToString();
            return text.StartsWith("///", StringComparison.Ordinal) && !text.StartsWith("////", StringComparison.Ordinal);
        }

        private static bool IsTodo(SyntaxTrivia comment) =>
            comment.ToString().TrimStart('/').TrimStart().StartsWith("TODO", StringComparison.Ordinal);
    }
}