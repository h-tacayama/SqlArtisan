using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>
/// Ties <see cref="DialectMatrix.VerifiedAgainstVersion"/> to both ends: docs/analyzer.md's
/// verified-against table, and the engine each integration lane pins. The Oracle row
/// has drifted from each: the doc once, the fixture until #681.
/// </summary>
public class DialectMatrixDocsTests
{
    // Scoped like DialectMatrixVersionBoundsTests' provenance gate: a token must
    // sit, digit-bounded, on a line that also names the dialect — a whole-file
    // substring check let common tokens survive a re-versioned row.
    [Fact]
    public void VerifiedAgainstVersions_AppearInAnalyzerDoc()
    {
        string[] allLines = File.ReadAllLines(Path.Combine(FindRepoRoot(), "docs", "analyzer.md"));

        // Scoped to the section's own lines: the version tokens recur elsewhere
        // in the page, so a whole-page match passes vacuously when it drifts.
        int start = System.Array.FindIndex(
            allLines, line => line.StartsWith(
                "## Verified-against versions",
                StringComparison.Ordinal));
        Assert.True(
            start >= 0,
            "docs/analyzer.md no longer has a '## Verified-against versions' section.");
        int end = System.Array.FindIndex(
            allLines, start + 1, line => line.StartsWith("## ", StringComparison.Ordinal));
        string[] docLines = allLines[start..(end < 0 ? allLines.Length : end)];

        List<string> missing = [];
        foreach (KeyValuePair<TargetDbms, string> pair in DialectMatrix.VerifiedAgainstVersion)
        {
            string display = TargetDbmsNames.Display(pair.Key);
            string[] nameLines = [.. docLines.Where(line => line.Contains(display))];

            // The display part before the parenthetical (container image details
            // may legitimately be elided or reworded in the doc's table).
            string displayPart = pair.Value.Split(" (")[0];
            MatchCollection versionTokens = Regex.Matches(displayPart, @"\d[\w.:\-]*");

            if (versionTokens.Count == 0)
            {
                if (!nameLines.Any(line => line.Contains(displayPart)))
                {
                    missing.Add($"{pair.Key}: \"{displayPart}\"");
                }

                continue;
            }

            foreach (Match token in versionTokens)
            {
                Regex bounded = new($@"(?<![\d.]){Regex.Escape(token.Value)}(?![\d.])");
                if (!nameLines.Any(line => bounded.IsMatch(line)))
                {
                    missing.Add($"{pair.Key}: \"{token.Value}\" (from \"{pair.Value}\")");
                }
            }
        }

        Assert.True(
            missing.Count == 0,
            $"docs/analyzer.md's verified-against table has drifted from "
                + $"DialectMatrix.VerifiedAgainstVersion — "
                + $"{missing.Count} missing tokens:\n  " + string.Join("\n  ", missing));
    }

    // The other end of the chain: a fixture's pinned image, which the dictionary
    // once named as the module default it no longer tracks (#681).
    [Theory]
    [InlineData("MySql")]
    [InlineData("Oracle")]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public void VerifiedAgainstVersion_NamesTheFixtureImage(string dbms)
    {
        string source = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "tests",
            "SqlArtisan.IntegrationTests",
            "Infrastructure",
            $"{dbms}Fixture.cs"));
        string image = Regex.Match(source, @"\.WithImage\(""([^""]+)""\)").Groups[1].Value;

        Assert.NotEmpty(image);
        Assert.Contains(
            $"`{image}`", DialectMatrix.VerifiedAgainstVersion[Enum.Parse<TargetDbms>(dbms)]);
    }

    // SQLite runs in-process, so its pin is the integration project's two package versions.
    [Theory]
    [InlineData("Microsoft.Data.Sqlite")]
    [InlineData("SQLitePCLRaw.bundle_e_sqlite3")]
    public void VerifiedAgainstVersion_NamesTheSqlitePackages(string package)
    {
        string project = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "tests",
            "SqlArtisan.IntegrationTests",
            "SqlArtisan.IntegrationTests.csproj"));
        string version = Regex.Match(
            project,
            $@"<PackageReference Include=""{Regex.Escape(package)}"" Version=""([^""]+)""")
            .Groups[1].Value;

        Assert.NotEmpty(version);
        Assert.Contains(
            $"{package} {version}", DialectMatrix.VerifiedAgainstVersion[TargetDbms.Sqlite]);
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
