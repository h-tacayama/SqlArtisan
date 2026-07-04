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
    [Fact]
    public void VerifiedAgainstVersions_AppearInAnalyzerDoc()
    {
        string doc = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs", "analyzer.md"));

        List<string> missing = [];
        foreach (KeyValuePair<TargetDbms, string> pair in DialectMatrix.VerifiedAgainstVersion)
        {
            // The display part before the parenthetical (container image details
            // may legitimately be elided or reworded in the doc's table).
            string displayPart = pair.Value.Split(" (")[0];
            MatchCollection versionTokens = Regex.Matches(displayPart, @"\d[\w.:\-]*");

            if (versionTokens.Count == 0)
            {
                if (!doc.Contains(displayPart))
                {
                    missing.Add($"{pair.Key}: \"{displayPart}\"");
                }

                continue;
            }

            foreach (Match token in versionTokens)
            {
                if (!doc.Contains(token.Value))
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
