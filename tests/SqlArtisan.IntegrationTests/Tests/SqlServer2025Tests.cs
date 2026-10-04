using System.Data;
using Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;

namespace SqlArtisan.IntegrationTests.Tests;

/// <summary>
/// SQL Server 2025 facts beside its bound sweep (#614): the <c>REGEXP_*</c> match-parameter
/// alphabet, and what a database kept at compatibility level 160 still accepts.
/// </summary>
[Trait("Engine", "SqlServer2025")]
public sealed class SqlServer2025Tests : IClassFixture<SqlServer2025Fixture>
{
    private readonly SqlServer2025Fixture _fixture;

    public SqlServer2025Tests(SqlServer2025Fixture fixture)
    {
        _fixture = fixture;
    }

    // An upgraded database can keep compatibility level 160, where REGEXP_LIKE alone of
    // the 2025 constructs is not recognized — the version bound cannot see the level.
    [Fact]
    public void AddedConstructs_AtCompatibilityLevel160_RejectOnlyRegexpLike()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        connection.Execute(
            "IF DB_ID('probe_cl160') IS NULL CREATE DATABASE probe_cl160; "
            + "ALTER DATABASE probe_cl160 SET COMPATIBILITY_LEVEL = 160;");
        connection.ChangeDatabase("probe_cl160");
        List<string> verdicts = [.. new[]
            {
                "SELECT 'a' || 'b'",
                "SELECT CASE WHEN REGEXP_LIKE('Ab', 'A') THEN 1 ELSE 0 END",
                "SELECT REGEXP_COUNT('Ab', 'A')",
                "SELECT REGEXP_REPLACE('Ab', 'A', 'x')",
                "SELECT REGEXP_SUBSTR('Ab', 'A')",
                "SELECT REGEXP_INSTR('Ab', 'A')",
            }
            .Select(sql => TryScalar(connection, sql))];

        Assert.Equal(["ab", "rejected", "1", "xb", "A", "1"], verdicts);
    }

    // The live twin of ArgumentValueValidity's SQL Server alphabet: each letter as
    // RegexpOptions emits it ('' is None); 'n' and 'x' are the gaps SQLA0104 reports.
    [Fact]
    public void RegexpLike_MatchParameterLetters_MatchTheAnalyzerAlphabet()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        List<string> verdicts = [.. new[] { "", "c", "i", "m", "n", "x", "ci" }
            .Select(flags => $"'{flags}': " + TryScalar(
                connection,
                $"SELECT CASE WHEN REGEXP_LIKE('Ab', 'ab', '{flags}') THEN 1 ELSE 0 END"))];

        Assert.Equal(
            ["'': 0", "'c': 0", "'i': 1", "'m': 0", "'n': rejected", "'x': rejected", "'ci': 1"],
            verdicts);
    }

    // SQLA0104 reads the alphabet per dialect, not per function (#528), so the 'n' and
    // 'x' gaps are pinned on every other flags taker, each beside a letter it has.
    [Theory]
    [InlineData("SELECT REGEXP_COUNT('Ab', 'ab', 1, '@')")]
    [InlineData("SELECT REGEXP_INSTR('Ab', 'ab', 1, 1, 0, '@')")]
    [InlineData("SELECT REGEXP_REPLACE('Ab', 'ab', 'x', 1, 0, '@')")]
    [InlineData("SELECT REGEXP_SUBSTR('Ab', 'ab', 1, 1, '@')")]
    public void RegexpMatchParameter_NewLineAndExcludingWhiteSpace_AreRejectedByTheEngine(
        string probe)
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.ExecuteScalar(probe.Replace("@", "i"));

        Assert.Equal(
            ["rejected", "rejected"],
            new[] { "n", "x" }.Select(flag => TryScalar(connection, probe.Replace("@", flag))));
    }

    private static string TryScalar(IDbConnection connection, string sql)
    {
        try
        {
            return Convert.ToString(connection.ExecuteScalar(sql)) ?? "null";
        }
        catch (Exception)
        {
            return "rejected";
        }
    }
}
