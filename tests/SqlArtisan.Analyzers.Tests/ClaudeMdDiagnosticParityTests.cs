using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>
/// Ties CLAUDE.md's analyzer diagnostic list — the map every session reads
/// first — to <see cref="DialectUsageAnalyzer.SupportedDiagnostics"/>, the
/// real source of truth.
/// </summary>
/// <remarks>
/// A new diagnostic ships correctly (it's in <c>SupportedDiagnostics</c>, its
/// tests pass) whether or not CLAUDE.md's hand-written bullet list and spelled-out
/// count are updated to match — nothing downstream of the shipped analyzer reads
/// this file, so drift here is invisible everywhere except to the next
/// contributor or AI session that trusts it.
/// </remarks>
public class ClaudeMdDiagnosticParityTests
{
    private static readonly Regex BulletIdPattern = new(@"^- \*\*(SQLA\d{4})\*\*", RegexOptions.Multiline);
    private static readonly Regex ShipsCountPattern = new(@"ships (\w+) diagnostics");

    private static readonly IReadOnlyDictionary<string, int> NumberWords = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["one"] = 1,
        ["two"] = 2,
        ["three"] = 3,
        ["four"] = 4,
        ["five"] = 5,
        ["six"] = 6,
        ["seven"] = 7,
        ["eight"] = 8,
        ["nine"] = 9,
        ["ten"] = 10,
        ["eleven"] = 11,
        ["twelve"] = 12,
        ["thirteen"] = 13,
        ["fourteen"] = 14,
        ["fifteen"] = 15,
        ["sixteen"] = 16,
        ["seventeen"] = 17,
        ["eighteen"] = 18,
        ["nineteen"] = 19,
        ["twenty"] = 20,
    };

    private static readonly string AnalyzerSection = ExtractAnalyzerSection();

    private static readonly IReadOnlyList<string> DocumentedIds =
        [.. BulletIdPattern.Matches(AnalyzerSection).Select(m => m.Groups[1].Value)];

    private static readonly IReadOnlyList<string> ShippedIds =
        [.. new DialectUsageAnalyzer().SupportedDiagnostics.Select(d => d.Id).Distinct(StringComparer.Ordinal)];

    [Fact]
    public void EveryShippedId_HasABulletInClaudeMd()
    {
        string[] missing = [.. ShippedIds.Except(DocumentedIds, StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal)];

        Assert.True(
            missing.Length == 0,
            $"{missing.Length} shipped diagnostic id(s) have no '## Analyzer' bullet in CLAUDE.md:\n  "
                + string.Join("\n  ", missing));
    }

    [Fact]
    public void EveryBulletedId_IsAShippedDiagnostic()
    {
        string[] phantom = [.. DocumentedIds.Except(ShippedIds, StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal)];

        Assert.True(
            phantom.Length == 0,
            $"{phantom.Length} id(s) bulleted in CLAUDE.md's '## Analyzer' section name no diagnostic "
                + $"DialectUsageAnalyzer actually reports:\n  {string.Join("\n  ", phantom)}");
    }

    [Fact]
    public void ShipsCount_MatchesTheDistinctIdCount()
    {
        Match match = ShipsCountPattern.Match(AnalyzerSection);
        Assert.True(match.Success, "CLAUDE.md's '## Analyzer' section has no 'ships <N> diagnostics' sentence to check.");

        Assert.True(
            NumberWords.TryGetValue(match.Groups[1].Value, out int claimed),
            $"'{match.Groups[1].Value}' in CLAUDE.md's 'ships {match.Groups[1].Value} diagnostics' is not a "
                + "recognized spelled-out number — extend NumberWords or reword the sentence.");

        Assert.True(
            claimed == ShippedIds.Count,
            $"CLAUDE.md claims '{match.Groups[1].Value}' ({claimed}) diagnostics; "
                + $"DialectUsageAnalyzer.SupportedDiagnostics carries {ShippedIds.Count} distinct ids.");
    }

    private static string ExtractAnalyzerSection()
    {
        string claudeMd = File.ReadAllText(Path.Combine(FindRepoRoot(), "CLAUDE.md"));
        int start = claudeMd.IndexOf("\n## Analyzer\n", StringComparison.Ordinal);
        Assert.True(start >= 0, "CLAUDE.md has no '## Analyzer' section.");

        int next = claudeMd.IndexOf("\n## ", start + 1, StringComparison.Ordinal);
        return next < 0 ? claudeMd.Substring(start) : claudeMd.Substring(start, next - start);
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
