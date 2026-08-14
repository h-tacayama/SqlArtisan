using System.Text.RegularExpressions;

namespace SqlArtisan.Tests;

// docs-style.md's Hazard callouts rule bounds `[!WARNING]` to 10 `> ` lines
// and `[!NOTE]` to 5, with no table inside a NOTE — the length bound issue
// #458 found nothing enforcing. This gate reads every doc page with callouts
// and checks both mechanically, the same read-the-real-files philosophy as
// DocsIndexTests.
public class DocsCalloutTests
{
    private const int WarningMaxLines = 10;
    private const int NoteMaxLines = 5;

    private static readonly string[] s_pagesWithCallouts =
    [
        "docs/functions.md",
        "docs/expressions.md",
        "docs/query-statements.md",
    ];

    public static IEnumerable<object[]> Pages() => s_pagesWithCallouts.Select(p => new object[] { p });

    [Theory]
    [MemberData(nameof(Pages))]
    public void Callouts_StayWithinLengthAndTableBounds(string page)
    {
        string root = FindRepoRoot();
        string[] lines = File.ReadAllLines(Path.Combine(root, page));

        int i = 0;

        while (i < lines.Length)
        {
            Match header = Regex.Match(lines[i], @"^> \[!(WARNING|NOTE|IMPORTANT|TIP|CAUTION)\]$");

            if (!header.Success)
            {
                i++;
                continue;
            }

            string kind = header.Groups[1].Value;
            int start = i;

            while (i < lines.Length && lines[i].StartsWith(">", StringComparison.Ordinal))
            {
                i++;
            }

            int lineCount = i - start;

            if (kind == "WARNING")
            {
                Assert.True(
                    lineCount <= WarningMaxLines,
                    $"{page}: [!WARNING] at line {start + 1} is {lineCount} lines (max {WarningMaxLines}).");
            }
            else if (kind == "NOTE")
            {
                Assert.True(
                    lineCount <= NoteMaxLines,
                    $"{page}: [!NOTE] at line {start + 1} is {lineCount} lines (max {NoteMaxLines}).");

                string body = string.Join('\n', lines[start..i]);
                Assert.False(
                    body.Contains("|---", StringComparison.Ordinal),
                    $"{page}: [!NOTE] at line {start + 1} contains a table — tables are WARNING-only.");
            }
        }
    }

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
