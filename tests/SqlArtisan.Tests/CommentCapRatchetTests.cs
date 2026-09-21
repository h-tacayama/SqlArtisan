using System.Text.RegularExpressions;

namespace SqlArtisan.Tests;

// Mechanizes code-comments.md's caps like the other ratchets: every over-cap
// block is pinned per file in Baselines/comment-caps.txt (code-comments.md's pre-rule
// survivors), so a new or edited comment may not add one.
public class CommentCapRatchetTests
{
    private static readonly Regex s_declaration = new(
        @"^\s*(?:\[|(?:public|private|protected|internal|static|sealed|abstract|override|virtual"
            + @"|readonly|partial|async|extern|new|const|class|interface|struct|record|enum"
            + @"|delegate"
            + @"|event|namespace|using)\b)",
        RegexOptions.Compiled);

    private static readonly Regex s_partStart = new(
        @"<(summary|remarks|para|param|returns|exception|typeparam|value|example)\b", RegexOptions
            .Compiled);

    private static readonly Regex s_partEnd = new(
        @"</(summary|remarks|para|param|returns|exception|typeparam|value|example)>", RegexOptions
            .Compiled);

    [Fact]
    public void OverCapCommentBlocks_MatchTheBaselineExactly()
    {
        string root = FindRepoRoot();
        Dictionary<string, int> found = new(StringComparer.Ordinal);

        foreach (string path in SourceFiles(root))
        {
            int count = CountOverCap(File.ReadAllLines(path));
            if (count > 0)
            {
                found[Path.GetRelativePath(root, path).Replace('\\', '/')] = count;
            }
        }

        RatchetBaseline.AssertMatches(
            Path.Combine(root, "tests", "SqlArtisan.Tests", "Baselines", "comment-caps.txt"),
            found,
            "over-cap comment block(s)",
            "trim to the why (code-comments.md), or lower the baseline when a block was trimmed");
    }

    internal static IEnumerable<string> SourceFiles(string root)
    {
        foreach (string top in new[] { "src", "tests" })
        {
            foreach (string path in Directory.EnumerateFiles(
                Path.Combine(root, top), "*.cs", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(root, path).Replace('\\', '/');
                if (!relative.Contains("/bin/", StringComparison.Ordinal)
                    && !relative.Contains("/obj/", StringComparison.Ordinal))
                {
                    yield return path;
                }
            }
        }
    }

    private static int CountOverCap(string[] lines)
    {
        int count = 0;
        int i = 0;
        while (i < lines.Length)
        {
            string trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith("///", StringComparison.Ordinal))
            {
                int start = i;
                while (i < lines.Length
                    && lines[i].TrimStart().StartsWith("///", StringComparison.Ordinal))
                {
                    i++;
                }

                count += OverCapDocParts(lines, start, i);
            }
            else if (trimmed.StartsWith("//", StringComparison.Ordinal))
            {
                int start = i;
                while (i < lines.Length
                    && lines[i].TrimStart().StartsWith("//", StringComparison.Ordinal)
                    && !lines[i].TrimStart().StartsWith("///", StringComparison.Ordinal))
                {
                    i++;
                }

                int next = i;
                while (next < lines.Length && lines[next].Trim().Length == 0)
                {
                    next++;
                }

                bool header = start == 0 || next >= lines.Length || s_declaration.IsMatch(
                    lines[next]);
                if (i - start > (header ? 3 : 2))
                {
                    count++;
                }
            }
            else
            {
                i++;
            }
        }

        return count;
    }

    // Prose lines per part: a line carrying only tags is a boundary, not prose;
    // a part with text after its opening tag counts that line.
    private static int OverCapDocParts(string[] lines, int start, int end)
    {
        int over = 0;
        int prose = 0;
        for (int i = start; i < end; i++)
        {
            string text = lines[i].TrimStart().Substring(3).Trim();
            bool starts = s_partStart.IsMatch(text);
            bool ends = s_partEnd.IsMatch(text);
            string stripped = Regex.Replace(
                text,
                @"</?(summary|remarks|para|param[^>]*|returns|exception[^>]*|typeparam[^>]*"
                    + @"|value|example)>",
                string.Empty).Trim();

            if (starts && !ends)
            {
                prose = stripped.Length > 0 ? 1 : 0;
            }
            else if (ends)
            {
                if (stripped.Length > 0 && !starts)
                {
                    prose++;
                }

                if (prose > 3)
                {
                    over++;
                }

                prose = 0;
            }
            else if (stripped.Length > 0)
            {
                prose++;
            }
        }

        return over;
    }

    internal static string FindRepoRoot()
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
