using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SqlArtisan.Analyzer.Tests;

// Runs TargetDbmsFunctionAnalyzer over an in-memory compilation. The MSBuild
// target goes through GlobalOptions (build_property.*) and the .editorconfig
// target through per-file options (sqlartisan_target_dbms) — the same two channels
// real projects use — so precedence can be tested.
internal static class AnalyzerHarness
{
    public static Task<ImmutableArray<Diagnostic>> RunAsync(
        string source,
        string? msbuildTarget = null,
        string? editorConfigTarget = null)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: "File0.cs");

        ImmutableDictionary<string, string> global = ImmutableDictionary<string, string>.Empty;
        if (msbuildTarget is not null)
        {
            global = global.Add("build_property.SqlArtisanTargetDbms", msbuildTarget);
        }

        ImmutableDictionary<string, string> perFile = ImmutableDictionary<string, string>.Empty;
        if (editorConfigTarget is not null)
        {
            perFile = perFile.Add("sqlartisan_target_dbms", editorConfigTarget);
        }

        Dictionary<SyntaxTree, AnalyzerConfigOptions> byTree = new() { [tree] = new Options(perFile) };
        return RunAsync(new[] { tree }, byTree, new Options(global));
    }

    // Several files in ONE compilation, each with its own per-file .editorconfig
    // target — proves per-folder scoping within a single project.
    public static Task<ImmutableArray<Diagnostic>> RunPerFileAsync(
        string sharedStub,
        params (string source, string? editorConfigTarget)[] files)
    {
        List<SyntaxTree> trees = new();
        Dictionary<SyntaxTree, AnalyzerConfigOptions> byTree = new();

        SyntaxTree stub = CSharpSyntaxTree.ParseText(sharedStub, path: "Stub.cs");
        trees.Add(stub);
        byTree[stub] = new Options(ImmutableDictionary<string, string>.Empty);

        for (int i = 0; i < files.Length; i++)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(files[i].source, path: $"File{i}.cs");
            trees.Add(tree);

            ImmutableDictionary<string, string> values = ImmutableDictionary<string, string>.Empty;
            if (files[i].editorConfigTarget is not null)
            {
                values = values.Add("sqlartisan_target_dbms", files[i].editorConfigTarget!);
            }

            byTree[tree] = new Options(values);
        }

        return RunAsync(trees, byTree, new Options(ImmutableDictionary<string, string>.Empty));
    }

    private static async Task<ImmutableArray<Diagnostic>> RunAsync(
        IEnumerable<SyntaxTree> trees,
        Dictionary<SyntaxTree, AnalyzerConfigOptions> byTree,
        AnalyzerConfigOptions global)
    {
        IEnumerable<MetadataReference> references =
            ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(path => path.Length > 0)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path));

        CSharpCompilation compilation = CSharpCompilation.Create(
            "AnalyzerTestAssembly",
            trees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        AnalyzerOptions options = new(
            ImmutableArray<AdditionalText>.Empty,
            new PerTreeOptionsProvider(byTree, global));

        CompilationWithAnalyzers withAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new TargetDbmsFunctionAnalyzer()),
            options);

        return await withAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    private sealed class PerTreeOptionsProvider(
        Dictionary<SyntaxTree, AnalyzerConfigOptions> byTree,
        AnalyzerConfigOptions global) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions => global;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) =>
            byTree.TryGetValue(tree, out AnalyzerConfigOptions? o) ? o : global;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => global;
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
