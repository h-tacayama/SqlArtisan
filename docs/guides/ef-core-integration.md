# Guide: Introducing SqlArtisan into an EF Core Project

[← Back to README](https://github.com/h-tacayama/SqlArtisan/blob/main/README.md) ·
[Reference Home](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/README.md)

Keep EF Core for CRUD and change tracking; build the hard queries — reporting,
dynamic search, batch DML — with SqlArtisan, and run them on the **same
connection and transaction** as the rest of your `DbContext` work. Nothing
about your EF model changes; SqlArtisan only produces a SQL string and its
bind parameters, and EF Core and Dapper both know how to run those.

Every snippet below is a working pattern (verified against EF Core 8 on
SQLite); swap in your provider's parameter type where noted.

## Contents

- [Setup](#setup)
- [The parameter bridge](#the-parameter-bridge)
- [Querying entities with FromSqlRaw](#querying-entities-with-fromsqlraw)
- [Querying plain values with SqlQueryRaw](#querying-plain-values-with-sqlqueryraw)
- [Dapper on the DbContext connection](#dapper-on-the-dbcontext-connection)
- [Which path to pick](#which-path-to-pick)

## Setup

```bash
dotnet add package SqlArtisan --prerelease
dotnet add package SqlArtisan.Dapper --prerelease   # optional: for the Dapper path below
```

Define a [table class](https://github.com/h-tacayama/SqlArtisan/blob/main/README.md#quick-start)
for each table you query through SqlArtisan, mirroring the **database** names
(the ones EF maps your entities to), not the C# entity names:

```csharp
using SqlArtisan;

internal sealed class UsersTable : DbTableBase
{
    public UsersTable(string alias = "") : base("users", alias)
    {
        Id = new DbColumn(this, "id");
        Name = new DbColumn(this, "name");
        Age = new DbColumn(this, "age");
    }

    public DbColumn Id { get; }
    public DbColumn Name { get; }
    public DbColumn Age { get; }
}
```

One C# note: in a file that also imports `System.Data.Common`, the name
`DbColumn` is ambiguous (that namespace has one too) — disambiguate with
`using DbColumn = SqlArtisan.DbColumn;`. Table classes in their own files
don't hit this.

## The parameter bridge

EF Core's raw-SQL APIs take `DbParameter` objects, so the one piece of glue
is a loop converting `sql.Parameters` into your provider's parameter type —
`NpgsqlParameter` here; `SqliteParameter`, `MySqlParameter`, `SqlParameter`,
`OracleParameter` are the same shape:

```csharp
using Npgsql;
using SqlArtisan;

internal static class SqlArtisanEfBridge
{
    public static NpgsqlParameter[] ToNpgsqlParameters(this SqlParameters parameters)
    {
        List<NpgsqlParameter> result = new();
        parameters.ForEach((name, bind) =>
        {
            NpgsqlParameter p = new(name, bind.Value ?? DBNull.Value);
            if (bind.DbType is not null) p.DbType = bind.DbType.Value;
            if (bind.Direction is not null) p.Direction = bind.Direction.Value;
            if (bind.Size is not null) p.Size = bind.Size.Value;
            result.Add(p);
        });
        return result.ToArray();
    }
}
```

The parameter names already carry the dialect's prefix (`:0`, `@0`, …) because
you built the statement for that dialect — the provider matches them to the
markers in `sql.Text` as-is.

## Querying entities with FromSqlRaw

`FromSqlRaw` turns the built statement into tracked entities, composable with
further LINQ like any EF query. Select every column of the mapped table
(`u.Asterisk` or the explicit list) so EF can materialize the entity:

```csharp
using Microsoft.EntityFrameworkCore;
using static SqlArtisan.Sql;

UsersTable u = new("u");
SqlStatement adults =
    Select(u.Asterisk)
    .From(u)
    .Where(u.Age >= 18)
    .OrderBy(u.Id)
    .Build(Dbms.PostgreSql);
// SELECT "u".* FROM users "u" WHERE "u".age >= :0 ORDER BY "u".id

List<User> users = ctx.Users
    .FromSqlRaw(adults.Text, adults.Parameters.ToNpgsqlParameters())
    .ToList();
```

## Querying plain values with SqlQueryRaw

For scalars and unmapped DTOs, `Database.SqlQueryRaw<T>` skips the entity
model. EF wraps your statement as a subquery and, for a primitive `T`, reads
the column named `Value` — alias the expression accordingly:

```csharp
UsersTable u = new("u");
SqlStatement count =
    Select(Count(Asterisk).As("Value"))
    .From(u)
    .Where(u.Age >= 18)
    .Build(Dbms.PostgreSql);

int adults = ctx.Database
    .SqlQueryRaw<int>(count.Text, count.Parameters.ToNpgsqlParameters())
    .Single();
```

(For a DTO `T`, alias each column to the property name instead.)

Because EF wraps the statement in `SELECT ... FROM (<your sql>)`, a trailing
`ORDER BY` without a row limit can be rejected on some engines — sort in the
outer LINQ, or keep the `ORDER BY` paired with `Limit`/`FetchNext`.

## Dapper on the DbContext connection

For DML and DTO queries, the lightest path is
[SqlArtisan.Dapper](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/guides/dapper-quickstart.md)
on the context's own connection — no bridge loop needed, and an EF
transaction is shared by passing its underlying `DbTransaction`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SqlArtisan.Dapper;
using static SqlArtisan.Sql;

using IDbContextTransaction tx = ctx.Database.BeginTransaction();
DbConnection cnn = ctx.Database.GetDbConnection();
DbTransaction dbTx = tx.GetDbTransaction();

UsersTable w = new();
int affected = cnn.Execute(
    Update(w).Set(w.Age == 18).Where(w.Id == 2),
    transaction: dbTx);

UsersTable r = new("u");
IEnumerable<string> names = cnn.Query<string>(
    Select(r.Name).From(r).Where(r.Age >= 18).OrderBy(r.Id),
    transaction: dbTx);

tx.Commit();
```

EF work and SqlArtisan work commit or roll back together, and the dialect is
detected from the connection automatically. Entities EF already tracks do
not see out-of-band updates until re-read — `ctx.ChangeTracker.Clear()` (or a
fresh context) after bulk DML keeps the tracker honest.

## Which path to pick

| You want | Use |
|----------|-----|
| Tracked entities, composable with LINQ | `FromSqlRaw` + the parameter bridge |
| Scalars / report DTOs through EF | `Database.SqlQueryRaw<T>` + the bridge |
| DTO queries and DML with the least glue | Dapper on `GetDbConnection()` |

All three run on the same connection; the Dapper path also shares
transactions with one extra argument. From here, the
[Query Cookbook](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/cookbook.md)
has the query shapes EF's LINQ provider struggles with — window functions,
recursive CTEs, per-dialect UPSERT and MERGE.
