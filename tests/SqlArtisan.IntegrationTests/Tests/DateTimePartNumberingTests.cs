using System.Text.RegularExpressions;
using SqlArtisan.IntegrationTests.Infrastructure;

namespace SqlArtisan.IntegrationTests.Tests;

/// <summary>
/// Engine-independent gate over the numbering bases <c>DateTimePart</c>'s
/// summaries state: each must sit in <see cref="DateTimePartNumbering.Claims"/>,
/// whose rows a live twin executes. Runs without a database (#523).
/// </summary>
public sealed partial class DateTimePartNumberingTests
{
    // A claim with no catalog entry is a result-semantics sentence with nothing
    // holding it — the layer ADR 0020 measured as the one that always drifted.
    [Fact]
    public void EveryNumberingClaim_SitsInTheCatalog()
    {
        List<string> problems = [];

        foreach ((string member, string summary) in Summaries())
        {
            bool statesBasis = NumberingBasis().IsMatch(summary);
            bool catalogued = DateTimePartNumbering.Claims.TryGetValue(
                member, out DateTimePartNumbering.NumberingClaim? claim);

            if (statesBasis && !catalogued)
            {
                problems.Add(
                    $"DateTimePart.{member}'s summary states a numbering basis that no "
                        + "catalog entry pins — add one with its live rows, or drop the claim.");
            }
            else if (statesBasis && !summary.Contains(claim!.Phrase, StringComparison.Ordinal))
            {
                problems.Add(
                    $"DateTimePart.{member}'s summary no longer carries its catalogued phrase "
                        + $"\"{claim.Phrase}\" — the prose and the live rows have diverged.");
            }
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    // The staleness direction: an entry whose member stopped claiming anything
    // pins nothing, and reads as load-bearing while it sits there.
    [Fact]
    public void EveryCatalogEntry_IsStillClaimedBySomeSummary()
    {
        Dictionary<string, string> summaries = Summaries();

        List<string> inert = [.. DateTimePartNumbering.Claims
            .Where(pair => !summaries.TryGetValue(pair.Key, out string? summary)
                || !summary.Contains(pair.Value.Phrase, StringComparison.Ordinal))
            .Select(pair => pair.Key)
            .OrderBy(member => member, StringComparer.Ordinal)];

        Assert.True(
            inert.Count == 0,
            $"{inert.Count} numbering entr(ies) pin a phrase no summary states any more, so "
                + $"retire them:\n  {string.Join("\n  ", inert)}");
    }

    private static Dictionary<string, string> Summaries()
    {
        string source = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "SqlArtisan", "SqlPart", "FunctionArgument", "DateTimePart.cs"));

        Dictionary<string, string> summaries = new(StringComparer.Ordinal);
        foreach (Match member in SummaryBlock().Matches(source))
        {
            summaries[member.Groups["name"].Value] = Whitespace().Replace(
                member.Groups["summary"].Value.Replace("///", " "), " ");
        }

        Assert.NotEmpty(summaries);
        return summaries;
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

    [GeneratedRegex(
        @"<summary>(?<summary>.*?)</summary>\s*(?<name>[A-Za-z]+) = \d+,", RegexOptions.Singleline)]
    private static partial Regex SummaryBlock();

    // "Sunday = 0", "Monday = 1" — a day named beside the number it carries.
    [GeneratedRegex(@"\b(?:Mon|Tues|Wednes|Thurs|Fri|Satur|Sun)day\s*=\s*\d")]
    private static partial Regex NumberingBasis();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
