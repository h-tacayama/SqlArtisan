using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace SqlArtisan.TableClassGen.Tests;

// Compiles generated table-class source against the real SqlArtisan assembly.
// The generator is engine-agnostic, so proving its output compiles once covers
// every engine's generated classes.
internal static class GeneratedCodeCompiler
{
    private static readonly IReadOnlyList<MetadataReference> s_references = BuildReferences();

    // Each generated table class is a complete file (own namespace), so the
    // sources compile as separate syntax trees, not one concatenated unit.
    public static void AssertCompiles(IEnumerable<string> sources)
    {
        CSharpCompilation compilation = CSharpCompilation.Create(
            "GeneratedTables",
            sources.Select(s => CSharpSyntaxTree.ParseText(s)),
            s_references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using MemoryStream stream = new();
        EmitResult result = compilation.Emit(stream);

        string errors = string.Join(
            Environment.NewLine,
            result.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString()));

        Assert.True(result.Success, $"Generated code failed to compile:{Environment.NewLine}{errors}");
    }

    private static IReadOnlyList<MetadataReference> BuildReferences()
    {
        string trusted = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;

        HashSet<string> paths = trusted
            .Split(Path.PathSeparator)
            .Where(p => p.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // The runtime's trusted set omits project references not yet JIT-loaded, so
        // add SqlArtisan (DbTableBase / DbColumn) explicitly.
        paths.Add(typeof(DbTableBase).Assembly.Location);

        return [.. paths.Select(p => MetadataReference.CreateFromFile(p))];
    }
}
