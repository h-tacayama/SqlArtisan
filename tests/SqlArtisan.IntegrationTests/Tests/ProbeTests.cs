using System.Data;
using Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;

namespace SqlArtisan.IntegrationTests.Tests;

// SCRATCH PROBE for #520 — delete before merge.
internal static class Probe
{
    public static string Run(IDbConnection connection, (string Label, string Sql)[] cases)
    {
        System.Text.StringBuilder report = new();
        report.AppendLine();
        foreach ((string label, string sql) in cases)
        {
            try
            {
                List<int> rows = connection.Query<int>(sql).ToList();
                report.AppendLine($"ACCEPT  {label}  rows=[{string.Join(",", rows)}]  :: {sql}");
            }
            catch (Exception ex)
            {
                string message = ex.Message.Replace("\n", " ").Replace("\r", " ");
                report.AppendLine($"REJECT  {label}  {message}  :: {sql}");
            }
        }

        return report.ToString();
    }
}

[Trait("Engine", "Oracle")]
public sealed class OracleProbeTests : IClassFixture<OracleFixture>
{
    private readonly OracleFixture _fixture;

    public OracleProbeTests(OracleFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Probe520()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        string report = Probe.Run(connection,
        [
            ("baseline-fetch", "SELECT id FROM users ORDER BY id FETCH FIRST 1 ROWS ONLY"),
            ("baseline-lock", "SELECT id FROM users ORDER BY id FOR UPDATE"),
            ("fetchfirst+lock",
                "SELECT id FROM users ORDER BY id FETCH FIRST 1 ROWS ONLY FOR UPDATE"),
            ("fetchfirst+lock+skip",
                "SELECT id FROM users ORDER BY id FETCH FIRST 1 ROWS ONLY FOR UPDATE SKIP LOCKED"),
            ("offsetrows+fetchnext+lock",
                "SELECT id FROM users ORDER BY id OFFSET 1 ROWS FETCH NEXT 1 ROWS ONLY FOR UPDATE"),
            ("offsetrows+lock", "SELECT id FROM users ORDER BY id OFFSET 1 ROWS FOR UPDATE"),
            ("lock+fetchfirst",
                "SELECT id FROM users ORDER BY id FOR UPDATE FETCH FIRST 1 ROWS ONLY"),
        ]);

        Assert.Fail(report);
    }
}

[Trait("Engine", "MySql")]
public sealed class MySqlProbeTests : IClassFixture<MySqlFixture>
{
    private readonly MySqlFixture _fixture;

    public MySqlProbeTests(MySqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Probe520()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        string report = Probe.Run(connection,
        [
            ("baseline-limit", "SELECT id FROM users ORDER BY id LIMIT 1"),
            ("baseline-lock", "SELECT id FROM users ORDER BY id FOR UPDATE"),
            ("limit+lock", "SELECT id FROM users ORDER BY id LIMIT 1 FOR UPDATE"),
            ("limit+lock+skip",
                "SELECT id FROM users ORDER BY id LIMIT 1 FOR UPDATE SKIP LOCKED"),
            ("limit+offset+lock", "SELECT id FROM users ORDER BY id LIMIT 1 OFFSET 1 FOR UPDATE"),
            ("lock+limit", "SELECT id FROM users ORDER BY id FOR UPDATE LIMIT 1"),
            ("fetchfirst+lock",
                "SELECT id FROM users ORDER BY id FETCH FIRST 1 ROWS ONLY FOR UPDATE"),
        ]);

        Assert.Fail(report);
    }
}
