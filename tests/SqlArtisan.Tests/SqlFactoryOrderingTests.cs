using System.IO;
using System.Text.RegularExpressions;

namespace SqlArtisan.Tests;

// Mechanizes the alphabetical convention CLAUDE.md states for Sql.<Letter>.cs —
// seven files had drifted before this gate (release audit pass 6). Overloads
// share a name, so the stable sort keeps them adjacent.
public class SqlFactoryOrderingTests
{
    private static readonly Regex s_memberPattern = new(
        @"^    public static [^(=]*?\b(\w+)(?:<[\w, ]+>)?\s*(?:\(|=>|\{)", RegexOptions.Compiled);

    public static IEnumerable<object[]> FactoryFiles() =>
        Directory.GetFiles(Path.Combine(FindRepoRoot(), "src", "SqlArtisan", "Sql"), "Sql.?.cs")
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => new object[] { Path.GetFileName(path) });

    [Theory]
    [MemberData(nameof(FactoryFiles))]
    public void SqlFactoryFile_MembersAreOrderedByName(string fileName)
    {
        string path = Path.Combine(FindRepoRoot(), "src", "SqlArtisan", "Sql", fileName);
        List<string> names = [.. File.ReadLines(path)
            .Select(line => s_memberPattern.Match(line))
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value)];

        Assert.NotEmpty(names);
        Assert.Equal(
            [.. names
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ThenBy(n => n, StringComparer.Ordinal)],
            names);
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
