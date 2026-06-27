# Expressions

[← Back to README](https://github.com/h-tacayama/SqlArtisan/blob/main/README.md) ·
[Reference Home](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/README.md)

> **How to read this reference.** Each entry shows the **C# you write** and the
> **exact SQL it emits** — verbatim, what you see is what runs.
>
> **Design constraints (read before generating code):**
> - SqlArtisan emits the SQL you write; **cross-database portability is a non-goal**.
> - Pick the API for your target DBMS; a single call is not rewritten per dialect.
> - Dialect notes list databases in the order **MySQL, Oracle, PostgreSQL, SQLite, SQL Server**.

## Contents

- [NULL Literal](#null-literal)
- [Arithmetic Operators](#arithmetic-operators)
- [Conditions](#conditions)
- [CASE Expressions](#case-expressions)
- [CAST](#cast)
- [Window Functions](#window-functions)
- [String Aggregation](#string-aggregation)
- [Sequence](#sequence)

---
## NULL Literal
```csharp
SqlStatement sql =
    Select(
        Null,
        Null.As("NoValue"))
    .Build();

// SELECT
// NULL,
// NULL "NoValue"
```

---

## Arithmetic Operators
```csharp
UsersTable u = new();
SqlStatement sql =
    Select(
        u.Age + 1,
        u.Age - 2,
        u.Age * 3,
        u.Age / 4,
        u.Age % 5)
    .From(u)
    .Build();

// SELECT
// (age + :0),
// (age - :1),
// (age * :2),
// (age / :3),
// (age % :4)
// FROM users
```

---

## Conditions

### Logical Condition
```csharp
UsersTable u = new();
SqlStatement sql =
    Select(u.Name)
    .From(u)
    .Where(
        (u.Id == 1 & u.Id == 2)
        | (u.Id == 3 & Not(u.Id == 4)))
    .Build();

// SELECT name
// FROM users
// WHERE ((id = :0) AND (id = :1))
// OR ((id = :2) AND (NOT (id = :3)))
```

**Note:** SqlArtisan's logical conditions use `&` for SQL `AND` and `|` for SQL `OR`, unlike their standard C# meanings (bitwise or non-short-circuiting logic).

### Comparison Condition
```csharp
UsersTable u = new();
SqlStatement sql =
    Select(u.Name)
    .From(u)
    .Where(
        u.Id == 1
        & u.Id != 2
        & u.Id > 3
        & u.Id >= 4
        & u.Id < 5
        & u.Id <= 6)
    .Build();

// SELECT name
// FROM users
// WHERE (id = :0)
// AND (id <> :1)
// AND (id > :2)
// AND (id >= :3)
// AND (id < :4)
// AND (id <= :5)
```

### NULL Condition
```csharp
UsersTable u = new();
SqlStatement sql =
    Select(u.Name)
    .From(u)
    .Where(u.Id.IsNull
        | u.Id.IsNotNull)
    .Build();

// SELECT name
// FROM users
// WHERE (id IS NULL)
// OR (id IS NOT NULL)
```

### Pattern Matching Condition
```csharp
UsersTable u = new();
SqlStatement sql =
    Select(u.Name)
    .From(u)
    .Where(
        u.Name.Like("%a")
        | u.Name.NotLike("%b")
        | RegexpLike(u.Name, "^.*c$"))
    .Build();

// SELECT name
// FROM users
// WHERE (name LIKE :0)
// OR (name NOT LIKE :1)
// OR (REGEXP_LIKE(name, :2))
```

To match a wildcard (`%` or `_`) literally, chain `.Escape(...)` onto a `Like` / `NotLike` condition to emit a standard `ESCAPE` clause (supported identically across all dialects):

```csharp
UsersTable u = new();
SqlStatement sql =
    Select(u.Name)
    .From(u)
    .Where(u.Name.Like("100%_off").Escape('!'))
    .Build();

// SELECT name
// FROM users
// WHERE name LIKE :0 ESCAPE '!'
```

The escape character is emitted as an inline string literal rather than a bind
parameter — MySQL rejects a parameter marker after `ESCAPE` — so it is valid
identically on every dialect.

### BETWEEN Condition
```csharp
UsersTable u = new();
SqlStatement sql =
    Select(u.Name)
    .From(u)
    .Where(
        u.Id.Between(1, 2)
        | u.Id.NotBetween(3, 4))
    .Build();

// SELECT name
// FROM users
// WHERE (id BETWEEN :0 AND :1)
// OR (id NOT BETWEEN :2 AND :3)
```

### IN Condition
```csharp
UsersTable u = new();
SqlStatement sql =
    Select(u.Name)
    .From(u)
    .Where(
        u.Id.In(1, 2, 3)
        | u.Id.NotIn(4, 5, 6))
    .Build();

// SELECT name
// FROM users
// WHERE (id IN (:0, :1, :2))
// OR (id NOT IN (:3, :4, :5))
```

### IN Condition with Subquery
```csharp
UsersTable a = new("a");
UsersTable b = new("b");
UsersTable c = new("c");
SqlStatement sql =
    Select(a.Name)
    .From(a)
    .Where(
        a.Id.In(Select(b.Id).From(b))
        | a.Id.NotIn(Select(c.Id).From(c)))
    .Build();

// SELECT "a".name
// FROM users "a"
// WHERE ("a".id IN (SELECT "b".id FROM users "b"))
// OR ("a".id NOT IN (SELECT "c".id FROM users "c"))
```

### EXISTS Condition
```csharp
UsersTable a = new("a");
UsersTable b = new("b");
UsersTable c = new("c");
SqlStatement sql =
    Select(a.Name)
    .From(a)
    .Where(
        Exists(Select(b.Id).From(b))
        & NotExists(Select(c.Id).From(c)))
    .Build();

// SELECT "a".name
// FROM users "a"
// WHERE (EXISTS (SELECT "b".id FROM users "b"))
// AND (NOT EXISTS (SELECT "c".id FROM users "c"))
```

### Dynamic Condition

SqlArtisan allows you to dynamically include or exclude conditions using a helper like `ConditionIf`. This is useful when parts of your `WHERE` clause depend on runtime logic.

#### Case 1: Condition is Included 

```csharp
bool filterUnderTen = true;

UsersTable u = new();
SqlStatement sql =
    Select(u.Name)
    .From(u)
    .Where(
        u.Id > 0
        & ConditionIf(filterUnderTen, u.Id < 10))
    .Build();

// SELECT name
// FROM users
// WHERE (id > :0)
// AND (id < :1)
```

#### Case 2: Condition is Excluded

```csharp
bool filterUnderTen = false;

UsersTable u = new();
SqlStatement sql =
    Select(u.Name)
    .From(u)
    .Where(
        u.Id > 0
        & ConditionIf(filterUnderTen, u.Id < 10))
    .Build();

// SELECT name
// FROM users
// WHERE (id > :0)
```

---

## CASE Expressions

### Simple CASE Expression
```csharp
UsersTable u = new();
SqlStatement sql =
    Select(
        u.Id,
        u.Name,
        Case(
            u.StatusId,
            When(1).Then("Active"),
            When(2).Then("Inactive"),
            When(3).Then("Pending"),
            Else("Unknown"))
        .As("StatusDescription"))
    .From(u)
    .Build();

// SELECT id, name,
// CASE status_id
// WHEN :0 THEN :1
// WHEN :2 THEN :3
// WHEN :4 THEN :5
// ELSE :6
// END "StatusDescription"
// FROM users
```

### Searched CASE Expression
```csharp
UsersTable u = new();
SqlStatement sql =
    Select(
        u.Id,
        u.Name,
        Case(
            When(u.Age < 18).Then("Minor"),
            When(u.Age >= 18 & u.Age < 65).Then("Adult"),
            Else("Senior"))
        .As("AgeGroup"))
    .From(u)
    .Build();

// SELECT id, name,
// CASE
// WHEN (age < :0) THEN :1
// WHEN ((age >= :2) AND (age < :3)) THEN :4
// ELSE :5
// END "AgeGroup"
// FROM users
```

---

## CAST

The ANSI `CAST(expr AS type)` expression converts a value to another type. It is supported with the same syntax on every dialect.

```csharp
UsersTable u = new();
SqlStatement sql =
    Select(Cast(u.Id, "VARCHAR(10)").As("id_text"))
    .From(u)
    .Build();

// SELECT CAST(id AS VARCHAR(10)) "id_text"
// FROM users
```

The target type is emitted verbatim, so write the exact SQL data type for your target database (for example `VARCHAR2(10)` on Oracle, `NVARCHAR(10)` on SQL Server). Consistent with the [Design Philosophy](https://github.com/h-tacayama/SqlArtisan/blob/main/README.md#design-philosophy), SqlArtisan does not abstract or translate dialect-specific type names.

---

## Window Functions

### Example using ROW_NUMBER

```csharp
UsersTable u = new();
SqlStatement sql =
    Select(
        u.Id,
        u.Name,
        u.DepartmentId,
        RowNumber().Over(
            PartitionBy(u.DepartmentId)
            .OrderBy(u.Salary.Desc)))
    .From(u)
    .Build();

// SELECT id, name, department_id,
// ROW_NUMBER() OVER (
// PARTITION BY department_id
// ORDER BY salary DESC)
// FROM users
```

For a comprehensive list of all available window functions, please refer to the [Functions: Window Functions](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/functions.md#window-functions) section.

### Example using LAG / LEAD

`Lag(...)` / `Lead(...)` read a value from a row a given number of rows behind or ahead of the current row. The offset defaults to `1`, and an optional default value fills the gap at the edge of the window.

```csharp
UsersTable u = new();
SqlStatement sql =
    Select(
        u.Id,
        u.Salary,
        Lag(u.Salary).Over(OrderBy(u.Id)).As("prev_salary"),
        Lead(u.Salary, 1, 0).Over(OrderBy(u.Id)).As("next_salary"))
    .From(u)
    .Build();

// SELECT id, salary,
// LAG(salary) OVER (ORDER BY id) "prev_salary",
// LEAD(salary, 1, :0) OVER (ORDER BY id) "next_salary"
// FROM users
```

Both require an `OrderBy(...)` (optionally with `PartitionBy(...)`). The offset is emitted as a literal so the SQL stays portable (MySQL requires a literal here), while the default value is parameterized.

### Example using FIRST_VALUE / LAST_VALUE / NTH_VALUE

`FirstValue(...)`, `LastValue(...)`, and `NthValue(...)` read a value from a specific row of the window. Unlike the ranking and offset functions, they can be scoped by an explicit frame (`Rows(...)` / `Range(...)`).

```csharp
UsersTable u = new();
SqlStatement sql =
    Select(
        u.Id,
        FirstValue(u.Salary)
            .Over(PartitionBy(u.DepartmentId).OrderBy(u.Salary.Desc))
            .As("top_salary"))
    .From(u)
    .Build();

// SELECT id,
// FIRST_VALUE(salary) OVER (PARTITION BY department_id ORDER BY salary DESC) "top_salary"
// FROM users
```

- `LastValue` is frame-sensitive: the default frame ends at the current row, so pair it with an explicit frame such as `RowsBetween(UnboundedPreceding, UnboundedFollowing)` to read the last row of the whole partition.
- `NthValue`'s position is emitted as an integer literal, and is **not supported by SQL Server**.

### Example using an Aggregate

Aggregate functions (`Sum`, `Count`, `Avg`, `Max`, `Min`) can also be used as window functions via `Over(...)`.

```csharp
UsersTable u = new();
SqlStatement sql =
    Select(
        u.Id,
        u.Salary,
        Sum(u.Salary).Over(PartitionBy(u.DepartmentId)).As("dept_total"))
    .From(u)
    .Build();

// SELECT id, salary,
// SUM(salary) OVER (PARTITION BY department_id) "dept_total"
// FROM users
```

`Over()` supports the whole result set (`OVER ()`), `PartitionBy(...)`, `OrderBy(...)`, and `PartitionBy(...).OrderBy(...)`.

**Note:** Most databases do not allow `DISTINCT` in a windowed aggregate (e.g. `SUM(DISTINCT x) OVER (...)`), so avoid combining a distinct aggregate with `Over(...)`.

#### Window Frames (ROWS / RANGE)

Attach a frame to an ordered window with `Rows(...)` / `Range(...)` for moving or cumulative calculations.

```csharp
UsersTable u = new();
SqlStatement sql =
    Select(
        u.Id,
        Avg(u.Amount).Over(OrderBy(u.Date).Rows(Preceding(4))).As("moving_avg"))
    .From(u)
    .Build();

// SELECT id,
// AVG(amount) OVER (ORDER BY date ROWS 4 PRECEDING) "moving_avg"
// FROM users
```

- Bounds: `UnboundedPreceding`, `CurrentRow`, `UnboundedFollowing`, `Preceding(n)`, `Following(n)`.
- A single bound uses `Rows(bound)` / `Range(bound)` (e.g. `Rows(UnboundedPreceding)`); `RowsBetween(start, end)` / `RangeBetween(start, end)` produce `ROWS/RANGE BETWEEN ... AND ...`.
- A frame requires `ORDER BY`, so `Rows(...)` / `Range(...)` are available only after `OrderBy(...)` (optionally with `PartitionBy(...)`).
- Requires PostgreSQL, Oracle, MySQL 8.0+, SQLite 3.25+, or SQL Server 2012+.

### Example using PERCENTILE_CONT / PERCENTILE_DISC

`PercentileCont(...)` and `PercentileDisc(...)` are ordered-set aggregates that compute a percentile over an ordered group via `WITHIN GROUP (ORDER BY ...)`. The fraction (0..1) is emitted as a literal.

```csharp
UsersTable u = new();
SqlStatement sql =
    Select(PercentileCont(0.5).WithinGroup(OrderBy(u.Salary)).As("median"))
    .From(u)
    .Build();

// SELECT PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY salary) "median" FROM users
```

Attach `.Over(...)` for the windowed form — `Over()` over the whole result set, or `Over(PartitionBy(...))`:

```csharp
PercentileCont(0.5).WithinGroup(OrderBy(u.Salary)).Over()
// PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY salary) OVER ()

PercentileCont(0.5).WithinGroup(OrderBy(u.Salary)).Over(PartitionBy(u.DepartmentId))
// PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY salary) OVER (PARTITION BY department_id)
```

- Dialect support is split: Oracle allows both forms; PostgreSQL only the plain `WITHIN GROUP` form; SQL Server only the windowed `.Over(PartitionBy(...))` form. MySQL and SQLite do not support these functions.

---

## String Aggregation

String aggregation flattens the values of a group into one delimited string. This is the most syntactically divergent feature in scope, so SqlArtisan exposes it per dialect (no unified rewrite): you call the function your target DBMS supports, and the SQL you write is the SQL that runs.

### STRING_AGG (PostgreSQL / SQL Server)

```csharp
// PostgreSQL: ordering is inline inside the call, so pass OrderBy(...) as an argument
Select(StringAgg(u.Name, ", ", OrderBy(u.Name)))
    .From(u)
    .Build(Dbms.PostgreSql);
// SELECT STRING_AGG(name, :0 ORDER BY name) FROM users   [:0 = ", "]

// SQL Server: ordering uses WITHIN GROUP
Select(StringAgg(u.Name, ", ").WithinGroup(OrderBy(u.Name)))
    .From(u)
    .Build(Dbms.SqlServer);
// SELECT STRING_AGG(name, @0) WITHIN GROUP (ORDER BY name) FROM users   [@0 = ", "]
```

### LISTAGG (Oracle)

```csharp
Select(Listagg(u.Name, ", ").WithinGroup(OrderBy(u.Name)))
    .From(u)
    .Build(Dbms.Oracle);
// SELECT LISTAGG(name, :0) WITHIN GROUP (ORDER BY name) FROM users   [:0 = ", "]
```

### GROUP_CONCAT (MySQL / SQLite)

The separator diverges: SQLite takes a positional second argument, while MySQL uses a `SEPARATOR` keyword selected with `Sql.Separator(...)`. `DISTINCT` is supported by both (SQLite only in the single-argument form, without a separator). MySQL also accepts an inline `ORDER BY`, passed as an `OrderBy(...)` argument because it sits inside the call.

```csharp
// SQLite: positional separator
Select(GroupConcat(u.Name, ", "))
    .From(u)
    .Build(Dbms.Sqlite);
// SELECT GROUP_CONCAT(name, :0) FROM users   [:0 = ", "]

// MySQL: SEPARATOR keyword, with DISTINCT and an inline ORDER BY argument
Select(GroupConcat(Distinct, u.Name, OrderBy(u.Name.Desc), Separator(", ")))
    .From(u)
    .Build(Dbms.MySql);
// SELECT GROUP_CONCAT(DISTINCT name ORDER BY name DESC SEPARATOR ', ') FROM users
```

MySQL's grammar requires the `SEPARATOR` value to be a string literal (a bind parameter is a syntax error there), so `Sql.Separator(...)` emits it inline as a single-quote-escaped literal. SQLite's positional separator (`GroupConcat(expr, sep)`) remains a bind parameter.

> [!NOTE]
> MySQL silently truncates `GROUP_CONCAT` output at `group_concat_max_len` (1024 bytes by default). Raise that session/global variable (e.g. `SET SESSION group_concat_max_len = 1000000;`) when a group can exceed it.

---

## Sequence

### Oracle Example
```csharp
SqlStatement sql =
    Select(
        Sequence("users_id_seq").Currval,
        Sequence("users_id_seq").Nextval)
    .Build();

// SELECT
// users_id_seq.CURRVAL,
// users_id_seq.NEXTVAL
```

### PostgreSQL Example
```csharp
SqlStatement sql =
    Select(
        Currval("users_id_seq"),
        Nextval("users_id_seq"))
    .Build();

// SELECT
// CURRVAL('users_id_seq'),
// NEXTVAL('users_id_seq')
```

### SQL Server Example
```csharp
SqlStatement sql =
    Select(
        NextValueFor("users_id_seq"))
    .Build();

// SELECT
// NEXT VALUE FOR users_id_seq
```

