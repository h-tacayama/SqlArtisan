using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SqlArtisan.Analyzer.Tests;

// Runs TargetDbmsFunctionAnalyzer over an in-memory compilation, supplying the
// target DBMS through a fake AnalyzerConfigOptions (the same channel MSBuild's
// <SqlArtisanTargetDbms> would use). Returns only analyzer diagnostics.
internal static class AnalyzerHarness
{
    public static async Task<ImmutableArray<Diagnostic>> RunAsync(string source, string? targetDbms)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source);

        IEnumerable<MetadataReference> references =
            ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(path => path.Length > 0)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path));

        CSharpCompilation compilation = CSharpCompilation.Create(
            "AnalyzerTestAssembly",
            new[] { tree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        ImmutableDictionary<string, string> global = ImmutableDictionary<string, string>.Empty;
        if (targetDbms is not null)
        {
            global = global.Add("build_property.SqlArtisanTargetDbms", targetDbms);
        }

        AnalyzerOptions options = new(
            ImmutableArray<AdditionalText>.Empty,
            new OptionsProvider(global));

        CompilationWithAnalyzers withAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new TargetDbmsFunctionAnalyzer()),
            options);

        return await withAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    private sealed class OptionsProvider(ImmutableDictionary<string, string> values) : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerConfigOptions _options = new Options(values);

        public override AnalyzerConfigOptions GlobalOptions => _options;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _options;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _options;
    }

    private sealed class Options(ImmutableDictionary<string, string> values) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string? value)
        {
            if (values.TryGetValue(key, out string? found))
            {
                value = found;
                return true;
            }

            value = null;
            return false;
        }
    }
}
