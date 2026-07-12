# Guide: Dapper + SqlArtisan from Scratch

[← Back to README](https://github.com/h-tacayama/SqlArtisan/blob/main/README.md) ·
[Reference Home](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/README.md)

The greenfield path: an empty project to executed, type-safe queries in four
steps. `SqlArtisan.Dapper` detects the dialect from your connection and runs
the built statement through Dapper, so the only moving parts are your table
classes and the queries themselves.

## Contents

- [1. Install the packages](#1-install-the-packages)
- [2. Create table classes](#2-create-table-classes)
- [3. Execute queries](#3-execute-queries)
- [4. Write statements](#4-write-statements)
- [Pinning queries with tests](#pinning-queries-with-tests)

## 1. Install the packages

```bash
dotnet add package SqlArtisan --prerelease          # core query builder
dotnet add package SqlArtisan.Dapper --prerelease   # Dapper execution extensions
```

`SqlArtisan.Dapper` pulls in Dapper itself; your ADO.NET provider (Npgsql,
MySqlConnector, Microsoft.Data.SqlClient, …) is whatever you already use to
open connections.

## 2. Create table classes

A [table class](https://github.com/h-tacayama/SqlArtisan/blob/main/README.md#quick-start)
per database table gives every query IntelliSense and compile-checked
column names.

**Generate them** when the database already exists — the
`SqlArtisan.TableClassGen` tool connects to **Oracle** or **PostgreSQL** and
writes one class file per table:

```bash
dotnet tool install --global SqlArtisan.TableClassGen --prerelease
sa-tableclassgen    # interactive: connection info → namespace → output directory
```

**Write them by hand** on MySQL, SQLite, and SQL Server (or when you prefer
to) — each class is a constructor and one `DbColumn` per column:

```csharp
using SqlArtisan;

internal sealed class UsersTable : DbTableBase
{
    public UsersTable(string tableAlias = "") : base("users", tableAlias)
    {
        Id = new DbColumn(this, "id");
        Name = new DbColumn(this, "name");
        CreatedAt = new DbColumn(this, "created_at");
    }

    public DbColumn Id { get; }
    public DbColumn Name { get; }
    public DbColumn CreatedAt { get; }
}
```

For a one-off query you can skip the class and name a table inline with
`new DbTable("users", "u")` — but generated or hand-written classes are the
default; they are what makes wrong column names a compile error.

## 3. Execute queries

Add `using SqlArtisan.Dapper;` and call Dapper's verbs directly on your
`IDbConnection`, passing the builder chain instead of a SQL string. The
extension builds the statement for the connection's dialect (resolved from the
connection's provider type — Npgsql → PostgreSQL, MySqlConnector → MySQL, …)
and forwards text and bind parameters to Dapper:

```csharp
using SqlArtisan;
using SqlArtisan.Dapper;
using static SqlArtisan.Sql;

// Dapper maps snake_case columns to PascalCase members with this on:
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

UsersTable u = new("u");

IEnumerable<UserDto> users = await connection.QueryAsync<UserDto>(
    Select(u.Id, u.Name, u.CreatedAt)
    .From(u)
    .Where(u.Name.Like("A%"))
    .OrderBy(u.Id));

UsersTable w = new();
int affected = await connection.ExecuteAsync(
    Update(w).Set(w.Name == "renamed").Where(w.Id == 1));
```

The full Dapper verb set is mirrored (`Query`, `QueryFirst`, `QuerySingle`,
`Execute`, `ExecuteScalar`, `QueryMultiple`, `ExecuteReader`, and their
`...Async` twins), each taking the usual `transaction` / `commandTimeout`
arguments. For Oracle `RETURNING ... INTO`, `ExecuteReturningInto` returns the
parameter bag so the output values can be read back with `Get<T>`.

Two execution rules to know from day one:

- **A builder chain is single-use.** The Dapper call runs `Build()` for you,
  which finishes the chain — build a fresh query per call instead of holding
  one. See [Reusing a builder chain](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#reusing-a-builder-chain).
- **Unknown providers need one registration.** `DbmsResolver` knows the common
  connection types; a wrapper or exotic provider registers once at startup:
  `DbmsResolver.RegisterProvider("My.Provider.Connection", Dbms.PostgreSql)`.

Not using Dapper after all? Call `.Build(Dbms.X)` yourself and feed
`sql.Text` / `sql.Parameters` to raw ADO.NET or any other mapper — see the
[Quick Start](https://github.com/h-tacayama/SqlArtisan/blob/main/README.md#quick-start).

## 4. Write statements

From here it is the reference material:

- [Query Statements](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md) —
  every statement and clause, with the exact SQL each call emits.
- [Expressions](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md) and
  [Functions](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/functions.md) —
  conditions, operators, and the built-in function catalog.
- [Query Cookbook](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/cookbook.md) —
  realistic end-to-end queries (reporting, search screens, batch DML) to
  start from.

Write the SQL your target DBMS runs — SqlArtisan emits it faithfully and does
not translate between dialects, so pick the API for your engine as you go
(dialect notes in the reference flag the per-DBMS constructs).

## Pinning queries with tests

`Build()` is deterministic, so a plain unit test freezes a query's exact SQL
and parameters — cheap insurance that a refactor (or an AI edit) doesn't
silently change what runs:

```csharp
[Fact]
public void ActiveUsersQuery_CorrectSql()
{
    UsersTable u = new("u");
    SqlStatement sql =
        Select(u.Id, u.Name).From(u).Where(u.Id > 100).Build(Dbms.PostgreSql);

    Assert.Equal(
        "SELECT \"u\".id, \"u\".name FROM users \"u\" WHERE \"u\".id > :0",
        sql.Text);
    Assert.Equal(100, sql.Parameters.Get<int>(":0"));
}
```

The [AI coding assistants guide](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/guides/ai-assistants.md)
builds on this pattern, and the
[cookbook](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/cookbook.md)
pins all of its own recipes this way.
