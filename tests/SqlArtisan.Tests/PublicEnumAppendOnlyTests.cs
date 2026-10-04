using System.Text.RegularExpressions;

namespace SqlArtisan.Tests;

// Gates docs/versioning.md's "public enum values are append-only" over every shipped
// assembly: a shipped member keeps its number, and a new one is added to the baseline.
public class PublicEnumAppendOnlyTests
{
    // The library packages; TableClassGen ships as a tool, with no API to call.
    private static readonly Type[] s_shippedAnchors =
    [
        typeof(Sql),
        typeof(SqlArtisan.Dapper.SqlMapper),
        typeof(SqlArtisan.ArrayBind.OracleArrayBind),
    ];

    [Fact]
    public void PublicEnumMembers_MatchTheBaselineExactly()
    {
        string path = Path.Combine(
            CommentCapRatchetTests.FindRepoRoot(),
            "tests",
            "SqlArtisan.Tests",
            "Baselines",
            "public-enums.txt");
        List<string> baseline = [.. File.ReadAllLines(path)
            .Where(line => line.Length > 0 && !line.StartsWith('#'))];

        List<string> found = [.. PublicEnums()
            .SelectMany(type => Enum.GetNames(type).Select(name =>
                $"{type.Name}.{name} {Convert.ToInt64(Enum.Parse(type, name), null)}"))
            .OrderBy(entry => entry, StringComparer.Ordinal)];

        Assert.True(
            baseline.SequenceEqual(found),
            "Public enum members drifted from public-enums.txt — a shipped value must keep its "
                + "number; append a new member at the next unused value and add it here:\n  "
                + string.Join("\n  ", found.Except(baseline).Select(e => "+ " + e)
                    .Concat(baseline.Except(found).Select(e => "- " + e))));
    }

    // DatepartKeywords indexes by value and sizes by member count, so an alias that
    // repeats a value breaks it; a replaced member gets a new value (versioning.md).
    [Fact]
    public void PublicEnums_HaveNoDuplicateValues()
    {
        List<string> duplicated = [.. PublicEnums()
            .Where(type => Enum.GetNames(type).Length
                != Enum.GetValues(type).Cast<object>().Distinct().Count())
            .Select(type => type.Name)];

        Assert.Empty(duplicated);
    }

    [Fact]
    public void PublicEnums_AreTheSetVersioningMdNames()
    {
        string text = File.ReadAllText(Path.Combine(
            CommentCapRatchetTests.FindRepoRoot(), "docs", "versioning.md"));
        Match sentence = Regex.Match(
            text,
            @"\*\*Public enum values are append-only\.\*\*(.*?)carry explicit",
            RegexOptions.Singleline);
        Assert.True(sentence.Success);

        List<string> documented = [.. Regex.Matches(sentence.Groups[1].Value, "`(\\w+)`")
            .Select(m => m.Groups[1].Value)
            .OrderBy(name => name, StringComparer.Ordinal)];
        List<string> shipped = [.. PublicEnums()
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)];

        Assert.Equal(shipped, documented);
    }

    private static IEnumerable<Type> PublicEnums() =>
        s_shippedAnchors
            .Select(anchor => anchor.Assembly)
            .Distinct()
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type.IsEnum);
}
