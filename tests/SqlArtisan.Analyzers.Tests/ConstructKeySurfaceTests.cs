using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SqlArtisan.Analyzers.Tests;

// Users write these keys into .editorconfig, and a key the derivation stops producing turns
// into a SQLA0001 there, so a change to the set is a deliberate baseline edit (#655).
public class ConstructKeySurfaceTests
{
    [Fact]
    public void DerivedKeys_MatchTheBaselineExactly()
    {
        string path = Path.Combine(
            FindRepoRoot(),
            "tests",
            "SqlArtisan.Analyzers.Tests",
            "Baselines",
            "construct-keys.txt");
        List<string> baseline = [.. File.ReadAllLines(path)
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .OrderBy(key => key, StringComparer.Ordinal)];

        List<string> derived = [.. ConstructKeySurface.Collect(SqlArtisanAssembly())
            .OrderBy(key => key, StringComparer.Ordinal)];

        Assert.True(
            baseline.SequenceEqual(derived),
            "The sqlartisan_construct_* keys drifted from construct-keys.txt — a key users "
                + "wrote now reports SQLA0001; call it out in the CHANGELOG and update the "
                + "file:\n  "
                + string.Join("\n  ", derived.Except(baseline).Select(k => "+ " + k)
                    .Concat(baseline.Except(derived).Select(k => "- " + k))));
    }

    // With the runtime referenced, as in a real build: without it System.Enum does not
    // resolve, enum types stop reading as enums, and their members slip into the set.
    private static IAssemblySymbol SqlArtisanAssembly()
    {
        MetadataReference reference =
            MetadataReference.CreateFromFile(typeof(Sql).Assembly.Location);
        IEnumerable<MetadataReference> runtime =
            ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator)
                .Where(path => Path.GetFileName(path) != "SqlArtisan.dll")
                .Select(path => MetadataReference.CreateFromFile(path));
        CSharpCompilation compilation = CSharpCompilation.Create(
            "KeySurfaceProbe",
            references: [reference, .. runtime]);
        return (IAssemblySymbol)compilation.GetAssemblyOrModuleSymbol(reference)!;
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);

        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SqlArtisan.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }
}
