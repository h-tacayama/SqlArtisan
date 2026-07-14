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
> - A builder chain is **single-use**: `Build()` finishes it, including when a Dapper call runs `Build()` internally — see [Reusing a builder chain](#reusing-a-builder-chain).

## Contents

- [SELECT](#select-query)
  - [SELECT Clause](#select-clause) · [FROM](#from-clause) · [WHERE](#where-clause) · [JOIN](#join-clause) · [ORDER BY](#order-by-clause) · [GROUP BY / HAVING](#group-by-and-having-clause) · [Set Operators](#set-operators) · [FOR UPDATE](#for-update-clause) · [Pagination](#pagination)
- [DELETE](#delete-statement)
- [UPDATE](#update-statement)
  - [Correlated UPDATE / DELETE](#correlated-update--delete)
- [INSERT](#insert-statement)
  - [Standard](#standard-syntax) · [Multiple Rows](#multiple-rows) · [SET-like](#alternative-syntax-set-like) · [INSERT … SELECT](#insert-select-syntax) · [UPSERT](#upsert-insert-update-or-skip) · [MERGE](#merge-statement) · [WITH / CTE](#with-clause-common-table-expressions)
- [RETURNING](#returning-clause)
  - [RETURNING INTO (Oracle)](#returning-into-oracle)

---

## SELECT Query

### SELECT Clause

#### SELECT * and Qualified Star
`Asterisk` is the bare `*` select item; a table's `.Asterisk` is the qualified star (`alias.*`, or `table.*` when the table has no alias) — every column of that one relation, handy beside other select items in a join. It works the same way on a CTE or derived table (`cte.Asterisk` → `"cte".*`), which are always quoted like an alias. Both stars are universal syntax, with one mixing rule: on Oracle a bare `*` must be the only select item — beside other items, use the table's `.Asterisk` (`t.*, id`) as in the join example below.
```csharp
UsersTable u = new();
SqlStatement sql =
    Select(Asterisk)
    .From(u)
    .Build();

// SELECT * FROM users
```
```csharp
UsersTable u = new("u");
OrdersTable o = new("o");
SqlStatement sql =
    Select(u.Asterisk, o.Amount)
    .From(u)
    .InnerJoin(o).On(o.UserId == u.Id)
    .Build();

// SELECT "u".*, "o".amount
// FROM users "u"
// INNER JOIN orders "o" ON "o".user_id = "u".id
```
Both markers are valid only in a `SELECT` or `RETURNING` list, with one exception: `Count(Asterisk)` emits `COUNT(*)` — the only aggregate where `*` is legal. Any other expression position (`Upper(...)`, `ORDER BY`, …) throws or does not compile.

Do **not** write `Select("*")` — a string is always a bind value, never SQL, so it emits `SELECT :0` returning the literal `'*'` per row. The same rule protects every string you bind from injection; `Asterisk` is the SQL spelling. For `EXISTS (SELECT * ...)`, prefer the equivalent `Exists(Select(1)...)` idiom — see [Conditions](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#conditions).

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

#### DISTINCT ON (PostgreSQL)
Keeps one row per distinct combination of the listed expressions — the first per the query's `ORDER BY`. Pass `DistinctOn(...)` as the select prefix.
```csharp
UsersTable u = new("u");
SqlStatement sql =
    Select(DistinctOn(u.DepartmentId), u.DepartmentId, u.Name)
    .From(u)
    .OrderBy(u.DepartmentId, u.Salary.Desc)
    .Build();

// SELECT DISTINCT ON ("u".department_id) "u".department_id, "u".name
// FROM users "u"
// ORDER BY "u".department_id, "u".salary DESC
```
PostgreSQL syntax; emitted faithfully on every dialect, with availability left to the database.

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

#### Ad-hoc Tables

For a one-off query you don't want to declare a typed table class (a `DbTableBase` subclass) for, name a table inline with `DbTable` and read its columns by name with `Column(name)`:

```csharp
DbTable u = new("users", "u");
SqlStatement sql =
    Select(u.Column("id"), u.Column("name"))
    .From(u)
    .Where(u.Column("id") > 0)
    .Build();

// SELECT "u".id, "u".name FROM users "u" WHERE "u".id > :0
```

Columns are qualified by the alias — or rendered unqualified when the table has no alias (`new DbTable("users")` → `id`). `DbTable` works anywhere a table class does, including `INSERT` / `UPDATE` / `DELETE`. For columns referenced repeatedly, or for IntelliSense on column names, declare a typed `DbTableBase` subclass (or generate one with SqlArtisan.TableClassGen) instead.

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
MySQL also accepts `FROM DUAL` (the `SYSDATE` shown here is Oracle syntax).

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
- `NaturalJoin()` for `NATURAL JOIN`, and `NaturalLeftJoin()` / `NaturalRightJoin()` / `NaturalFullJoin()` for the outer forms

A `NATURAL` join takes no `On(...)` — it matches on **every** column name the two
tables share, so adding or renaming a column on either side silently changes the
join; reach for it only when that coupling is acceptable. On SQL Server, which
has no `NATURAL JOIN`, write the match explicitly with `On(...)`; on MySQL, which
has no `FULL JOIN` at all, `NaturalFullJoin()` is unsupported too — emulate it
with `LeftJoin(...).On(...)` unioned with `RightJoin(...).On(...)` there.

#### JOIN ... USING

`.Using(column, ...)` follows `InnerJoin()` / `LeftJoin()` / `RightJoin()` /
`FullJoin()` in place of `.On(...)`, matching where the named columns are equal
(they must exist, unqualified, on both sides):

```csharp
DbTable u = new("users", "u");
DbTable o = new("orders", "o");

SqlStatement sql =
    Select(u.Column("name"))
    .From(u)
    .InnerJoin(o)
    .Using(u.Column("user_id"))
    .Build();

// SELECT "u".name
// FROM users "u"
// INNER JOIN orders "o"
// USING (user_id)
```

MySQL, Oracle, PostgreSQL, and SQLite support this (standard SQL); on SQL Server,
which has no `JOIN ... USING`, write the equivalent `On(...)` predicate.

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
// CROSS APPLY (SELECT "o".id id FROM orders "o" WHERE "o".user_id = "u".id) "x"
```

When you reference the derived table's columns repeatedly, subclass
`DerivedTableBase` and expose them as typed `DbColumn` members (the same
pattern as a CTE's `CteBase`); pass that instance as the handle.

| Method | Emits | Typical DBMS |
|---|---|---|
| `CrossApply(subquery, handle)` | `CROSS APPLY (...) alias` | SQL Server, Oracle |
| `OuterApply(subquery, handle)` | `OUTER APPLY (...) alias` | SQL Server, Oracle |
| `CrossJoinLateral(subquery, handle)` | `CROSS JOIN LATERAL (...) alias` | MySQL, Oracle 12c+, PostgreSQL |
| `LeftJoinLateral(subquery, handle)` | `LEFT JOIN LATERAL (...) alias ON TRUE` | MySQL, PostgreSQL |
| `JoinLateral(subquery, handle).On(cond)` | `JOIN LATERAL (...) alias ON cond` | MySQL, Oracle 12c+, PostgreSQL |

The derived-table alias is alias-quoted at its definition (`... ) "x"`), matching
both how a CTE name is written and the alias-quoted column references through the
handle (`"x".id`), so the alias resolves consistently on every dialect — including
Oracle, where a bare alias would case-fold (`x` → `X`) and no longer match the
quoted reference.

The DBMS column lists where each form is idiomatic, not the limit of what is
emitted: availability is the target database's concern (and the opt-in
analyzer's). SQLite supports neither family, and `LATERAL` has no SQL Server
form. Oracle (12c+) accepts both families except `LeftJoinLateral` — its
injected `ON TRUE` relies on a boolean literal Oracle lacks before 23c; use
`OuterApply` there. SqlArtisan emits the construct faithfully rather than
gating it at build time.

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

To group by an expression that carries bind parameters — a `CASE` label, a `DECODE`, date math with a parameter — hold the expression in a variable and pass the **same instance** to `Select(...)` and `GroupBy(...)`; both occurrences then emit identical parameter markers and the statement runs everywhere. The two clauses need not match otherwise — here the `SELECT` side aliases the instance and adds an aggregate:

```csharp
UsersTable u = new();
SqlExpression label =
    Case(u.DepartmentId, When(10).Then("East"), Else("Other"));
SqlStatement sql =
    Select(label.As("region"), Count(Asterisk))
    .From(u)
    .GroupBy(label)
    .Build();

// SELECT CASE department_id WHEN :0 THEN :1 ELSE :2 END "region", COUNT(*)
// FROM users
// GROUP BY CASE department_id WHEN :0 THEN :1 ELSE :2 END
```

Building the expression twice instead binds separate parameters per occurrence (`GROUP BY ... WHEN :3 ...`), and Oracle, PostgreSQL, and SQL Server reject the statement — they match `GROUP BY` expressions syntactically and cannot prove the two parameter sets equal (ORA-00979 / 42803 / Msg 8120; MySQL and SQLite accept it). Two alternatives avoid the repetition: project the expression in a CTE and group over the projected column, or — if you'd rather keep the expression's shape written out in each clause — hoist only the *values* with `Sql.Bind(value)` (`BindValue p10 = Bind(10);`) and pass the same handle to both occurrences; `Bind(...)` is otherwise just an explicit form of the parameterization every literal already gets automatically.

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

`Rollup(...)`, `Cube(...)`, and `GroupingSets(...)` always emit their standard function forms (`ROLLUP(...)`, `CUBE(...)`, `GROUPING SETS(...)`) on every dialect — Oracle, PostgreSQL, and SQL Server support all three. MySQL's own grouping syntax is instead the `WITH ROLLUP` suffix; chain `.WithRollup()` onto `GroupBy(...)` for it:

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

A subtotal row from `Rollup(...)` / `Cube(...)` / `GroupingSets(...)` has `NULL` in the aggregated-away column — indistinguishable from a genuine `NULL` data value without `Grouping(...)`, which returns `1` for a subtotal row and `0` for a data row. Use it to label subtotal rows, typically via a `Case`:

```csharp
SqlStatement sql =
    Select(
        s.Region,
        Sum(s.Amount),
        Case(When(Grouping(s.Region) == 1).Then("All Regions"), Else(s.Region)))
    .From(s)
    .GroupBy(Rollup(s.Region))
    .Build();

// SELECT region, SUM(amount), CASE WHEN (GROUPING(region) = :0) THEN :1 ELSE region END
// FROM sales
// GROUP BY ROLLUP(region)
```

`Grouping(a, b, ...)` also takes multiple columns, returning a combined bitmask (MySQL, PostgreSQL); Oracle and SQL Server spell the same bitmask `GroupingId(a, ...)` instead. MySQL accepts `Grouping(...)` only inside a `WITH ROLLUP` query (its own grouping syntax, above) — using it with an ordinary `GroupBy(...)` is rejected there, a context requirement no dialect matrix key can express.

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

#### OFFSET/FETCH family (Oracle 12c+ / PostgreSQL / SQL Server 2012+)

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

#### Row-limited queries as subqueries

A query ending in a row-limiting clause is still a subquery: embed it as an aliased scalar value (`.As("alias")`), an `IN` / `NOT IN` / `EXISTS` operand, a CTE body, or a `LATERAL` / `APPLY` operand. The canonical per-group top-N:

```csharp
UsersTable u = new("u");
OrdersTable o = new("o");
DerivedTable x = new("x");

SqlStatement sql =
    Select(u.Name, x.Column("amount"))
    .From(u)
    .CrossJoinLateral(
        Select(o.Amount.As(x.Column("amount")))
            .From(o)
            .Where(o.UserId == u.Id)
            .OrderBy(o.Amount)
            .Limit(3),
        x)
    .Build();

// SELECT "u".name, "x".amount
// FROM users "u"
// CROSS JOIN LATERAL (SELECT "o".amount amount FROM orders "o"
// WHERE "o".user_id = "u".id ORDER BY "o".amount LIMIT :0) "x"
```

On MySQL, `LIMIT` directly inside an `IN` / `ALL` / `ANY` / `SOME` subquery is rejected ("This version of MySQL doesn't yet support 'LIMIT & IN/ALL/ANY/SOME subquery'") — route the limited query through a CTE (`With(c.As(...))`) and select from that instead. Every other subquery position — scalar, `EXISTS`, CTE body, `LATERAL` — accepts `LIMIT` on MySQL.

#### Reusing a builder chain

A builder chain is **single-use**: once you call `Build()`, that builder is finished — any further call on it, including a second `Build()`, throws. This turns a silently-wrong reuse into a loud error, so a held chain can't leak one page's clauses into another:

```csharp
// NG — holding a base query to fetch each page throws on the second Build().
TestTable t = new();
var q = Select(t.Code).From(t).OrderBy(t.Code).Limit(10);

SqlStatement page1 = q.Offset(0).Build();
SqlStatement page2 = q.Offset(10).Build();  // throws:
// This SELECT statement was already built; start a new chain.
```

Rebuild the chain per call instead — a local function parameterized by the part that changes:

```csharp
// OK — each call builds a fresh chain.
SqlStatement Page(int offset) =>
    Select(t.Code).From(t).OrderBy(t.Code).Limit(10).Offset(offset).Build();

SqlStatement page1 = Page(0);
SqlStatement page2 = Page(10);
```

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

**Dialect note:** On SQL Server the `DELETE` target cannot be aliased — pass an unaliased table (`DeleteFrom(new UsersTable())`), since T-SQL introduces the alias through a `FROM` clause instead; building an aliased target for SQL Server throws. MySQL, Oracle, PostgreSQL, and SQLite accept an aliased target. When the statement contains a subquery that references the target, the target **must** be aliased — see [Correlated UPDATE / DELETE](#correlated-update--delete).

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

**Dialect note:** As with `DELETE`, on SQL Server the `UPDATE` target cannot be aliased — pass an unaliased table; building an aliased target for SQL Server throws. MySQL, Oracle, PostgreSQL, and SQLite accept an aliased target.

### Correlated UPDATE / DELETE

A target-table column referenced inside a subquery renders bare when the
target has no alias, and every engine resolves it to the subquery's own
table — a tautology that silently updates or deletes **every row**. SqlArtisan
refuses to build that form; `Build()` throws:

> The target of a correlated UPDATE or DELETE must be aliased.

Alias the target — the outer reference then renders qualified and the
statement means what it says:

```csharp
UsersTable u = new("u");
OrdersTable o = new("o");
SqlStatement sql =
    DeleteFrom(u)
    .Where(NotExists(
        Select(o.Id)
        .From(o)
        .Where(o.UserId == u.Id)))
    .Build();

// DELETE FROM users AS "u"
// WHERE NOT EXISTS
// (SELECT "o".id FROM orders "o" WHERE "o".user_id = "u".id)
```

To deliberately re-select from the target table in an uncorrelated subquery,
give the inner scope its own instance — one C# instance cannot stand for two
SQL scopes, so reusing the target instance inside the subquery also throws:

```csharp
UsersTable u = new();
UsersTable i = new("i");
SqlStatement sql =
    DeleteFrom(u)
    .Where(u.Id.In(
        Select(i.Id)
        .From(i)
        .Where(i.Name == "duplicate")))
    .Build();

// DELETE FROM users
// WHERE id IN
// (SELECT "i".id FROM users "i" WHERE "i".name = :0)
```

| DBMS | Correlated `UPDATE` / `DELETE` |
|------|--------------------------------|
| MySQL, Oracle, PostgreSQL, SQLite | Alias the target — `DeleteFrom(new UsersTable("u"))` — so the outer column renders qualified. |
| SQL Server | The target cannot be aliased (aliasing throws at build) and joined DML is not yet supported — express the correlated update as a [`MERGE`](#merge-statement), the T-SQL idiom. |

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

**Dialect note:** On SQL Server the `INSERT` target cannot be aliased — pass an unaliased table (`InsertInto(new UsersTable())`), since T-SQL introduces a table alias through a `FROM` clause instead; building an aliased target for SQL Server throws. PostgreSQL, by contrast, uses an aliased `INSERT` target to name the row for [`ON CONFLICT`](#upsert-insert-update-or-skip), and MySQL, Oracle, and SQLite emit the alias faithfully as well.

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

**Note:** Multi-row `VALUES` is supported by MySQL, PostgreSQL, SQLite, and SQL Server (2008+). Oracle does not support multi-row `VALUES`; it uses `INSERT ALL` instead.

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

### UPSERT (Insert, Update, or Skip)

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

**MySQL — `INSERT IGNORE`**

For the do-nothing case — skip the rows that would collide, rather than update
them — MySQL also offers `INSERT IGNORE`, exposed as `InsertIgnoreInto(...)`:

```csharp
UsersTable u = new();
SqlStatement sql =
    InsertIgnoreInto(u, u.Id, u.Name)
    .Values(1, "newName")
    .Build(Dbms.MySql);

// INSERT IGNORE INTO users (id, name) VALUES (?0, ?1)
```

`IGNORE` downgrades the statement's errors to warnings — not only duplicate keys
but foreign-key violations and out-of-range values, whose rows are skipped or
coerced. Prefer it to the `ON DUPLICATE KEY UPDATE id = id` trick, which burns an
`AUTO_INCREMENT` value per skipped row; for a portable skip-existing insert, use
`INSERT … SELECT … WHERE NOT EXISTS`. On PostgreSQL and SQLite the do-nothing
insert is `ON CONFLICT DO NOTHING` (above), so SQLite's `INSERT OR IGNORE` is not
exposed separately.

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

// PostgreSQL 15+ / SQL Server: WHEN MATCHED THEN DELETE
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
> statements if you do not need atomic UPSERT semantics.

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
        SeniorId = new DbColumn(this, "senior_id");
        SeniorName = new DbColumn(this, "senior_name");
        SeniorAge = new DbColumn(this, "senior_age");
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
// (SELECT "users".id senior_id,
// "users".name senior_name,
// "users".age senior_age
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

SqlArtisan also supports more advanced WITH clause scenarios:

- **Recursive CTEs** — `WithRecursive()` emits `WITH RECURSIVE`, required by MySQL, PostgreSQL, and SQLite. On Oracle (< 23ai) and SQL Server, recurse with plain `With(...)` — those engines accept the recursive body but reject the `RECURSIVE` keyword. The analyzer warns when `WithRecursive()` targets a dialect that does not support it.
- **CTEs with DML main statements** — `With(...)` before an `INSERT`, `UPDATE`, or `DELETE` main statement is supported. DML *inside* a CTE body (PostgreSQL data-modifying CTEs) is not supported.

---

## RETURNING Clause

The `Returning()` method appends a `RETURNING` clause to `INSERT`, `UPDATE`, and `DELETE` statements, letting you read back the affected rows. It accepts the same items as a `SELECT` list, including `Asterisk` for `RETURNING *`.

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

For Oracle, chain `Into()` after `Returning()` to bind the returned columns into output parameters. Each output is an `OutputParameter` — a variable name, its `DbType`, and an optional size (required for variable-length types such as strings). The type cannot be inferred from the returned column, so it is supplied here. The number of output parameters must match the number of returned expressions, and each variable name must be unique. They are registered with `ParameterDirection.Output` so you can read their values after execution.

```csharp
UsersTable u = new();
SqlStatement sql =
    DeleteFrom(u)
    .Where(u.Id == 1)
    .Returning(u.Id, u.Name)
    .Into(new OutputParameter("outId", DbType.Int32),
          new OutputParameter("outName", DbType.String, 100))
    .Build(Dbms.Oracle);

// DELETE FROM users
// WHERE id = :0
// RETURNING id, name
// INTO :outId, :outName
```

With the Dapper integration, `ExecuteReturningInto` runs the statement and returns the parameter bag, so the bound output values can be read back by name after execution:

```csharp
DynamicParameters outputs = connection.ExecuteReturningInto(
    DeleteFrom(u)
    .Where(u.Id == 1)
    .Returning(u.Id, u.Name)
    .Into(new OutputParameter("outId", DbType.Int32),
          new OutputParameter("outName", DbType.String, 100)));

int deletedId = outputs.Get<int>("outId");
string deletedName = outputs.Get<string>("outName");
```

**Note:** `RETURNING` is supported by Oracle, PostgreSQL, and SQLite (3.35+). It is not supported by SQL Server (which uses `OUTPUT`) or MySQL. The `RETURNING ... INTO` form is Oracle-specific. SqlArtisan does not validate database feature support, so ensure the clause is valid for your target DBMS.
