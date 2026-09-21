using System.Text.RegularExpressions;

namespace SqlArtisan.Tests;

// Mechanizes unit-tests.md's guard-assertion clause, scoped as that rule says:
// the pre-convention bare throws are a fixed per-file baseline, and a new or
// edited test may not add one — assert the message or ParamName instead.
public class GuardAssertionRatchetTests
{
    private static readonly Regex s_bareThrow = new(
        @"^\s*(?:await )?Assert\.Throws(?:Async)?<Argument\w*>\(", RegexOptions.Compiled);

    // A captured throw followed by a prefix/substring check on its message pins
    // only part of a fixed string (release audit pass 8).
    private static readonly Regex s_partialMessage = new(
        @"Assert\.(?:StartsWith|Contains|EndsWith)\([^;]*\.Message\)", RegexOptions.Compiled);

    private static readonly Dictionary<string, int> s_baseline = new(StringComparer.Ordinal)
    {
        ["IncompleteExpressionMessageTests.cs"] = 10,
        ["AggregateWindowTests.cs"] = 2,
        ["PercentileTests.cs"] = 2,
        ["SqlBuilderTests/ExpressionResolverTests.cs"] = 3,
        ["WindowFunctionTests/WindowCumeDistTests.cs"] = 1,
        ["WindowFunctionTests/WindowDenseRankTests.cs"] = 1,
        ["WindowFunctionTests/WindowFirstValueTests.cs"] = 1,
        ["WindowFunctionTests/WindowLagTests.cs"] = 1,
        ["WindowFunctionTests/WindowLastValueTests.cs"] = 1,
        ["WindowFunctionTests/WindowLeadTests.cs"] = 1,
        ["WindowFunctionTests/WindowNthValueTests.cs"] = 1,
        ["WindowFunctionTests/WindowNtileTests.cs"] = 1,
        ["WindowFunctionTests/WindowPercentRankTests.cs"] = 1,
        ["WindowFunctionTests/WindowRankTests.cs"] = 1,
        ["WindowFunctionTests/WindowRowNumberTests.cs"] = 1,
    };

    [Fact]
    public void BareArgumentThrows_MatchTheBaselineExactly()
    {
        string root = Path.Combine(FindRepoRoot(), "tests", "SqlArtisan.Tests");
        Dictionary<string, int> found = new(StringComparer.Ordinal);

        foreach (string path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(root, path).Replace('\\', '/');
            if (relative.StartsWith("bin/", StringComparison.Ordinal)
                || relative.StartsWith("obj/", StringComparison.Ordinal))
            {
                continue;
            }

            int count = CountBareThrows(File.ReadAllLines(path));
            if (count > 0)
            {
                found[relative] = count;
            }
        }

        foreach ((string file, int count) in found)
        {
            int allowed = s_baseline.GetValueOrDefault(file);
            Assert.True(
                count <= allowed,
                $"{file} has {count} bare Assert.Throws<Argument*> call(s) (baseline {allowed}): "
                    + "assert ex.Message or ex.ParamName (unit-tests.md); the grace covers only "
                        + "the baseline.");
        }

        foreach ((string file, int allowed) in s_baseline)
        {
            Assert.True(
                found.GetValueOrDefault(file) == allowed,
                $"{file} now has {found.GetValueOrDefault(file)} bare throw(s), not {allowed}: "
                    + $"lower its baseline.");
        }
    }

    // A throw is bare when nothing captures it: the line starts the call and
    // the previous line does not end in an assignment.
    private static int CountBareThrows(string[] lines)
    {
        int count = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            if (!s_bareThrow.IsMatch(lines[i]))
            {
                continue;
            }

            string previous = string.Empty;
            for (int j = i - 1; j >= 0 && previous.Length == 0; j--)
            {
                previous = lines[j].Trim();
            }

            if (!previous.EndsWith('='))
            {
                count++;
            }
            else if (PartialMessageAssertionFollows(lines, i))
            {
                count++;
            }
        }

        return count;
    }

    private static bool PartialMessageAssertionFollows(string[] lines, int throwLine)
    {
        for (int j = throwLine + 1;
            j < lines.Length && !lines[j].Contains("[Fact]") && !lines[j].Contains("[Theory]");
            j++)
        {
            if (s_partialMessage.IsMatch(lines[j]))
            {
                return true;
            }
        }

        return false;
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
