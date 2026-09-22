using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Dapper;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;
using SqlArtisan.IntegrationTests.Schema;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Tests;

// TEMPORARY probe scaffolding for #523 item 2. Each lane reports, in one
// deliberately-failing assertion, whether the engine accepts or rejects every
// SQLA0102-candidate shape. Deleted once the facts are recorded as live twins.
public abstract class Issue523ProbeBase(IDatabaseFixture fixture)
{
    [Fact]
    public void Report()
    {
        var log = new StringBuilder();
        log.Append("\n#523 item 2 probe — ").Append(fixture.Dbms).Append('\n');

        foreach ((string Label, Func<ISqlBuilder> Build) probe in Probes())
        {
            log.Append(Run(probe.Label, probe.Build)).Append('\n');
        }

        Assert.Fail(log.ToString());
    }

    private static IEnumerable<(string Label, Func<ISqlBuilder> Build)> Probes()
    {
        yield return ("A joined DELETE, comma FROM", () =>
        {
            UsersTable u = new("u");
            OrdersTable o = new("o");
            return DeleteFrom(u).From(u, o).Where((u.Id == o.UserId) & (u.Id == 3));
        }
        );
        yield return ("B joined DELETE, FROM + INNER JOIN", () =>
        {
            UsersTable u = new("u");
            OrdersTable o = new("o");
            return DeleteFrom(u).From(u).InnerJoin(o).On(u.Id == o.UserId).Where(u.Id == 3);
        }
        );
        yield return ("C DELETE ... USING", () =>
        {
            UsersTable u = new("u");
            OrdersTable o = new("o");
            return DeleteFrom(u).Using(o).Where((u.Id == o.UserId) & (u.Id == 3));
        }
        );
        yield return ("D joined UPDATE, JOIN form", () =>
        {
            UsersTable u = new("u");
            OrdersTable o = new("o");
            return Update(u).InnerJoin(o).On(u.Id == o.UserId).Set(u.Age == 999);
        }
        );
        yield return ("E joined UPDATE, FROM form (target not re-listed)", () =>
        {
            UsersTable u = new("u");
            OrdersTable o = new("o");
            return Update(u).Set(u.Age == 999).From(o).Where((u.Id == o.UserId) & (u.Id == 3));
        }
        );
        yield return ("F FOR UPDATE after GROUP BY", () =>
        {
            UsersTable u = new();
            return (ISqlBuilder)Select(u.DepartmentId).From(u)
                .GroupBy(u.DepartmentId).OrderBy(u.DepartmentId).ForUpdate();
        }
        );
        yield return ("G FOR UPDATE, no GROUP BY (control)", () =>
        {
            UsersTable u = new();
            return (ISqlBuilder)Select(u.Id).From(u).OrderBy(u.Id).ForUpdate();
        }
        );
    }

    private string Run(string label, Func<ISqlBuilder> build)
    {
        string text;
        try
        {
            text = build().Build(fixture.Dbms).Text;
        }
        catch (Exception e)
        {
            return $"  {label}\n    BUILD-THROWS: {e.GetType().Name}: {e.Message}";
        }

        using IDbConnection connection = fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();
        try
        {
            connection.Execute(build(), transaction);
            return $"  {label}\n    SQL:      {text}\n    ACCEPTED";
        }
        catch (Exception e)
        {
            return $"  {label}\n    SQL:      {text}\n    REJECTED: {e.GetType().Name}: "
                + e.Message.Replace("\n", " ").Replace("\r", string.Empty);
        }
        finally
        {
            transaction.Rollback();
        }
    }
}

[Trait("Engine", "MySql")]
public sealed class MySqlIssue523ProbeTests(MySqlFixture fixture)
    : Issue523ProbeBase(fixture), IClassFixture<MySqlFixture>;

[Trait("Engine", "Oracle")]
public sealed class OracleIssue523ProbeTests(OracleFixture fixture)
    : Issue523ProbeBase(fixture), IClassFixture<OracleFixture>;

[Trait("Engine", "PostgreSql")]
public sealed class PostgreSqlIssue523ProbeTests(PostgreSqlFixture fixture)
    : Issue523ProbeBase(fixture), IClassFixture<PostgreSqlFixture>;

[Trait("Engine", "SqlServer")]
public sealed class SqlServerIssue523ProbeTests(SqlServerFixture fixture)
    : Issue523ProbeBase(fixture), IClassFixture<SqlServerFixture>;

[Trait("Engine", "Sqlite")]
public sealed class SqliteIssue523ProbeTests(SqliteFixture fixture)
    : Issue523ProbeBase(fixture), IClassFixture<SqliteFixture>;
