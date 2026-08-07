using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>
/// Ties the <c>CompilerVisibleProperty</c> entries the shipped
/// <c>build/SqlArtisan.props</c> declares to the <c>build_property.*</c> keys
/// <see cref="AnalyzerConfigResolver"/> actually reads.
/// </summary>
/// <remarks>
/// The two lists are hand-written independently — the props file in XML, the
/// resolver in C# string interpolation over <see cref="TargetDbms"/>'s
/// spelling — so a rename on either side (a DBMS enum member, a declared
/// property) silently orphans the other: the property never reaches the
/// resolver, or the resolver reads a key MSBuild never emits. Both stay
/// silent at the failing call site (a missing key just reads as unset), so
/// only this gate catches the drift.
/// </remarks>
public class MSBuildPropertyParityTests
{
    private static readonly IReadOnlyList<string> DeclaredProperties =
        [.. XDocument.Load(Path.Combine(FindRepoRoot(), "src", "SqlArtisan.Analyzers", "build", "SqlArtisan.props"))
            .Descendants("CompilerVisibleProperty")
            .Select(e => e.Attribute("Include")!.Value)];

    private static readonly IReadOnlyList<string> ResolverExpectedProperties =
        [
            StripPrefix(AnalyzerConfigResolver.TargetDbmsMSBuildPropertyKey),
            StripPrefix(AnalyzerConfigResolver.TargetVersionMSBuildPropertyKey),
            .. AnalyzerConfigResolver.AllDbms.Select(dbms => StripPrefix(AnalyzerConfigResolver.SyntaxMSBuildPropertyKey(dbms))),
        ];

    private static string StripPrefix(string buildPropertyKey) =>
        buildPropertyKey.Substring("build_property.".Length);

    [Fact]
    public void EveryDeclaredProperty_IsReadByTheResolver()
    {
        string[] unread = [.. DeclaredProperties.Except(ResolverExpectedProperties, StringComparer.Ordinal)];

        Assert.True(
            unread.Length == 0,
            $"{unread.Length} propert(y|ies) declared in build/SqlArtisan.props have no matching "
                + $"AnalyzerConfigResolver key, so a consumer setting them has no effect:\n  "
                + string.Join("\n  ", unread));
    }

    [Fact]
    public void EveryResolverProperty_IsDeclaredInTheProps()
    {
        string[] undeclared = [.. ResolverExpectedProperties.Except(DeclaredProperties, StringComparer.Ordinal)];

        Assert.True(
            undeclared.Length == 0,
            $"{undeclared.Length} key(s) AnalyzerConfigResolver reads have no matching "
                + "CompilerVisibleProperty in build/SqlArtisan.props, so MSBuild never makes them "
                + $"visible to the analyzer:\n  {string.Join("\n  ", undeclared)}");
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
