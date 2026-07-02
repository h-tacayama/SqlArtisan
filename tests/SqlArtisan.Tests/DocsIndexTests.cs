using System.Text;
using System.Text.RegularExpressions;

namespace SqlArtisan.Tests;

// docs/README.md (the reference home) indexes each reference page's sections.
// That index has drifted repeatedly when a feature added a section without
// updating it (#210), so this gate compares the pages' `## ` headings against
// the index mechanically — the same philosophy as KeywordsTests and the BOM
// gate (#134).
public class DocsIndexTests
{
    // Pages whose every `## ` section must be linked from docs/README.md.
    // query-statements.md is excluded: its index links `### ` subsections
    // through grouped entries, which this heading-level check cannot mirror.
    private static readonly string[] s_indexedPages =
    [
        "docs/expressions.md",
        "docs/functions.md",
    ];

    [Fact]
    public void DocsReadme_EveryReferenceSection_IsLinked()
    {
        string root = FindRepoRoot();
        string index = File.ReadAllText(Path.Combine(root, "docs", "README.md"));

        foreach (string page in s_indexedPages)
        {
            string[] lines = File.ReadAllLines(Path.Combine(root, page));

            foreach (string line in lines)
            {
                if (!line.StartsWith("## ", StringComparison.Ordinal))
                {
                    continue;
                }

                string heading = line[3..].Trim();

                if (heading == "Contents")
                {
                    continue;
                }

                string anchor = $"{Path.GetFileName(page)}#{ToGitHubAnchor(heading)}";

                Assert.True(
                    index.Contains(anchor, StringComparison.Ordinal),
                    $"docs/README.md has no link to \"{heading}\" ({anchor}) — add it to the index in page order.");
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

    // GitHub's heading-to-anchor slug: lowercase, drop everything that is not a
    // letter, digit, space, or hyphen, then turn spaces into hyphens.
    private static string ToGitHubAnchor(string heading)
    {
        StringBuilder slug = new(heading.Length);

        foreach (char c in heading.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c) || c == '-')
            {
                slug.Append(c);
            }
            else if (c == ' ')
            {
                slug.Append('-');
            }
        }

        return slug.ToString();
    }
}
