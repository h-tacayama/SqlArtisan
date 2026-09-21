namespace SqlArtisan.Tests;

// .editorconfig's max_line_length is editor-only, so this sweep is the gate:
// zero lines over 100 columns, bar the shapes csharp-formatting.md exempts.
public class LineLengthSweepTests
{
    // Each exemption is a rule in csharp-formatting.md, never a count in a
    // baseline: a row of a data table and a doc tag's own signature do not wrap.
    private static readonly string[] DocTags =
        ["<param", "<returns", "<exception", "<typeparam", "<inheritdoc cref="];

    [Fact]
    public void NoLineOver100Columns()
    {
        string root = CommentCapRatchetTests.FindRepoRoot();
        List<string> offenders = [];

        foreach (string path in CommentCapRatchetTests.SourceFiles(root))
        {
            bool inRawString = false;
            int lineNumber = 0;

            foreach (string line in File.ReadLines(path))
            {
                lineNumber++;

                // A raw string can open mid-line (`RunSilent("""`) and close on
                // its own line, so the delimiter count is what toggles.
                if (Occurrences(line, "\"\"\"") % 2 == 1)
                {
                    inRawString = !inRawString;
                    continue;
                }

                if (line.Length > 100 && !inRawString && !IsExempt(line))
                {
                    offenders.Add(
                        $"{Path.GetRelativePath(root, path).Replace('\\', '/')}:{lineNumber}"
                            + $" ({line.Length} columns)");
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            $"{offenders.Count} line(s) over 100 columns — wrap them, or state the exemption in "
                + $"csharp-formatting.md and read it here:\n  " + string.Join("\n  ", offenders));
    }

    private static int Occurrences(string line, string token)
    {
        int count = 0;
        int at = line.IndexOf(token, StringComparison.Ordinal);

        while (at >= 0)
        {
            count++;
            at = line.IndexOf(token, at + token.Length, StringComparison.Ordinal);
        }

        return count;
    }

    private static bool IsExempt(string line)
    {
        string trimmed = line.TrimStart();

        return DocTags.Any(tag => line.Contains(tag, StringComparison.Ordinal))
            || line.Contains("\"\"\"", StringComparison.Ordinal)
            || IsTableRow(trimmed);
    }

    // One row of a data table: a collection-initializer entry, or a catalog's
    // single registration call naming one construct.
    private static bool IsTableRow(string trimmed) =>
        (trimmed.StartsWith('[') && trimmed.EndsWith(','))
        || (trimmed.StartsWith("Add", StringComparison.Ordinal) && trimmed.EndsWith(");"));
}
