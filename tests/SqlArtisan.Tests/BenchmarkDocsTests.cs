using System.Text.RegularExpressions;

namespace SqlArtisan.Tests;

// The benchmark README's checkable facts (pinned versions, entrant categories,
// the returned tuple, the validate asserts) live in its source; prose drifts
// silently (#381 shipped three errors in one page), so they are gated here.
public class BenchmarkDocsTests
{
    private const string BenchmarkDir = "tests/SqlArtisan.Benchmark";

    private static readonly Regex PackageReferencePattern = new(
        @"<PackageReference Include=""(?<name>[^""]+)"" Version=""(?<version>[^""]+)""",
        RegexOptions.Compiled);

    private static readonly Regex CategoryConstantPattern = new(
        @"private const string \w+ = ""(?<category>[^""]+)"";",
        RegexOptions.Compiled);

    private static readonly Regex BenchmarkMethodPattern = new(
        @"\[Benchmark\]\s*\r?\n\s*\[BenchmarkCategory\([^)]*\)\]\s*\r?\n\s*public (?<returns>[^\r\n]+?) (?<name>\w+)\(\)",
        RegexOptions.Compiled);

    [Fact]
    public void PinnedVersionTable_DirectPackages_MatchTheCsproj()
    {
        string root = FindRepoRoot();
        string csproj = File.ReadAllText(
            Path.Combine(root, BenchmarkDir, "SqlArtisan.Benchmark.csproj"));

        Dictionary<string, string> pinned = [];
        foreach (Match match in PackageReferencePattern.Matches(csproj))
        {
            pinned[match.Groups["name"].Value] = match.Groups["version"].Value;
        }

        Assert.Equal(pinned, DocumentedVersions(root, transitive: false));
    }

    // Transitive rows earn their place by floating free of a direct pin, so they are
    // the ones a restore can move without touching the csproj.
    [Fact]
    public void PinnedVersionTable_TransitivePackages_MatchTheRestoredGraph()
    {
        string root = FindRepoRoot();
        string assets = File.ReadAllText(
            Path.Combine(root, BenchmarkDir, "obj", "project.assets.json"));

        foreach (KeyValuePair<string, string> row in DocumentedVersions(root, transitive: true))
        {
            Assert.Contains($"\"{row.Key}/{row.Value}\"", assets);
        }
    }

    [Fact]
    public void EntrantTable_Categories_MatchTheBenchmarkSource()
    {
        string root = FindRepoRoot();
        string source = File.ReadAllText(
            Path.Combine(root, BenchmarkDir, "SqlBuilderBenchmarks.cs"));

        string[] declared = [.. CategoryConstantPattern.Matches(source)
            .Select(m => m.Groups["category"].Value)
            .Order()];

        string[] documented = [.. TableRows(ReadDocs(root), "| Category |")
            .Select(cells => cells[0].Trim('`', ' '))
            .Order()];

        Assert.Equal(declared, documented);
    }

    [Fact]
    public void CrossLibraryEntrants_EveryBenchmark_ReturnsSqlAndParameterCount()
    {
        string root = FindRepoRoot();
        string source = File.ReadAllText(
            Path.Combine(root, BenchmarkDir, "SqlBuilderBenchmarks.cs"));

        MatchCollection entrants = BenchmarkMethodPattern.Matches(source);

        Assert.NotEmpty(entrants);
        foreach (Match entrant in entrants)
        {
            Assert.Equal("(string Sql, int ParameterCount)", entrant.Groups["returns"].Value);
        }
    }

    [Theory]
    [InlineData("expectedParameters", "exactly {0} bind parameters")]
    [InlineData("expectedGroupByKeys", "{0} `GROUP BY` keys")]
    public void ValidateDescription_AssertedShape_MatchesTheSource(string constant, string phrasing)
    {
        string root = FindRepoRoot();
        string source = File.ReadAllText(
            Path.Combine(root, BenchmarkDir, "Benchmark", "BenchmarkValidation.cs"));

        Match expected = Regex.Match(source, $@"const int {constant} = (?<count>\d+);");

        Assert.True(expected.Success, $"BenchmarkValidation no longer declares {constant}.");
        Assert.Contains(
            string.Format(phrasing, NumberWord(expected.Groups["count"].Value)),
            Unwrapped(ReadDocs(root)));
    }

    // The claim is the subject here, not the line layout it happens to be wrapped at.
    private static string Unwrapped(string markdown) => Regex.Replace(markdown, @"\s+", " ");

    private static string ReadDocs(string root) =>
        File.ReadAllText(Path.Combine(root, BenchmarkDir, "README.md"));

    // The version table's third column annotates transitive rows and is empty on the
    // direct ones, which is what separates the two halves.
    private static Dictionary<string, string> DocumentedVersions(string root, bool transitive)
    {
        Dictionary<string, string> documented = [];
        foreach (string[] cells in TableRows(ReadDocs(root), "| Package |"))
        {
            if (cells.Length > 2 && cells[2].Trim().Length > 0 == transitive)
            {
                documented[cells[0].Trim()] = cells[1].Trim();
            }
        }

        return documented;
    }

    private static IEnumerable<string[]> TableRows(string markdown, string header)
    {
        string[] lines = markdown.Split('\n');
        int start = Array.FindIndex(lines, line => line.StartsWith(header, StringComparison.Ordinal));

        Assert.True(start >= 0, $"The docs no longer contain a table headed '{header}'.");

        for (int i = start + 2; i < lines.Length && lines[i].StartsWith('|'); i++)
        {
            yield return lines[i].Trim().Trim('|').Split('|');
        }
    }

    private static string NumberWord(string count) => count switch
    {
        "2" => "two",
        _ => count,
    };

    private static string FindRepoRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);

        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SqlArtisan.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir.FullName;
    }
}
