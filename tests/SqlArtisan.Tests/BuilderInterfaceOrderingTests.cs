using System.IO;
using System.Text.RegularExpressions;

namespace SqlArtisan.Tests;

// Mechanizes sql-building-style.md rule 5 for a builder interface's base list:
// ISqlBuilder first when present, the rest alphabetical (release audit pass 6
// found seven lists ordered by hand).
public class BuilderInterfaceOrderingTests
{
    private static readonly Regex s_interfacePattern = new(
        @"public interface (\w+)\s*:\s*([^{]+?)\s*\{", RegexOptions.Compiled);

    public static IEnumerable<object[]> InterfaceFiles() =>
        Directory.GetFiles(
                Path.Combine(FindRepoRoot(), "src", "SqlArtisan", "Internal", "SqlBuilder"),
                "I*.cs",
                SearchOption.AllDirectories)
            .Where(path => s_interfacePattern.IsMatch(File.ReadAllText(path)))
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => new object[] { Path.GetRelativePath(FindRepoRoot(), path) });

    [Theory]
    [MemberData(nameof(InterfaceFiles))]
    public void BuilderInterface_BaseListIsOrdered(string relativePath)
    {
        Match match = s_interfacePattern.Match(
            File.ReadAllText(Path.Combine(FindRepoRoot(), relativePath)));
        List<string> bases = [.. match.Groups[2].Value.Split(',').Select(b => b.Trim())];

        List<string> expected = [.. bases
            .Where(b => b != "ISqlBuilder")
            .OrderBy(b => b, StringComparer.OrdinalIgnoreCase)
            .ThenBy(b => b, StringComparer.Ordinal)];
        if (bases.Contains("ISqlBuilder"))
        {
            expected.Insert(0, "ISqlBuilder");
        }

        Assert.Equal(expected, bases);
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
