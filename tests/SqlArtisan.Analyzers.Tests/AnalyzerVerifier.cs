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

    public static string EditorConfig(string dbms) => $"""
        root = true

        [*.cs]
        sqlartisan_syntax_{dbms} = any
        """;

    public static string EditorConfig(string dbms, string version) => $"""
        root = true

        [*.cs]
        sqlartisan_syntax_{dbms} = {version}
        """;

    /// <summary>
    /// The deprecated legacy pair, for the SQLA0002 deprecation-suite tests only —
    /// every other test in this project builds its <c>.editorconfig</c> from the
    /// family via <see cref="EditorConfig(string)"/> so it stays green untouched.
    /// </summary>
    public static string LegacyEditorConfig(string dbms) => $"""
        root = true

        [*.cs]
        sqlartisan_target_dbms = {dbms}
        """;

    public static string LegacyEditorConfig(string dbms, string version) => $"""
        root = true

        [*.cs]
        sqlartisan_target_dbms = {dbms}
        sqlartisan_target_version = {version}
        """;

    public static string Unmarked(string source) =>
        source.Replace("{|#0:", string.Empty).Replace("|}", string.Empty);
}
