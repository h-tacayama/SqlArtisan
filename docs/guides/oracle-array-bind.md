# Guide: Oracle Array-Bind Execution with SqlArtisan.ArrayBind

[← Back to README](https://github.com/h-tacayama/SqlArtisan/blob/main/README.md) ·
[Reference Home](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/README.md)

Oracle has no multi-row `VALUES`, so batch loads through the core builder run
row by row — fine for a handful of rows, a ceiling for thousands.
`SqlArtisan.ArrayBind` is the execution-layer companion for that case: build N
statements of identical shape (typically one `INSERT`/`UPDATE`/`DELETE` per
row) the normal way, through the query builder, and it runs them together in
**one round trip** via ODP.NET array binding (`OracleCommand.ArrayBindCount`).
It builds no SQL itself — every statement comes from `SqlArtisan`.

## Contents

- [1. Install the package](#1-install-the-package)
- [2. Build one statement per row](#2-build-one-statement-per-row)
- [3. Execute](#3-execute)
- [Beyond INSERT](#beyond-insert)
- [Failure modes](#failure-modes)

## 1. Install the package

The package is pre-release, so pass `--prerelease`. It pulls
`Oracle.ManagedDataAccess.Core` (ODP.NET) transitively.

```bash
dotnet add package SqlArtisan.ArrayBind --prerelease
```

## 2. Build one statement per row

Build each row's statement through the ordinary fluent builder, keeping the
same columns and clause shape across rows — only the bound values differ.
Use `Sql.BindNull()` instead of a bare `null` literal for any value that can
be null: a bare `null` inlines the `NULL` keyword directly into the SQL text
(changing it row to row), while `BindNull()` reserves a real parameter, so a
null row binds the same marker a non-null row does.

```csharp
UsersTable users = new();
UserRow[] rows = LoadRows(); // thousands of rows

List<ISqlBuilder> statements = rows.Select(row =>
    InsertInto(users, users.Id, users.Name, users.CreatedAt)
        .Values(
            row.Id,
            row.Name is null ? BindNull() : row.Name,
            row.CreatedAt is null ? BindNull() : row.CreatedAt))
    .ToList();
```

Each bound position's `OracleDbType` is inferred from the first non-null
value at that position across the whole batch — supported types are `int`,
`long`, `short`, `decimal`, `string`, and `DateTime` (binds as `TIMESTAMP`; a
`DATE` column's own engine-side conversion truncates it to second precision).
If every row's value at a position is null, pass the type explicitly on at
least one row: `Sql.BindNull(DbType.Int32)`.

## 3. Execute

`ExecuteArrayBind` / `ExecuteArrayBindAsync` are extension methods on
`OracleConnection`; both return the number of rows affected and accept an
optional transaction:

```csharp
using SqlArtisan.ArrayBind;

using OracleConnection connection = new(connectionString);
connection.Open();
using OracleTransaction transaction = connection.BeginTransaction();

int inserted = connection.ExecuteArrayBind(statements, transaction);
// or: await connection.ExecuteArrayBindAsync(statements, transaction, cancellationToken);

transaction.Commit();
```

## Beyond INSERT

`ExecuteArrayBind` takes any `ISqlBuilder`, so the same call array-binds an
`UPDATE` or `DELETE` just as well — one statement per row, same shape:

```csharp
List<ISqlBuilder> statements = rows.Select(row =>
        Update(users).Set(users.Name == row.Name).Where(users.Id == row.Id))
    .ToList();

int updated = connection.ExecuteArrayBind(statements, transaction);
```

## Failure modes

Misuse fails loudly at the call, before anything reaches the database:

- An empty statement set — `ExecuteArrayBind requires at least one statement.`
- Statements that don't build identical SQL text — `ExecuteArrayBind requires
  every statement to build identical SQL text; statement at index ... differs
  from index 0.`
- A bound value's type outside the supported set — `ExecuteArrayBind cannot
  map bound value of type ... to an OracleDbType; supported types are int,
  long, short, decimal, string, and DateTime.`
- Every value at a position is null with no type hint — `ExecuteArrayBind
  cannot infer an OracleDbType for parameter :...; every bound value is null.
  Use Sql.BindNull(dbType) on at least one row to state the type explicitly.`

`SqlArtisan.ArrayBind` is an optional companion package — the core stays a
pure query builder. The package is a host for provider-specific array-bind
paths; Oracle is the first, and other providers may join it.
