namespace SqlArtisan.Tests;

// The blank-line shapes csharp-formatting.md names that the formatter cannot see
// (IDE2000 gates runs only): none directly after `{` or before `}`.
public class FormattingSweepTests
{
    [Fact]
    public void NoBlankLineBesideABrace()
    {
        string root = CommentCapRatchetTests.FindRepoRoot();
        List<string> offenders = [];

        foreach (string path in CommentCapRatchetTests.SourceFiles(root))
        {
            string[] lines = File.ReadAllLines(path);
            for (int i = 1; i < lines.Length - 1; i++)
            {
                if (lines[i].Trim().Length != 0)
                {
                    continue;
                }

                if (lines[i - 1].TrimEnd().EndsWith('{') || lines[i + 1].Trim() == "}")
                {
                    offenders.Add($"{Path.GetRelativePath(root, path).Replace('\\', '/')}:{i + 1}");
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            $"{offenders.Count} blank line(s) beside a brace:\n  " + string.Join(
                "\n  ",
                offenders));
    }
}
