using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>
/// Builds a <see cref="DialectUsageAnalyzer"/> test with the SqlArtisan core
/// assembly available to the source under test, and an optional
/// <c>.editorconfig</c> content string applied to it.
/// </summary>
internal static class AnalyzerVerifier
{
    private static readonly ReferenceAssemblies Net80 = new(
        "net8.0",
        new PackageIdentity("Microsoft.NETCore.App.Ref", "8.0.0"),
        Path.Combine("ref", "net8.0"));

    public static CSharpAnalyzerTest<DialectUsageAnalyzer, DefaultVerifier> Create(string source, string? editorConfig = null)
    {
        var test = new CSharpAnalyzerTest<DialectUsageAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = Net80,
        };

        test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(Sql).Assembly.Location));

        if (editorConfig is not null)
        {
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));
        }

        return test;
    }
}
