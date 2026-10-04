using System.Data;
using Dapper;
using SqlArtisan.Analyzers;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;

namespace SqlArtisan.IntegrationTests.Tests;

/// <summary>
/// Engine facts true at SQL Server 2025 but not at the 2022 baseline lane (#614):
/// <c>||</c> and the <c>REGEXP_*</c> family, as SqlArtisan emits them.
/// </summary>
[Trait("Engine", "SqlServer2025")]
public sealed class SqlServer2025Tests : IClassFixture<SqlServer2025Fixture>
{
    private static readonly string[] s_sweptMembers =
    [
        "DoublePipe", "RegexpLike", "RegexpCount", "RegexpReplace", "RegexpSubstr", "RegexpInstr",
    ];

    private readonly SqlServer2025Fixture _fixture;

    public SqlServer2025Tests(SqlServer2025Fixture fixture)
    {
        _fixture = fixture;
    }

    // Each case is the sweep's own statement, so a verdict here is the cell 2025 would get.
    [Fact]
    public void AddedConstructs_AreAcceptedAt2025()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        List<string> verdicts = [.. MatrixSweepCatalog.Cases
            .Where(c => s_sweptMembers.Contains(c.Key.MemberName))
            .Select(c => $"{Label(c.Key)}: {TryScalar(connection, c.Build(_fixture.Dbms))}")];

        Assert.NotEmpty(verdicts);
        Assert.True(
            verdicts.TrueForAll(v => v.EndsWith(": ok", StringComparison.Ordinal)),
            "SQL Server 2025 verdicts:\n  " + string.Join("\n  ", verdicts));
    }

    // An upgraded database can keep compatibility level 160; whether the 2025 constructs
    // still parse there decides if a version bound alone describes them.
    [Fact]
    public void AddedConstructs_AtCompatibilityLevel160()
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
            .Select(sql => sql + " -> " + TryScalarWithMessage(connection, sql))];

        Assert.True(
            verdicts.TrueForAll(v => !v.Contains("rejected", StringComparison.Ordinal)),
            "Compatibility level 160 verdicts:\n  " + string.Join("\n  ", verdicts));
    }

    // The match-parameter alphabet, read letter by letter as RegexpOptions emits it;
    // '' is RegexpOptions.None.
    [Fact]
    public void RegexpMatchParameter_Alphabet()
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

    private static string TryScalar(IDbConnection connection, ISqlBuilder builder)
    {
        try
        {
            connection.ExecuteScalar(builder);
            return "ok";
        }
        catch (Exception ex)
        {
            return "rejected (" + ex.Message.Split('\n')[0].Trim() + ")";
        }
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

    private static string TryScalarWithMessage(IDbConnection connection, string sql)
    {
        try
        {
            return Convert.ToString(connection.ExecuteScalar(sql)) ?? "null";
        }
        catch (Exception ex)
        {
            return "rejected (" + ex.Message.Split('\n')[0].Trim() + ")";
        }
    }

    private static string Label(MatrixKey key) =>
        key.Arity is { } arity ? $"{key.MemberName}/arity{arity}" : key.MemberName;
}
