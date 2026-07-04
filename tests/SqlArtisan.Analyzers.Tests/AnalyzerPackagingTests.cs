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
}
