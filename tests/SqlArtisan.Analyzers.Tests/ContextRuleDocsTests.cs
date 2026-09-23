using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>
/// Guards every context-rule count in docs/analyzer.md against drifting from
/// <see cref="ContextRules"/>. Two branches wrote one of them over different
/// rule totals once, and the merge kept the stale wording silently (#521).
/// </summary>
public class ContextRuleDocsTests
{
    private static readonly string[] s_numberWords =
    [
        "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
        "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen",
        "nineteen", "twenty",
    ];

    // Each count sentence must occur exactly once, so a second copy of one fails
    // here rather than drifting unwatched like the two this gate was missing.
    [Fact]
    public void EveryRuleCount_MatchesContextRules()
    {
        int walking = typeof(ContextRules)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Count(method => method.Name.StartsWith("Check", StringComparison.Ordinal));

        // None is the "no DML shape here" answer, not a rule.
        int dmlShapes = Enum.GetValues<ContextRules.DmlShape>().Length - 1;
        string page = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs", "analyzer.md"));

        (string Pattern, int Expected)[] counts =
        [
            (@"(\w+) rules ship today", walking + dmlShapes),
            (@"rules ship today — (\w+) reading", walking),
            (@"surroundings, (\w+) reading the DML statement", dmlShapes),
            (@"alone among the (\w+) —", walking + dmlShapes),
            (@"For the (\w+) that read the construct's surroundings", walking),
        ];

        List<string> wrong = [];
        foreach ((string pattern, int expected) in counts)
        {
            MatchCollection sites = Regex.Matches(page, pattern, RegexOptions.IgnoreCase);
            if (sites.Count != 1)
            {
                wrong.Add($"\"{pattern}\" matched {sites.Count} sites, expected exactly 1");
                continue;
            }

            string actual = sites[0].Groups[1].Value;
            if (!string.Equals(actual, Word(expected), StringComparison.OrdinalIgnoreCase))
            {
                wrong.Add($"\"{pattern}\" says \"{actual}\", code says \"{Word(expected)}\"");
            }
        }

        Assert.True(
            wrong.Count == 0,
            "docs/analyzer.md's context-rule counts have drifted from ContextRules "
                + $"({walking} walking + {dmlShapes} DML shapes):\n  "
                + string.Join("\n  ", wrong));
    }

    private static string Word(int count) => count < s_numberWords.Length
        ? s_numberWords[count]
        : count.ToString(CultureInfo.InvariantCulture);

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
