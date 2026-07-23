# Guide: Oracle Bulk Insert with SqlArtisan.BulkCopy

[← Back to README](https://github.com/h-tacayama/SqlArtisan/blob/main/README.md) ·
[Reference Home](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/README.md)

Oracle has no multi-row `VALUES`, so batch loads through the core builder run
row by row — fine for a handful of rows, a ceiling for thousands.
`SqlArtisan.BulkCopy` is the execution-layer companion for that case: it sends
every row in **one round trip** via ODP.NET array binding
(`OracleCommand.ArrayBindCount`), mapping your table class's columns to
same-named properties of your row objects. It builds no SQL through the query
builder — one fixed `INSERT INTO t (c1, c2) VALUES (:0, :1)` command carries an
array of values per column.

## Contents

- [1. Install the package](#1-install-the-package)
- [2. Map rows to a table class](#2-map-rows-to-a-table-class)
- [3. Insert](#3-insert)
- [Failure modes](#failure-modes)

## 1. Install the package

The package is pre-release, so pass `--prerelease`. It pulls
`Oracle.ManagedDataAccess.Core` (ODP.NET) transitively.

```bash
dotnet add package SqlArtisan.BulkCopy --prerelease
```

## 2. Map rows to a table class

`BulkInsert` inserts into the table named by a
[table class](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/guides/dapper-quickstart.md#2-create-table-classes)
— every public `DbColumn` property of the table class must have a same-named
public property on the row type:

```csharp
internal sealed class UsersTable : DbTableBase
{
    public UsersTable(string alias = "") : base("users", alias)
    {
        Id = new DbColumn(this, "id");
        Name = new DbColumn(this, "name");
        CreatedAt = new DbColumn(this, "created_at");
    }

    public DbColumn Id { get; }
    public DbColumn Name { get; }
    public DbColumn CreatedAt { get; }
}

internal sealed class UserRow
{
    public long Id { get; init; }
    public string? Name { get; init; }
    public DateTime? CreatedAt { get; init; }
}
```

Supported property types — `int`, `long`, `short`, `decimal`, `string`, and
`DateTime`, plus their nullable forms. A `null` value inserts SQL `NULL`.
Two Oracle-specific notes:

- Model a `NUMBER(1)` flag column as `int` / `int?` — Oracle has no SQL
  `BOOLEAN` column type, so `bool` properties are rejected.
- `DateTime` binds as `DATE` (whole-second precision — Oracle `DATE` has no
  sub-second component).

## 3. Insert

`BulkInsert` / `BulkInsertAsync` are extension methods on `OracleConnection`;
both return the number of rows inserted and accept an optional transaction:

```csharp
using SqlArtisan.BulkCopy;

UsersTable users = new();
UserRow[] rows = LoadRows(); // thousands of rows

using OracleConnection connection = new(connectionString);
connection.Open();
using OracleTransaction transaction = connection.BeginTransaction();

int inserted = connection.BulkInsert(users, rows, transaction);
// or: await connection.BulkInsertAsync(users, rows, transaction, cancellationToken);

transaction.Commit();
```

## Failure modes

Misuse fails loudly at the call, before anything reaches the database:

- An empty row set — `BulkInsert requires at least one row.`
- A table-class column with no matching row property — `BulkInsert requires
  the row type '...' to have a public property '...' matching table class
  '...'.`
- A property type outside the supported set — `BulkInsert cannot map property
  '...' of type ... to an OracleDbType; supported types are int, long, short,
  decimal, string, and DateTime, plus their nullable forms.`

`SqlArtisan.BulkCopy` is an optional companion package — the core stays a pure
query builder. The package is a host for provider-specific bulk paths; Oracle
array binding is the first, and other providers may join it.
