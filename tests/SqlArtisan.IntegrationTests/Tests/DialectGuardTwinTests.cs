using System.Text.RegularExpressions;

namespace SqlArtisan.IntegrationTests.Tests;

/// <summary>
/// Engine-independent gate over ADR 0011's "Later instances": every dialect
/// guard names the live test that executes its raw statement per claimed
/// dialect, or declares the lane it still owes. Runs without a database.
/// </summary>
public sealed partial class DialectGuardTwinTests
{
    // A guard scoped to two dialects on one engine's evidence is how pass 8
    // shipped a SQLite regression; the twin sentence makes each claim show its lane.
    [Fact]
    public void EveryLaterInstance_NamesItsLiveTwinsOrTheOwedLane()
    {
        string root = FindRepoRoot();
        string adr = File.ReadAllText(
            Directory.EnumerateFiles(Path.Combine(root, "docs", "adr"), "0011-*.md").Single());
        string section = adr[adr.IndexOf("## Later instances", StringComparison.Ordinal)..];
        HashSet<string> testNames = TestMethodNames(
            Path.Combine(root, "tests", "SqlArtisan.IntegrationTests", "Tests"));

        string[] entries = [.. EntryStart().Split(section).Skip(1)];
        Assert.NotEmpty(entries);

        List<string> problems = [];
        foreach (string entry in entries)
        {
            string title = entry[..entry.IndexOf("**", StringComparison.Ordinal)];
            Match twins = TwinsSentence().Match(entry);
            if (!twins.Success)
            {
                problems.Add($"{title}: no `Live twins:` or `Live twin owed:` sentence");
                continue;
            }

            if (twins.Groups["owed"].Success)
            {
                continue;
            }

            foreach (Match name in BacktickedName().Matches(twins.Groups["names"].Value))
            {
                if (!testNames.Contains(name.Groups["method"].Value))
                {
                    problems.Add(
                        $"{title}: twin {name.Value} is not a test in the integration lanes");
                }
            }
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    private static HashSet<string> TestMethodNames(string testsDirectory)
    {
        HashSet<string> names = new(StringComparer.Ordinal);
        foreach (string path in Directory.EnumerateFiles(testsDirectory, "*.cs"))
        {
            foreach (Match m in TestMethod().Matches(File.ReadAllText(path)))
            {
                names.Add(m.Groups["name"].Value);
            }
        }

        return names;
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

    [GeneratedRegex(@"^- \*\*", RegexOptions.Multiline)]
    private static partial Regex EntryStart();

    // The sentence runs to the entry's end; "Live twin owed:" is a declared debt.
    [GeneratedRegex(@"Live twins?:(?<names>[^.]*)\.|(?<owed>Live twin owed:)")]
    private static partial Regex TwinsSentence();

    [GeneratedRegex(@"`(?:[A-Za-z0-9]+\.)?(?<method>[A-Za-z0-9_]+)`")]
    private static partial Regex BacktickedName();

    [GeneratedRegex(@"public (?:async Task|void) (?<name>[A-Za-z0-9_]+)\(")]
    private static partial Regex TestMethod();
}
