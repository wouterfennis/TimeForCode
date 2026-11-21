using System.Collections.Immutable;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using TimeForCode.Shared.Analyzers;

namespace TimeForCode.Shared.Tests.Analyzers
{
    [TestClass]
    public class DeclarationCommentAnalyzerTests
    {
        [TestMethod]
        public async Task Analyze_LineCommentAboveMethod_ReportsDiagnostic()
        {
            const string source = """
                public class Sample
                {
                    // Explains the method.
                    public void Run() { }
                }
                """;

            ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source);

            diagnostics.Select(d => d.Id).Should().Equal(DeclarationCommentAnalyzer.DiagnosticId);
        }

        [TestMethod]
        public async Task Analyze_LineCommentAboveType_ReportsDiagnostic()
        {
            const string source = """
                // Explains the type.
                public class Sample { }
                """;

            ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source);

            diagnostics.Should().ContainSingle();
        }

        [TestMethod]
        public async Task Analyze_LineCommentAboveField_ReportsDiagnostic()
        {
            const string source = """
                public class Sample
                {
                    // Explains the field.
                    private readonly int _value = 1;
                }
                """;

            ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source);

            diagnostics.Should().ContainSingle();
        }

        [TestMethod]
        public async Task Analyze_LineCommentAboveEnumMember_ReportsDiagnostic()
        {
            const string source = """
                public enum Sample
                {
                    // Explains the member.
                    First,
                }
                """;

            ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source);

            diagnostics.Should().ContainSingle();
        }

        [TestMethod]
        public async Task Analyze_LineCommentAboveAttributedMember_ReportsDiagnostic()
        {
            const string source = """
                public class Sample
                {
                    // Explains the method.
                    [System.Obsolete]
                    public void Run() { }
                }
                """;

            ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source);

            diagnostics.Should().ContainSingle();
        }

        [TestMethod]
        public async Task Analyze_CommentOverSeveralLines_ReportsOncePerDeclaration()
        {
            const string source = """
                public class Sample
                {
                    // Explains the method
                    // over two lines.
                    public void Run() { }
                }
                """;

            ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source);

            diagnostics.Should().ContainSingle();
        }

        [TestMethod]
        public async Task Analyze_LineCommentAboveMethod_ReportsAtTheFirstCommentLine()
        {
            const string source = """
                public class Sample
                {
                    // Explains the method
                    // over two lines.
                    public void Run() { }
                }
                """;

            ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source);

            diagnostics.Single().Location.GetLineSpan().StartLinePosition.Line.Should().Be(2);
        }

        [TestMethod]
        public async Task Analyze_XmlDocAboveMethod_ReportsNothing()
        {
            const string source = """
                public class Sample
                {
                    /// <summary>Explains the method.</summary>
                    public void Run() { }
                }
                """;

            ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source);

            diagnostics.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Analyze_XmlDocOverSeveralLinesAboveMethod_ReportsNothing()
        {
            const string source = """
                public class Sample
                {
                    /// <summary>
                    /// Explains the method.
                    /// </summary>
                    public void Run() { }
                }
                """;

            ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source);

            diagnostics.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Analyze_FourSlashCommentAboveMethod_ReportsDiagnostic()
        {
            const string source = """
                public class Sample
                {
                    //// Commented-out text.
                    public void Run() { }
                }
                """;

            ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source);

            diagnostics.Should().ContainSingle();
        }

        [TestMethod]
        public async Task Analyze_LineCommentInsideMethodBody_ReportsNothing()
        {
            const string source = """
                public class Sample
                {
                    public void Run()
                    {
                        // Explains the statement.
                        int value = 1;
                    }
                }
                """;

            ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source);

            diagnostics.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Analyze_TodoCommentAboveMethod_ReportsNothing()
        {
            const string source = """
                public class Sample
                {
                    // TODO(review): decide the name.
                    public void Run() { }
                }
                """;

            ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source);

            diagnostics.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Analyze_LineCommentSeparatedByBlankLine_ReportsNothing()
        {
            const string source = """
                public class Sample
                {
                    // A section heading.

                    public void Run() { }
                }
                """;

            ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source);

            diagnostics.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Analyze_LineCommentAboveAssemblyAttribute_ReportsNothing()
        {
            const string source = """
                // Why the assembly is configured this way.
                [assembly: System.CLSCompliant(false)]
                """;

            ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source);

            diagnostics.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Analyze_GeneratedCode_ReportsNothing()
        {
            const string source = """
                public class Sample
                {
                    // Explains the method.
                    public void Run() { }
                }
                """;

            ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source, "Sample.g.cs");

            diagnostics.Should().BeEmpty();
        }

        private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source, string path = "Sample.cs")
        {
            // The compiler does not parse documentation comments unless GenerateDocumentationFile is set, so they are plain trivia.
            CSharpParseOptions options = CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.None);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source, options, path);
            string trustedAssemblies = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
            IEnumerable<MetadataReference> references = trustedAssemblies
                .Split(Path.PathSeparator)
                .Select(assembly => MetadataReference.CreateFromFile(assembly));
            CSharpCompilation compilation = CSharpCompilation.Create(
                "SampleAssembly",
                [tree],
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            CompilationWithAnalyzers withAnalyzers = compilation.WithAnalyzers([new DeclarationCommentAnalyzer()]);

            return await withAnalyzers.GetAnalyzerDiagnosticsAsync();
        }
    }
}