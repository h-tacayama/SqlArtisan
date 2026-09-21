using System.Text.RegularExpressions;

namespace SqlArtisan.Tests;

// ADR 0002: a SqlPart renders one SQL shape and reads the differences through
// IDbmsDialect, so the target's own name never reaches it. SqlBuildingBuffer
// carries Dbms for the builders' Validate hook, which puts it one hop away.
public partial class DialectBranchSweepTests
{
    [Fact]
    public void NoSqlPartNamesTheTargetDbms()
    {
        string root = CommentCapRatchetTests.FindRepoRoot();
        string parts = Path.Combine(root, "src", "SqlArtisan").Replace('\\', '/');
        List<string> offenders = [];

        foreach (string path in CommentCapRatchetTests.SourceFiles(root))
        {
            string normalized = path.Replace('\\', '/');
            if (!normalized.StartsWith(parts, StringComparison.Ordinal)
                || !normalized.Contains("/SqlPart/", StringComparison.Ordinal))
            {
                continue;
            }

            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].TrimStart().StartsWith("//", StringComparison.Ordinal)
                    || lines[i].TrimStart().StartsWith("///", StringComparison.Ordinal))
                {
                    continue;
                }

                if (DbmsReference().IsMatch(lines[i]))
                {
                    offenders.Add(
                        $"{Path.GetRelativePath(root, path).Replace('\\', '/')}:{i + 1}");
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            $"{offenders.Count} SqlPart line(s) naming Dbms — read the difference through "
                + "IDbmsDialect instead:\n  " + string.Join("\n  ", offenders));
    }

    [GeneratedRegex(@"\bDbms\b")]
    private static partial Regex DbmsReference();
}
