using System.IO;
using System.Text.RegularExpressions;

namespace SqlArtisan.Tests;

// Mechanizes the alphabetical convention CLAUDE.md states for Keywords.cs —
// three drifted clusters shipped before this gate (release audit pass 1).
public class KeywordsOrderingTests
{
    [Fact]
    public void Keywords_ConstantsAreOrderedByMemberName()
    {
        string path = Path.Combine(
            FindRepoRoot(), "src", "SqlArtisan", "Internal", "SqlPart", "Keywords.cs");
        List<string> names = [.. File.ReadLines(path)
            .Select(line => Regex.Match(line, @"internal const string (\w+) ="))
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value)];

        Assert.NotEmpty(names);
        Assert.Equal([.. names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase)], names);
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
