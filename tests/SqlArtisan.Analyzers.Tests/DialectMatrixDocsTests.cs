using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>
/// Guards docs/analyzer.md's verified-against table against drifting from
/// <see cref="DialectMatrix.VerifiedAgainstVersion"/> — the Oracle row did
/// exactly that once (the dictionary said <c>gvenzl/oracle-free</c> while the
/// actual CI container was the XE 21c image). Every version-shaped token in a
/// dictionary value's display part must appear somewhere in the doc; a display
/// part with no such token must appear verbatim.
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

        // Scope to the verified-against section's own lines: the version tokens
        // co-occur elsewhere in the page (the enabling examples, register rows),
        // so a whole-page match passes vacuously when the table itself drifts.
        int start = System.Array.FindIndex(
            allLines, line => line.StartsWith("## Verified-against versions", StringComparison.Ordinal));
        Assert.True(start >= 0, "docs/analyzer.md no longer has a '## Verified-against versions' section.");
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
            $"docs/analyzer.md's verified-against table has drifted from DialectMatrix.VerifiedAgainstVersion — "
                + $"{missing.Count} missing tokens:\n  " + string.Join("\n  ", missing));
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
