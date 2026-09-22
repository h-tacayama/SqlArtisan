using System.Data;
using Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;

namespace SqlArtisan.IntegrationTests.Tests;

// TEMPORARY discovery harness for issue #523. Each probe executes a raw shape
// and reports whether the engine accepted or rejected it, so one dispatch of
// integration.yml settles the Oracle and SQL Server facts the container cannot
// reach. Deleted once the outcomes land as real assertions.
internal static class Issue523Probe
{
    // Each probe runs in its own rolled-back transaction: a mutating shape
    // (MERGE) must not disturb the lane's shared seed, and a syntax error can
    // doom the transaction it sits in.
    public static void Report(IDbConnection connection, (string Label, string Sql)[] probes)
    {
        List<string> lines = [];
        foreach ((string label, string sql) in probes)
        {
            using IDbTransaction tx = connection.BeginTransaction();
            try
            {
                connection.ExecuteScalar(sql, transaction: tx);
                lines.Add($"ACCEPTED | {label} | {sql}");
            }
            catch (Exception ex)
            {
                string message = ex.Message.ReplaceLineEndings(" ");
                lines.Add($"REJECTED | {label} | {sql} | {message}");
            }

            try
            {
                tx.Rollback();
            }
            catch (Exception ex)
            {
                lines.Add($"ROLLBACK-FAILED | {label} | {ex.Message.ReplaceLineEndings(" ")}");
            }
        }

        Assert.Fail("ISSUE523-REPORT\n" + string.Join("\n", lines));
    }
}

[Trait("Engine", "Oracle")]
public sealed class Issue523OracleProbeTests : IClassFixture<OracleFixture>
{
    private readonly OracleFixture _fixture;

    public Issue523OracleProbeTests(OracleFixture fixture) => _fixture = fixture;

    [Fact]
    public void Probe()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        Issue523Probe.Report(connection, [
            ("baseline-fetch", "SELECT id FROM users ORDER BY id FETCH FIRST 2 ROWS ONLY"),
            ("fetch-first-neg", "SELECT id FROM users ORDER BY id FETCH FIRST -1 ROWS ONLY"),
            ("fetch-next-neg",
                "SELECT id FROM users ORDER BY id OFFSET 0 ROWS FETCH NEXT -1 ROWS ONLY"),
            ("fetch-first-zero", "SELECT id FROM users ORDER BY id FETCH FIRST 0 ROWS ONLY"),
            ("offset-neg", "SELECT id FROM users ORDER BY id OFFSET -1 ROWS"),
            ("baseline-lag", "SELECT LAG(id, 1) OVER (ORDER BY id) FROM users"),
            ("lag-neg", "SELECT LAG(id, -1) OVER (ORDER BY id) FROM users"),
            ("lead-neg", "SELECT LEAD(id, -1) OVER (ORDER BY id) FROM users"),
            ("order-by-ordinal", "SELECT id, name FROM users ORDER BY 2"),
            ("order-by-2.5", "SELECT id, name FROM users ORDER BY 2.5"),
            ("order-by-neg", "SELECT id, name FROM users ORDER BY -1"),
            ("order-by-zero", "SELECT id, name FROM users ORDER BY 0"),
        ]);
    }

    [Fact]
    public void MergeProbe()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        Issue523Probe.Report(connection, [
            ("merge-baseline",
                "MERGE INTO users t USING (SELECT 1 id, 'x' name FROM dual) s ON (t.id = s.id) "
                    + "WHEN MATCHED THEN UPDATE SET t.name = t.name"),
            ("merge-two-when-matched",
                "MERGE INTO users t USING (SELECT 1 id, 'x' name FROM dual) s ON (t.id = s.id) "
                    + "WHEN MATCHED THEN UPDATE SET t.name = t.name "
                    + "WHEN MATCHED THEN DELETE"),
            ("merge-two-when-not-matched",
                "MERGE INTO users t USING (SELECT 999 id, 'x' name FROM dual) s ON (t.id = s.id) "
                    + "WHEN NOT MATCHED THEN INSERT (id, name) VALUES (s.id, s.name) "
                    + "WHEN NOT MATCHED THEN INSERT (id, name) VALUES (s.id, s.name)"),
            ("merge-update-then-delete-clause",
                "MERGE INTO users t USING (SELECT 1 id, 'x' name FROM dual) s ON (t.id = s.id) "
                    + "WHEN MATCHED THEN UPDATE SET t.name = t.name DELETE WHERE t.id = -1"),
        ]);
    }
}

