using System.Text.RegularExpressions;

namespace SqlArtisan.Tests;

// llms-full.txt is llms.txt's full-text companion: every page llms.txt links
// via a raw.githubusercontent.com URL, concatenated verbatim in that order.
// It is hand-maintained (regenerate with the command below), so this gate
// checks it byte-for-byte against the real source files — the same
// read-the-real-files-and-assert philosophy as DocsIndexTests (#210), one
// level stricter since faithful reproduction is this file's entire point.
//
// Regenerate from the repo root:
//   { printf '%s\n' "# SqlArtisan — Full Documentation" "" \
//       "> The full-text companion to llms.txt: every page it links via a raw-content" \
//       "> URL, concatenated in that same order, for AI tools that ingest one file" \
//       "> instead of following links. See llms.txt first for the short index; this" \
//       "> file is the deep-ingestion counterpart."; \
//     for f in README.md docs/README.md docs/query-statements.md docs/expressions.md \
//         docs/functions.md docs/analyzer.md docs/cookbook.md docs/comparison.md \
//         docs/guides/dapper-quickstart.md docs/guides/ai-assistants.md \
//         docs/versioning.md CHANGELOG.md; do \
//       printf '\n<!-- %s -->\n<!-- SOURCE: %s -->\n<!-- %s -->\n\n' \
//         '============================================================' "$f" \
//         '============================================================'; \
//       cat "$f"; \
//     done; } > llms-full.txt
public class LlmsFullTests
{
    private static readonly Regex RawUrlPattern = new(
        @"https://raw\.githubusercontent\.com/h-tacayama/SqlArtisan/main/(\S+?)\)",
        RegexOptions.Compiled);

    // Matches the whole 3-line delimiter block (border/SOURCE/border), not just
    // the middle line, so a section's boundaries exclude the delimiter itself.
    private static readonly Regex SourceDelimiterPattern = new(
        @"<!-- =+ -->\r?\n<!-- SOURCE: (\S+) -->\r?\n<!-- =+ -->",
        RegexOptions.Compiled);

    [Fact]
    public void LlmsFullTxt_SourceOrder_MatchesLlmsTxtRawLinkOrder()
    {
        string root = FindRepoRoot();
        string llmsTxt = File.ReadAllText(Path.Combine(root, "llms.txt"));
        string llmsFullTxt = File.ReadAllText(Path.Combine(root, "llms-full.txt"));

        List<string> expected = [.. RawUrlPattern.Matches(llmsTxt).Select(m => m.Groups[1].Value)];
        List<string> actual = [.. SourceDelimiterPattern.Matches(llmsFullTxt).Select(m => m.Groups[1].Value)];

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LlmsFullTxt_EverySection_MatchesSourceFileExactly()
    {
        string root = FindRepoRoot();
        string llmsFullTxt = File.ReadAllText(Path.Combine(root, "llms-full.txt"));

        MatchCollection sources = SourceDelimiterPattern.Matches(llmsFullTxt);
        Assert.True(sources.Count > 0, "llms-full.txt has no `<!-- SOURCE: ... -->` sections.");

        for (int i = 0; i < sources.Count; i++)
        {
            string path = sources[i].Groups[1].Value;
            int sectionStart = sources[i].Index + sources[i].Length;
            int sectionEnd = i + 1 < sources.Count ? sources[i + 1].Index : llmsFullTxt.Length;

            // Each section is preceded/followed by the delimiter's own comment-block
            // border lines and blank-line padding; trim to compare the embedded copy
            // against the source file's own trimmed content.
            string embedded = llmsFullTxt[sectionStart..sectionEnd].Trim();
            string sourceContent = File.ReadAllText(Path.Combine(root, path)).Trim();

            Assert.True(
                embedded == sourceContent,
                $"llms-full.txt's copy of \"{path}\" is stale — regenerate llms-full.txt (see this file's header comment).");
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
