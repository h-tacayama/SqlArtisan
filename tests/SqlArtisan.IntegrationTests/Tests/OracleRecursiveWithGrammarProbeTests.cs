using System.Data;
using System.Data.Common;
using Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;

namespace SqlArtisan.IntegrationTests.Tests;

// TEMPORARY discovery probe (#263 follow-up) — deliberately fails to surface the
// accept/reject table in CI logs; deleted once the WithRecursive Oracle facts are
// promoted into DialectMatrix. Raw SQL on purpose: the point is the engine's
// grammar, not SqlArtisan's emission.
internal static class OracleRecursiveWithGrammarProbe
{
    internal static readonly (string Label, string Sql)[] Variants =
    [
        ("P1 RECURSIVE + column list, bare name",
            "WITH RECURSIVE rcte(n) AS (SELECT 1 FROM DUAL UNION ALL "
                + "SELECT n + 1 FROM rcte WHERE n < 3) SELECT MAX(n) FROM rcte"),
        ("P2 RECURSIVE + column list, quoted name",
            "WITH RECURSIVE \"rcte\"(n) AS (SELECT 1 FROM DUAL UNION ALL "
                + "SELECT n + 1 FROM \"rcte\" WHERE n < 3) SELECT MAX(n) FROM \"rcte\""),
        ("P3 plain WITH + column list (11gR2 classic)",
            "WITH rcte(n) AS (SELECT 1 FROM DUAL UNION ALL "
                + "SELECT n + 1 FROM rcte WHERE n < 3) SELECT MAX(n) FROM rcte"),
        ("P4 RECURSIVE, no column list (current emission shape)",
            "WITH RECURSIVE rcte AS (SELECT 1 AS n FROM DUAL UNION ALL "
                + "SELECT n + 1 FROM rcte WHERE n < 3) SELECT MAX(n) FROM rcte"),
    ];

    internal static string Run(IDbConnection connection, string engineLabel)
    {
        List<string> results = [];
        foreach ((string label, string sql) in Variants)
        {
            try
            {
                connection.ExecuteScalar(sql);
                results.Add($"{label}: ACCEPTED");
            }
            catch (DbException ex)
            {
                results.Add($"{label}: REJECTED — {ex.Message.Split('\n')[0].Trim()}");
            }
        }

        return $"PROBE RESULTS ({engineLabel}):\n  " + string.Join("\n  ", results);
    }
}

[Trait("Engine", "Oracle23ai")]
public sealed class Oracle23aiRecursiveWithGrammarProbeTests : IClassFixture<Oracle23aiFixture>
{
    private readonly Oracle23aiFixture _fixture;

    public Oracle23aiRecursiveWithGrammarProbeTests(Oracle23aiFixture fixture) => _fixture = fixture;

    [Fact]
    public void Probe_RecursiveWithGrammar_ReportsResults()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        Assert.Fail(OracleRecursiveWithGrammarProbe.Run(connection, "Oracle 23ai"));
    }
}

[Trait("Engine", "Oracle")]
public sealed class OracleRecursiveWithGrammarProbeTests : IClassFixture<OracleFixture>
{
    private readonly OracleFixture _fixture;

    public OracleRecursiveWithGrammarProbeTests(OracleFixture fixture) => _fixture = fixture;

    [Fact]
    public void Probe_RecursiveWithGrammar_ReportsResults()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        Assert.Fail(OracleRecursiveWithGrammarProbe.Run(connection, "Oracle XE 21c"));
    }
}
