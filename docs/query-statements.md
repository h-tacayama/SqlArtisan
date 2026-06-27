# Query Statements

[← Back to README](https://github.com/h-tacayama/SqlArtisan/blob/main/README.md) ·
[Reference Home](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/README.md)

> **How to read this reference.** Each entry shows the **C# you write** and the
> **SQL it emits**. The SQL in comments is line-wrapped for readability; `sql.Text`
> is a single line carrying the same tokens in the same order.
>
> **Design constraints (read before generating code):**
> - SqlArtisan emits the SQL you write; **cross-database portability is a non-goal**.
> - Pick the API for your target DBMS; a single call is not rewritten per dialect.
> - Dialect notes list databases in the order **MySQL, Oracle, PostgreSQL, SQLite, SQL Server**.

## Contents

- [SELECT](#select-query)
  - [SELECT Clause](#select-clause) · [FROM](#from-clause) · [WHERE](#where-clause) · [JOIN](#join-clause) · [ORDER BY](#order-by-clause) · [GROUP BY / HAVING](#group-by-and-having-clause) · [Set Operators](#set-operators) · [FOR UPDATE](#for-update-clause) · [Pagination](#pagination)
- [DELETE](#delete-statement)
- [UPDATE](#update-statement)
- [INSERT](#insert-statement)
  - [Standard](#standard-syntax) · [Multiple Rows](#multiple-rows) · [SET-like](#alternative-syntax-set-like) · [INSERT … SELECT](#insert-select-syntax) · [UPSERT](#upsert-insert-or-update) · [MERGE](#merge-statement) · [WITH / CTE](#with-clause-common-table-expressions)
- [RETURNING](#returning-clause)
  - [RETURNING INTO (Oracle)](#returning-into-oracle)

---

## SELECT Query

### SELECT Clause

#### Column Aliases
```csharp
UsersTable u = new();
SqlStatement sql =
    Select(
        u.Id.As("user_id"),
        u.Name.As("user_name"))
    .From(u)
    .Build();

// SELECT id "user_id",
// name "user_name"
// FROM users
```

#### DISTINCT
```csharp
UsersTable u = new();
SqlStatement sql =
    Select(Distinct, u.Id)
    .From(u)
    .Build();

// SELECT DISTINCT id
// FROM users
```

#### Hints
```csharp
// The hint below refers to this alias "u".
UsersTable u = new("u");
SqlStatement sql =
    Select(Hints("/*+ INDEX(u users_ix) */"), u.Id)
    .From(u)
    .Build();

// SELECT /*+ INDEX(u users_ix) */ "u".id
// FROM users "u"
```

---

### FROM Clause

#### FROM-less Queries
```csharp
SqlStatement sql =
    Select(CurrentTimestamp)
    .Build();

// SELECT CURRENT_TIMESTAMP
```

#### Using DUAL (Oracle)
```csharp
SqlStatement sql =
    Select(Sysdate)
    .From(Dual)
    .Build();

// SELECT SYSDATE FROM DUAL
```

---

### WHERE Clause

#### Example

```csharp
UsersTable u = new();
SqlStatement sql =
    Select(u.Name)
    .From(u)
    .Where(u.Id == 1)
    .Build();

// SELECT name
// FROM users
// WHERE id = :0
```

For a detailed guide on constructing various types of conditions (like **Logical**, **Comparison**, `NULL`, **Pattern Matching**, `BETWEEN`, `IN`, `EXISTS`), including how to use **Dynamic Conditions** (`ConditionIf`), check out the [Expressions: Conditions](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#conditions) section.

---

### JOIN Clause

#### Example using INNER JOIN
```csharp
UsersTable u = new("u");
OrdersTable o = new("o");
SqlStatement sql =
    Select(u.Name)
    .From(u)
    .InnerJoin(o)
    .On(u.Id == o.UserId)
    .Build();

// SELECT "u".name
// FROM users "u"
// INNER JOIN orders "o"
// ON "u".id = "o".user_id
```
#### Supported JOIN APIs

- `InnerJoin()` for `INNER JOIN`
- `LeftJoin()` for `LEFT JOIN`
- `RightJoin()` for `RIGHT JOIN`
- `FullJoin()` for `FULL JOIN`
- `CrossJoin()` for `CROSS JOIN`

#### Correlated joins: APPLY / LATERAL

To join a correlated derived table (per-group Top-N, lateral function expansion,
…), pass a subquery and a **derived-table handle**. Because `APPLY` and `LATERAL`
are genuinely different grammars — not one construct spelled two ways — each is
its own method emitting exactly what you write (no `Build(Dbms)` rewriting); pick
the one your target DBMS speaks:

The handle, a `DerivedTable`, names the derived table once and is reused to
reference its columns via `Column(...)` — by name, or type-safely from the
projected column / `.As(...)` alias (`x.Column(o.Id)`, `x.Column(total)`) — so no
alias strings are repeated:

```csharp
UsersTable u = new("u");
OrdersTable o = new("o");
DerivedTable x = new("x");

SqlStatement sql =
    Select(u.Name, x.Column("id"))
    .From(u)
    .CrossApply(
        Select(o.Id.As(x.Column("id"))).From(o).Where(o.UserId == u.Id),
        x)
    .Build(Dbms.SqlServer);

// SELECT "u".name, "x".id
// FROM users "u"
// CROSS APPLY (SELECT "o".id "id" FROM orders "o" WHERE "o".user_id = "u".id) "x"
```

When you reference the derived table's columns repeatedly, subclass
`DerivedTableBase` and expose them as typed `DbColumn` members (the same
pattern as a CTE's `CteBase`); pass that instance as the handle.

| Method | Emits | Typical DBMS |
|---|---|---|
| `CrossApply(subquery, handle)` | `CROSS APPLY (...) alias` | SQL Server, Oracle |
| `OuterApply(subquery, handle)` | `OUTER APPLY (...) alias` | SQL Server, Oracle |
| `CrossJoinLateral(subquery, handle)` | `CROSS JOIN LATERAL (...) alias` | PostgreSQL, MySQL |
| `LeftJoinLateral(subquery, handle)` | `LEFT JOIN LATERAL (...) alias ON TRUE` | PostgreSQL, MySQL |
| `JoinLateral(subquery, handle).On(cond)` | `JOIN LATERAL (...) alias ON cond` | PostgreSQL, MySQL |

The derived-table alias is alias-quoted at its definition (`... ) "x"`), matching
both how a CTE name is written and the alias-quoted column references through the
handle (`"x".id`), so the alias resolves consistently on every dialect — including
Oracle, where a bare alias would case-fold (`x` → `X`) and no longer match the
quoted reference.

The DBMS column lists where each form is idiomatic, not the limit of what is
emitted: availability is the target database's concern (and the opt-in
analyzer's). SQLite supports neither family; `LATERAL` has no SQL Server form;
and Oracle's correlated-derived-table join is `CROSS APPLY` / `OUTER APPLY`, so
prefer those over the `LATERAL` forms there (the injected `ON TRUE` in particular
relies on a boolean literal Oracle lacks before 23c). SqlArtisan emits the
construct faithfully rather than gating it at build time.

---

### ORDER BY Clause

```csharp
UsersTable u = new();
SqlStatement sql =
    Select(u.Name)
    .From(u)
    .OrderBy(
        1,
        u.Id,
        u.Id.NullsFirst,
        u.Id.NullsLast,
        u.Id.Asc,
        u.Id.Asc.NullsFirst,
        u.Id.Asc.NullsLast,
        u.Id.Desc,
        u.Id.Desc.NullsFirst,
        u.Id.Desc.NullsLast)
    .Build();

// SELECT name
// FROM users
// ORDER BY
// 1,
// id,
// id NULLS FIRST,
// id NULLS LAST,
// id ASC,
// id ASC NULLS FIRST,
// id ASC NULLS LAST,
// id DESC,
// id DESC NULLS FIRST,
// id DESC NULLS LAST
```

---

### GROUP BY and HAVING Clause

```csharp
UsersTable u = new();
SqlStatement sql =
    Select(
        u.Id,
        u.Name,
        Count(u.Id))
    .From(u)
    .GroupBy(u.Id, u.Name)
    .Having(Count(u.Id) > 1)
    .Build();

// SELECT id, name, COUNT(id)
// FROM users
// GROUP BY id, name
// HAVING COUNT(id) > :0
```

For subtotal/grand-total reports, `GroupBy(...)` also accepts the grouping extensions `Rollup(...)`, `Cube(...)`, and `GroupingSets(...)`. Wrap columns in `Group(...)` to form a composite grouping element — a multi-column set inside `GroupingSets(...)`, or a parenthesized composite column inside `Rollup(...)` / `Cube(...)` — and use `Group()` with no columns for the empty set (the grand total):

```csharp
SalesTable s = new();
SqlStatement sql =
    Select(s.Region, s.Product, Sum(s.Amount))
    .From(s)
    .GroupBy(Rollup(s.Region, s.Product))
    .Build();

// SELECT region, product, SUM(amount)
// FROM sales
// GROUP BY ROLLUP(region, product)

// Composite grouping element via Group(...):
//   .GroupBy(Rollup(Group(s.Region, s.Product), s.Channel))
//   => GROUP BY ROLLUP((region, product), channel)
//   .GroupBy(GroupingSets(Group(s.Region, s.Product), Group(s.Channel), Group()))
//   => GROUP BY GROUPING SETS((region, product), channel, ())
```

`Rollup(...)`, `Cube(...)`, and `GroupingSets(...)` always emit their standard function forms (`ROLLUP(...)`, `CUBE(...)`, `GROUPING SETS(...)`) on every dialect — PostgreSQL, Oracle, and SQL Server support all three. MySQL's own grouping syntax is instead the `WITH ROLLUP` suffix; chain `.WithRollup()` onto `GroupBy(...)` for it:

```csharp
// MySQL:
Select(s.Region, s.Product, Sum(s.Amount))
    .From(s)
    .GroupBy(s.Region, s.Product)
    .WithRollup()
    .Build(Dbms.MySql);
// GROUP BY region, product WITH ROLLUP
```

`Build(Dbms)` emits every form faithfully and does not police DBMS availability — an unsupported combination such as `Cube` / `GroupingSets` on MySQL, the function-form `Rollup(...)` on MySQL, or any extension on SQLite, is emitted as written, leaving it for the database (and the planned opt-in analyzer) to flag rather than silently rewriting the query.

---

### Set Operators

#### Example using UNION
```csharp
UsersTable u = new();
ArchivedUsersTable a = new();

SqlStatement sql =
    Select(u.Id, u.Name)
    .From(u)
    .Union
    .Select(a.Id, a.Name)
    .From(a)
    .Build();

// SELECT id, name
// FROM users
// UNION
// SELECT id, name
// FROM archived_users
```

#### Example using UNION ALL
```csharp
UsersTable u = new();
ArchivedUsersTable a = new();

SqlStatement sql =
    Select(u.Id, u.Name)
    .From(u)
    .UnionAll
    .Select(a.Id, a.Name)
    .From(a)
    .Build();

// SELECT id, name
// FROM users
// UNION ALL
// SELECT id, name
// FROM archived_users
```

#### Supported Set Operators APIs

- `Union` for `UNION`
- `UnionAll` for `UNION ALL`
- `Except` for `EXCEPT`
- `ExceptAll` for `EXCEPT ALL`
- `Minus` for `MINUS`
- `MinusAll` for `MINUS ALL`
- `Intersect` for `INTERSECT`
- `IntersectAll` for `INTERSECT ALL`

---

### FOR UPDATE Clause

#### Basic Example
```csharp
UsersTable u = new();
SqlStatement sql =
    Select(u.Name)
    .From(u)
    .Where(u.Id == 1)
    .ForUpdate()
    .Build();

// SELECT name
// FROM users
// WHERE id = :0
// FOR UPDATE
```

#### Example with Options
```csharp
UsersTable u = new("u");
OrdersTable o = new("o");
SqlStatement sql =
    Select(u.Id, o.Id)
    .From(u)
    .InnerJoin(o)
    .On(u.Id == o.UserId)
    .Where(u.Id == 1)
    .ForUpdate(Of(u.Id), Wait(5))
    .Build();

// SELECT "u".id, "o".id
// FROM users "u"
// INNER JOIN orders "o"
// ON "u".id = "o".user_id
// WHERE "u".id = :0
// FOR UPDATE OF "u".id WAIT 5
```

#### Supported Options
- `Of()` for `OF`
- `Nowait` for `NOWAIT`
- `SkipLocked` for `SKIP LOCKED`
- `Wait()` for `WAIT`

---

### Pagination

Row limiting is dialect-divergent, so SqlArtisan exposes two faithful families and you choose the one for your target database (see [Design Philosophy](https://github.com/h-tacayama/SqlArtisan/blob/main/README.md#design-philosophy)).

#### LIMIT family (PostgreSQL / MySQL / SQLite)

```csharp
TestTable t = new();
SqlStatement sql =
    Select(t.Code)
    .From(t)
    .OrderBy(t.Code)
    .Limit(10)
    .Offset(20)
    .Build();

// SELECT code
// FROM test_table
// ORDER BY code
// LIMIT :0 OFFSET :1
```

`Limit()` and `Offset()` can be combined as `LIMIT n OFFSET m`. Note that a standalone `Offset()` (without `Limit()`) is only valid on PostgreSQL; MySQL and SQLite require `OFFSET` to be paired with `LIMIT`.

#### OFFSET/FETCH family (Oracle 12c+ / SQL Server 2012+)

```csharp
TestTable t = new();
SqlStatement sql =
    Select(t.Code)
    .From(t)
    .OrderBy(t.Code)
    .OffsetRows(20)
    .FetchNext(10)
    .Build(Dbms.Oracle);

// SELECT code
// FROM test_table
// ORDER BY code
// OFFSET :0 ROWS FETCH NEXT :1 ROWS ONLY
```

- Use `OffsetRows(m)` alone for `OFFSET m ROWS`.
- Use `FetchFirst(n)` for `FETCH FIRST n ROWS ONLY` (no offset). This standalone form is valid on **Oracle** (and PostgreSQL); **SQL Server requires an `OFFSET`**, so on SQL Server use `OffsetRows(0).FetchNext(n)` instead.
- This clause requires `ORDER BY` on SQL Server, and is available on Oracle 12c+ / SQL Server 2012+.

The row counts are parameterized like other literals, so the bind-parameter prefix follows the target dialect (`:` / `@` / `?`).

---

## DELETE Statement
```csharp
UsersTable u = new();
SqlStatement sql =
    DeleteFrom(u)
    .Where(u.Id == 1)
    .Build();

// DELETE FROM users
// WHERE id = :0
```

---

## UPDATE Statement
```csharp
UsersTable u = new();
SqlStatement sql =
    Update(u)
    .Set(
        u.Name == "newName",
        u.CreatedAt == CurrentTimestamp)
    .Where(u.Id == 1)
    .Build();

// UPDATE users
// SET name = :0,
// created_at = CURRENT_TIMESTAMP
// WHERE id = :1
```

**Note:** SqlArtisan's `Set()` method uses `Column == Value` for SQL-like assignment, unlike standard C# `==` (comparison). In `Where()` clauses, `==` is used for comparison as expected.

---

## INSERT Statement

### Standard Syntax

```csharp
UsersTable u = new();
SqlStatement sql =
    InsertInto(u, u.Id, u.Name, u.CreatedAt)
    .Values(1, "newName", CurrentTimestamp)
    .Build();

// INSERT INTO users
// (id, name, created_at)
// VALUES
// (:0, :1, CURRENT_TIMESTAMP)
```

---

### Multiple Rows

Chain `Values()` to insert multiple rows in a single statement.

```csharp
UsersTable u = new();
SqlStatement sql =
    InsertInto(u, u.Id, u.Name)
    .Values(1, "Alice")
    .Values(2, "Bob")
    .Values(3, "Carol")
    .Build();

// INSERT INTO users
// (id, name)
// VALUES
// (:0, :1), (:2, :3), (:4, :5)
```

**Note:** Multi-row `VALUES` is supported by PostgreSQL, MySQL, SQLite, and SQL Server (2008+). Oracle does not support multi-row `VALUES`; it uses `INSERT ALL` instead.

---

### Alternative Syntax (SET-like)

SqlArtisan also offers an alternative `INSERT` syntax, similar to `UPDATE`'s `Set()` method, for clearer column-value pairing.

```csharp
UsersTable u = new();
SqlStatement sql =
    InsertInto(u)
    .Set(
        u.Id == 1,
        u.Name == "newName",
        u.CreatedAt == CurrentTimestamp)
    .Build();

// INSERT INTO users
// (id, name, created_at)
// VALUES
// (:0, :1, CURRENT_TIMESTAMP)
```

**Note:** Generates standard `INSERT INTO ... (columns) VALUES (values)` SQL, not MySQL's `INSERT ... SET ...`, for broad database compatibility.

---

### INSERT SELECT Syntax

```csharp
UsersTable u = new();
ArchivedUsersTable a = new();

SqlStatement sql =
    InsertInto(a, a.Id, a.Name, a.CreatedAt)
    .Select(u.Id, u.Name, u.CreatedAt)
    .From(u)
    .Build();

// INSERT INTO archived_users
// (id, name, created_at)
// SELECT id, name, created_at
// FROM users
```

---

### UPSERT (Insert or Update)

SqlArtisan exposes UPSERT through **per-dialect methods** rather than a single
rewritten abstraction — the SQL you pick is the SQL that runs.

**PostgreSQL / SQLite — `ON CONFLICT`**

```csharp
UsersTable u = new();
SqlStatement sql =
    InsertInto(u, u.Id, u.Name)
    .Values(1, "newName")
    .OnConflict(u.Id)
    .DoUpdateSet(u.Name == Excluded(u.Name))
    .Build(Dbms.PostgreSql);

// INSERT INTO users (id, name)
// VALUES (:0, :1)
// ON CONFLICT (id)
// DO UPDATE SET name = EXCLUDED.name
```

`Excluded(column)` references the row proposed for insertion. It is resolved by
the dialect: `EXCLUDED` on PostgreSQL, lowercase `excluded` on SQLite, and the
`new` row alias on MySQL. A conditional update is expressed with `Where(...)`,
and `DoNothing()` skips conflicting rows:

```csharp
// ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name WHERE ...
.OnConflict(u.Id).DoUpdateSet(u.Name == Excluded(u.Name)).Where(u.Id < 100)

// ON CONFLICT (id) DO NOTHING
.OnConflict(u.Id).DoNothing()

// ON CONFLICT DO NOTHING   (no explicit conflict target)
.OnConflict().DoNothing()
```

**MySQL — `ON DUPLICATE KEY UPDATE`**

```csharp
UsersTable u = new();
SqlStatement sql =
    InsertInto(u, u.Id, u.Name)
    .Values(1, "newName")
    .OnDuplicateKeyUpdate(u.Name == Excluded(u.Name))
    .Build(Dbms.MySql);

// INSERT INTO users (id, name)
// VALUES (?0, ?1)
// AS new
// ON DUPLICATE KEY UPDATE name = new.name
```

MySQL keys off any unique index implicitly (no conflict target). SqlArtisan
emits the 8.0.19+ row-alias form (`AS new` … `new.column`) to avoid the
deprecated `VALUES()` function.

**Note:** `ON CONFLICT` is PostgreSQL/SQLite-only and `ON DUPLICATE KEY UPDATE`
is MySQL-only. Oracle and SQL Server use [`MERGE`](#merge-statement) instead.
SqlArtisan does not validate feature support, so ensure the clause is valid for
your target DBMS.

---

### MERGE Statement

`MERGE` is the native UPSERT path for **Oracle** and **SQL Server** (and
**PostgreSQL 15+**), which have no `ON CONFLICT` / `ON DUPLICATE KEY UPDATE`.
Start with `MergeInto(target)`, name the data source with `Using(...)`, match
rows with `On(...)`, then add one or more `WhenMatched` / `WhenNotMatched`
branches. As elsewhere, the SQL you write is the SQL that runs — the branches
are per-dialect and SqlArtisan does not rewrite them.

```csharp
UsersTable t = new("t");   // target, aliased
UsersTable s = new("s");   // source, aliased
UsersTable c = new();      // unaliased, for the INSERT column list

SqlStatement sql =
    MergeInto(t)
    .Using(s)
    .On(t.Id == s.Id)
    .WhenMatched().ThenUpdateSet(t.Name == s.Name)
    .WhenNotMatched().ThenInsert(c.Id, c.Name).Values(s.Id, s.Name)
    .Build(Dbms.SqlServer);

// MERGE INTO users "t"
// USING users "s"
// ON ("t".id = "s".id)
// WHEN MATCHED THEN UPDATE SET "t".name = "s".name
// WHEN NOT MATCHED THEN INSERT (id, name) VALUES ("s".id, "s".name);
```

The `INSERT` column list names target columns and must **not** be
alias-qualified (pass columns from an unaliased table instance, as `c` above).

**Per-dialect branches and pitfalls:**

```csharp
// WHEN MATCHED AND <cond> THEN ...   (filtered branch)
.WhenMatched(s.Status == "active").ThenUpdateSet(t.Name == s.Name)

// Oracle in-clause DELETE: WHEN MATCHED THEN UPDATE SET ... DELETE WHERE ...
.WhenMatched().ThenUpdateSet(t.Name == s.Name).DeleteWhere(t.Name.IsNull)

// SQL Server only: WHEN MATCHED THEN DELETE
.WhenMatched().ThenDelete()

// SQL Server only: WHEN NOT MATCHED BY SOURCE THEN UPDATE/DELETE
.WhenNotMatchedBySource().ThenUpdateSet(t.Name == "archived")
.WhenNotMatchedBySource(t.Name.IsNull).ThenDelete()
```

> [!IMPORTANT]
> **SQL Server `MERGE` caveats.** SqlArtisan appends the **required terminating
> semicolon** automatically when you `Build(Dbms.SqlServer)` (other dialects omit
> it). `MERGE` performs its `INSERT`/`UPDATE`/`DELETE` actions independently, so
> for concurrency safety you should take a serializable lock on the target —
> `MERGE target WITH (HOLDLOCK) ...` (add the hint to your target table source;
> SqlArtisan does not inject it). SQL Server's `MERGE` also has a history of
> bugs and surprising behavior; Microsoft and the community recommend caution,
> especially with a `DELETE` action or temporal tables. Prefer separate
> statements if you do not need atomic upsert semantics.

---

### WITH Clause (Common Table Expressions)

For a one-off CTE you don't want to declare a typed class for, use the
`Cte` and read its columns by name with `Column(name)`:

```csharp
Cte seniors = new("seniors");
SqlStatement sql =
    With(seniors.As(Select(users.Id.As(seniors.Column("id"))).From(users).Where(users.Age > 40)))
    .Select(seniors.Column("id"))
    .From(seniors)
    .Build();
```

When you reference a CTE's columns repeatedly, declare a typed `CteBase`
subclass instead:

1. Define your CTE class
```csharp
internal sealed class SeniorUsersCte : CteBase
{
    public SeniorUsersCte(string name) : base(name)
    {
        SeniorId = new DbColumn(name, "senior_id");
        SeniorName = new DbColumn(name, "senior_name");
        SeniorAge = new DbColumn(name, "senior_age");
    }

    public DbColumn SeniorId { get; }
    public DbColumn SeniorName { get; }
    public DbColumn SeniorAge { get; }
}
```

2. Build the Query

```csharp
UsersTable users = new("users");
SeniorUsersCte seniors = new("seniors");
OrdersTable orders = new("orders");

SqlStatement sql =
    With(
        seniors.As(
            Select(
                users.Id.As(seniors.SeniorId),
                users.Name.As(seniors.SeniorName),
                users.Age.As(seniors.SeniorAge))
            .From(users)
            .Where(users.Age > 40)))
    .Select(
        orders.Id,
        orders.OrderDate,
        seniors.SeniorId,
        seniors.SeniorName,
        seniors.SeniorAge
    )
    .From(orders)
    .InnerJoin(seniors)
    .On(orders.UserId == seniors.SeniorId)
    .Build();

// WITH "seniors" AS
// (SELECT "users".id "senior_id",
// "users".name "senior_name",
// "users".age "senior_age"
// FROM users "users" WHERE "users".age > :0)
// SELECT "orders".id,
// "orders".order_date,
// "seniors".senior_id,
// "seniors".senior_name,
// "seniors".senior_age
// FROM orders "orders"
// INNER JOIN "seniors"
// ON "orders".user_id = "seniors".senior_id
```

SqlArtisan also supports more advanced WITH clause scenarios, including:

- Recursive CTEs using the `WithRecursive()` method.
- CTEs with DML statements (`INSERT`, `UPDATE`, and `DELETE`).

---

## RETURNING Clause

The `Returning()` method appends a `RETURNING` clause to `INSERT`, `UPDATE`, and `DELETE` statements, letting you read back the affected rows.

```csharp
UsersTable u = new();
SqlStatement sql =
    DeleteFrom(u)
    .Where(u.Id == 1)
    .Returning(u.Id, u.Name)
    .Build();

// DELETE FROM users
// WHERE id = :0
// RETURNING id, name
```

It is also available on `INSERT` and `UPDATE`:

```csharp
UsersTable u = new();
SqlStatement sql =
    Update(u)
    .Set(u.Name == "newName")
    .Where(u.Id == 1)
    .Returning(u.Id, u.Name)
    .Build();

// UPDATE users
// SET name = :0
// WHERE id = :1
// RETURNING id, name
```

### RETURNING INTO (Oracle)

For Oracle, chain `Into()` after `Returning()` to bind the returned columns into output parameters. The number of variables must match the number of returned expressions, and each variable name must be unique. The output parameters are registered with `ParameterDirection.Output` so you can read their values after execution.

```csharp
UsersTable u = new();
SqlStatement sql =
    DeleteFrom(u)
    .Where(u.Id == 1)
    .Returning(u.Id, u.Name)
    .Into("outId", "outName")
    .Build(Dbms.Oracle);

// DELETE FROM users
// WHERE id = :0
// RETURNING id, name
// INTO :outId, :outName
```

**Note:** `RETURNING` is supported by Oracle, PostgreSQL, and SQLite (3.35+). It is not supported by SQL Server (which uses `OUTPUT`) or MySQL. The `RETURNING ... INTO` form is Oracle-specific. SqlArtisan does not validate database feature support, so ensure the clause is valid for your target DBMS.
