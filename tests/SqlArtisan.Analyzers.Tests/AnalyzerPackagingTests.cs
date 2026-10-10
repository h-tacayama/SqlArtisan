using System.Reflection;

namespace SqlArtisan.Analyzers.Tests;

public class AnalyzerPackagingTests
{
    [Fact]
    public void AnalyzerAssembly_ProjectReferenceOutput_IsLoadable()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "SqlArtisan.Analyzers.dll");
        Assert.True(File.Exists(path), $"Expected {path} to be copied to the test output.");

        Assembly assembly = Assembly.LoadFrom(path);
        Assert.Equal("SqlArtisan.Analyzers", assembly.GetName().Name);
    }

    // A compiler older than the referenced Roslyn skips the analyzer, so a bump moves the
    // published minimum; the test project pins the same version and would pass it silently.
    [Fact]
    public void AnalyzerAssembly_ReferencedRoslyn_IsTheMinimumVersioningDocStates()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "SqlArtisan.Analyzers.dll");
        Version roslyn = Assembly.LoadFrom(path)
            .GetReferencedAssemblies()
            .Single(a => a.Name == "Microsoft.CodeAnalysis")
            .Version!;

        string versioning = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs", "versioning.md"));

        Assert.Contains($"built against Roslyn {roslyn.Major}.{roslyn.Minor},", versioning);
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