[Trait("Engine", "SqlServer")]
public sealed class Issue523SqlServerProbeTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;

    public Issue523SqlServerProbeTests(SqlServerFixture fixture) => _fixture = fixture;

    [Fact]
    public void Probe()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        Issue523Probe.Report(connection, [
            ("baseline-top", "SELECT TOP (2) id FROM users"),
            ("top-zero", "SELECT TOP (0) id FROM users"),
            ("top-neg", "SELECT TOP (-1) id FROM users"),
            ("top-neg-bare", "SELECT TOP -1 id FROM users"),
            ("baseline-fetch",
                "SELECT id FROM users ORDER BY id OFFSET 0 ROWS FETCH NEXT 2 ROWS ONLY"),
            ("fetch-next-neg",
                "SELECT id FROM users ORDER BY id OFFSET 0 ROWS FETCH NEXT -1 ROWS ONLY"),
            ("fetch-next-zero",
                "SELECT id FROM users ORDER BY id OFFSET 0 ROWS FETCH NEXT 0 ROWS ONLY"),
            ("offset-neg", "SELECT id FROM users ORDER BY id OFFSET -1 ROWS"),
            ("baseline-lag", "SELECT LAG(id, 1) OVER (ORDER BY id) FROM users"),
            ("lag-neg", "SELECT LAG(id, -1) OVER (ORDER BY id) FROM users"),
            ("lead-neg", "SELECT LEAD(id, -1) OVER (ORDER BY id) FROM users"),
            ("order-by-ordinal", "SELECT id, name FROM users ORDER BY 2"),
            ("order-by-2.5", "SELECT id, name FROM users ORDER BY 2.5"),
            ("order-by-neg", "SELECT id, name FROM users ORDER BY -1"),
            ("order-by-zero", "SELECT id, name FROM users ORDER BY 0"),
        ]);
    }

    [Fact]
    public void MergeProbe()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        Issue523Probe.Report(connection, [
            ("merge-baseline",
                "MERGE INTO users AS t USING (SELECT 1 AS id, 'x' AS name) AS s ON t.id = s.id "
                    + "WHEN MATCHED THEN UPDATE SET name = t.name;"),
            ("merge-two-when-matched-conditioned",
                "MERGE INTO users AS t USING (SELECT 1 AS id, 'x' AS name) AS s ON t.id = s.id "
                    + "WHEN MATCHED AND t.age > 0 THEN UPDATE SET name = t.name "
                    + "WHEN MATCHED THEN DELETE;"),
            ("merge-three-when-matched",
                "MERGE INTO users AS t USING (SELECT 1 AS id, 'x' AS name) AS s ON t.id = s.id "
                    + "WHEN MATCHED AND t.age > 0 THEN UPDATE SET name = t.name "
                    + "WHEN MATCHED AND t.age > 1 THEN UPDATE SET name = t.name "
                    + "WHEN MATCHED THEN DELETE;"),
            ("merge-two-when-not-matched",
                "MERGE INTO users AS t USING (SELECT 999 AS id, 'x' AS name) AS s ON t.id = s.id "
                    + "WHEN NOT MATCHED AND s.id > 0 THEN INSERT (id, name) VALUES (s.id, s.name) "
                    + "WHEN NOT MATCHED THEN INSERT (id, name) VALUES (s.id, s.name);"),
            ("merge-three-when-not-matched-by-source",
                "MERGE INTO users AS t USING (SELECT 1 AS id, 'x' AS name) AS s ON t.id = s.id "
                    + "WHEN NOT MATCHED BY SOURCE AND t.age > 200 THEN UPDATE SET name = t.name "
                    + "WHEN NOT MATCHED BY SOURCE AND t.age > 201 THEN UPDATE SET name = t.name "
                    + "WHEN NOT MATCHED BY SOURCE AND t.age > 202 THEN DELETE;"),
        ]);
    }
}

[Trait("Engine", "PostgreSql")]
public sealed class Issue523PostgreSqlProbeTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public Issue523PostgreSqlProbeTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public void Probe()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        Issue523Probe.Report(connection, [
            ("fetch-first-neg", "SELECT id FROM users ORDER BY id FETCH FIRST -1 ROWS ONLY"),
            ("fetch-next-neg",
                "SELECT id FROM users ORDER BY id OFFSET 0 ROWS FETCH NEXT -1 ROWS ONLY"),
            ("lag-neg", "SELECT LAG(id, -1) OVER (ORDER BY id) FROM users"),
            ("lead-neg", "SELECT LEAD(id, -1) OVER (ORDER BY id) FROM users"),
        ]);
    }
}

[Trait("Engine", "MySql")]
public sealed class Issue523MySqlProbeTests : IClassFixture<MySqlFixture>
{
    private readonly MySqlFixture _fixture;

    public Issue523MySqlProbeTests(MySqlFixture fixture) => _fixture = fixture;

    [Fact]
    public void Probe()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        Issue523Probe.Report(connection, [
            ("limit-neg", "SELECT id FROM users ORDER BY id LIMIT -1"),
            ("lag-neg", "SELECT LAG(id, -1) OVER (ORDER BY id) FROM users"),
            ("lead-neg", "SELECT LEAD(id, -1) OVER (ORDER BY id) FROM users"),
            ("order-by-2.5", "SELECT id, name FROM users ORDER BY 2.5"),
        ]);
    }
}

[Trait("Engine", "Sqlite")]
public sealed class Issue523SqliteProbeTests : IClassFixture<SqliteFixture>
{
    private readonly SqliteFixture _fixture;

    public Issue523SqliteProbeTests(SqliteFixture fixture) => _fixture = fixture;

    [Fact]
    public void Probe()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        Issue523Probe.Report(connection, [
            ("limit-neg", "SELECT id FROM users ORDER BY id LIMIT -1"),
            ("lag-neg", "SELECT LAG(id, -1) OVER (ORDER BY id) FROM users"),
            ("lead-neg", "SELECT LEAD(id, -1) OVER (ORDER BY id) FROM users"),
            ("order-by-2.5", "SELECT id, name FROM users ORDER BY 2.5"),
        ]);
    }
}
