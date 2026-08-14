using System.Text.RegularExpressions;

namespace SqlArtisan.Tests;

// docs-style.md's Hazard callouts rule bounds `[!WARNING]` to 10 `> ` lines
// and `[!NOTE]` to 5, with no table inside a NOTE — the length bound issue
// #458 found nothing enforcing. This gate reads every page under docs/ and
// checks both mechanically, the same read-the-real-files philosophy as
// DocsIndexTests. It only classifies `[!WARNING]`/`[!NOTE]`: the rule does
// not define a severity taxonomy for `[!IMPORTANT]`/`[!TIP]`/`[!CAUTION]`,
// so those are left unchecked rather than silently under-enforced.
public class DocsCalloutTests
{
    private const int WarningMaxLines = 10;
    private const int NoteMaxLines = 5;

    private static readonly Regex s_tableRowPattern = new(
        @"^>\s*\|(\s*:?-+:?\s*\|)+\s*$", RegexOptions.Compiled);

    public static IEnumerable<object[]> Pages()
    {
        string root = FindRepoRoot();
        string docsDir = Path.Combine(root, "docs");

        foreach (string path in Directory.EnumerateFiles(docsDir, "*.md", SearchOption.AllDirectories))
        {
            yield return [Path.GetRelativePath(root, path).Replace('\\', '/')];
        }
    }

    [Theory]
    [MemberData(nameof(Pages))]
    public void Callouts_StayWithinLengthAndTableBounds(string page)
    {
        string root = FindRepoRoot();
        string[] lines = File.ReadAllLines(Path.Combine(root, page));

        int i = 0;

        while (i < lines.Length)
        {
            Match header = Regex.Match(lines[i], @"^> \[!(WARNING|NOTE)\]$");

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
            else
            {
                Assert.True(
                    lineCount <= NoteMaxLines,
                    $"{page}: [!NOTE] at line {start + 1} is {lineCount} lines (max {NoteMaxLines}).");

                Assert.False(
                    lines[start..i].Any(l => s_tableRowPattern.IsMatch(l)),
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

        if (dir is null)
        {
            throw new InvalidOperationException("Could not find repo root (SqlArtisan.sln) above " + AppContext.BaseDirectory);
        }

        return dir.FullName;
    }
}
