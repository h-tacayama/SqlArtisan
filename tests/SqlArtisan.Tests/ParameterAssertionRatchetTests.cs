using System.Text.RegularExpressions;

namespace SqlArtisan.Tests;

// unit-tests.md asks a test that binds a literal to assert sql.Parameters too;
// the pre-convention sites are a fixed per-file baseline (unit-tests.md), so a new or
// edited test that pins a marker without reading it back fails the suite
// (release audit pass 7: six such tests had been added after the convention).
public class ParameterAssertionRatchetTests
{
    private static readonly Regex s_marker = new(@"[:@?]0\b", RegexOptions.Compiled);
    private static readonly Regex s_testStart = new(
        @"\n    \[(?:Fact|Theory)",
        RegexOptions.Compiled);

    private static readonly Dictionary<string, int> s_baseline = new(StringComparer.Ordinal)
    {
        ["ArithmeticTests.cs"] = 6,
        ["ArrayBindTests/OracleArrayBindTests.cs"] = 1,
        ["BuilderReuseTests.cs"] = 4,
        ["CaseTests.cs"] = 22,
        ["ConditionTests/BetweenTests.cs"] = 2,
        ["ConditionTests/ComparisonTests.cs"] = 6,
        ["ConditionTests/ConditionIfTests.cs"] = 2,
        ["ConditionTests/ExistsTests.cs"] = 3,
        ["ConditionTests/InSubqueryTests.cs"] = 2,
        ["ConditionTests/InTests.cs"] = 8,
        ["ConditionTests/LikeTests.cs"] = 6,
        ["ConditionTests/LogicalConditionTests.cs"] = 9,
        ["ConditionTests/RegexpLikeTests.cs"] = 7,
        ["ConfigTests.cs"] = 4,
        ["CookbookTests.cs"] = 7,
        ["DbTableTests.cs"] = 6,
        ["DeleteTests.cs"] = 5,
        ["DerivedTableTests.cs"] = 3,
        ["FunctionTests/FunctionTests.A.cs"] = 1,
        ["FunctionTests/FunctionTests.C.cs"] = 2,
        ["FunctionTests/FunctionTests.G.cs"] = 1,
        ["FunctionTests/FunctionTests.L.cs"] = 4,
        ["FunctionTests/FunctionTests.M.cs"] = 2,
        ["FunctionTests/FunctionTests.N.cs"] = 2,
        ["FunctionTests/FunctionTests.P.cs"] = 2,
        ["FunctionTests/FunctionTests.R.cs"] = 18,
        ["FunctionTests/FunctionTests.S.cs"] = 5,
        ["FunctionTests/FunctionTests.T.cs"] = 9,
        ["FunctionTests/FunctionTests.U.cs"] = 2,
        ["HavingTests.cs"] = 3,
        ["InsertTests.cs"] = 5,
        ["JsonOperatorTests.cs"] = 1,
        ["MultiRowInsertTests.cs"] = 4,
        ["PaginationTests.cs"] = 8,
        ["PublicSurfaceNamingTests.cs"] = 2,
        ["ReturningTests.cs"] = 9,
        ["SetOperatorTests/ExceptTests.cs"] = 6,
        ["SetOperatorTests/IntersectTests.cs"] = 6,
        ["SetOperatorTests/MinusTests.cs"] = 6,
        ["SetOperatorTests/UnionTests.cs"] = 6,
        ["SqlBuilderTests/BuildTests.cs"] = 5,
        ["SubqueryTests.cs"] = 1,
        ["UpdateTests.cs"] = 4,
        ["UpsertTests.cs"] = 11,
        ["WithTests.cs"] = 12,
    };

    [Fact]
    public void MarkerWithoutParameterAssertion_MatchesTheBaselineExactly()
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

            int count = CountUnreadMarkers(File.ReadAllText(path));
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
                $"{file} has {count} test(s) pinning a bind marker without asserting "
                    + $"sql.Parameters "
                    + $"(baseline {allowed}): read the value back (unit-tests.md).");
        }

        foreach ((string file, int allowed) in s_baseline)
        {
            Assert.True(
                found.GetValueOrDefault(file) == allowed,
                $"{file} now has {found.GetValueOrDefault(file)} such test(s), not {allowed}: "
                    + "lower its baseline.");
        }
    }

    // A test pins a marker when its text contains a positional marker (:0, @0, ?0)
    // and reads none back when it never mentions Parameters.
    private static int CountUnreadMarkers(string source) =>
        s_testStart.Split(source)
            .Skip(1)
            .Count(test =>
                s_marker.IsMatch(test) && !test.Contains("Parameters", StringComparison.Ordinal));

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
