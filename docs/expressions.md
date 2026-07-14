# Expressions

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

- [NULL Literal](#null-literal)
- [Arithmetic Operators](#arithmetic-operators)
- [String Concatenation](#string-concatenation)
- [Conditions](#conditions)
- [JSON Operators](#json-operators)
- [Full-Text Search](#full-text-search)
- [Scalar Subquery](#scalar-subquery)
- [ALL / ANY / SOME](#all--any--some)
- [CASE Expressions](#case-expressions)
- [CAST](#cast)
- [Window Functions](#window-functions)
- [Conditional Aggregation (FILTER)](#conditional-aggregation-filter)
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

## String Concatenation

Concatenation has no common syntax across the five engines, so pick the form for your target DBMS rather than reaching for one factory everywhere:

```csharp
SqlStatement sql =
    Select(DoublePipe(u.FirstName, " ", u.LastName))
    .From(u)
    .Build(Dbms.PostgreSql);

// SELECT (first_name || :0 || last_name)
// FROM users
```

- **Oracle, PostgreSQL, SQLite (every version)** — `DoublePipe(a, b, ...)` for the native `||` operator, chaining any number of arguments without nesting.
- **MySQL** — `Concat(a, b)` / `Concat(a, b, c, ...)` for the `CONCAT` function. `DoublePipe(...)` is a trap on MySQL: under the default `sql_mode`, `||` is **logical OR**, not concatenation — valid SQL with a completely different result, not a syntax error, so nothing at the SQL level warns you. Use `Concat(...)` there.
- **SQL Server** — the existing `+` operator (`u.FirstName + " " + u.LastName`) for native concatenation, or the 2-argument `Concat(a, b)` function; SQL Server has no `||` operator at all.
- **Oracle's `CONCAT`** takes exactly two arguments — `Concat(a, b, c)` (three or more) is invalid there; use `DoublePipe(...)` for chains of three or more on Oracle.

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

Pass an existing collection straight through — a `List<T>`, `T[]`, `HashSet<T>`, or any `IReadOnlyCollection<T>` — without spreading it into the `params` form:

```csharp
List<int> ids = GetSelectedIds();   // 0..n from a faceted filter

SqlStatement sql =
    Select(u.Name)
    .From(u)
    .Where(u.Id.In(ids))
    .Build();

// WHERE id IN (:0, :1, ...)   — one bind per element
```

The overload takes `IReadOnlyCollection<T>` rather than `IEnumerable<T>` deliberately: a `string` is an `IEnumerable<char>` but *not* an `IReadOnlyCollection<char>`, so `col.In("abc")` stays a single-value predicate (`IN (:0)`) instead of silently expanding into one bind per character. An empty collection throws at the call site — an empty `IN ()` is invalid SQL — so build the chain with or without the predicate keyed on whether the list has any elements.

Each element is a **separate** bind parameter, so the parameter count grows with the list, and a very large `IN` list runs into per-engine bind ceilings (SQL Server 2100 parameters; older SQLite 999). On PostgreSQL, an equality-against-array — `col = ANY(:param)`, a single array-valued bind whose plan is stable across list sizes — sidesteps both the parameter count and the ceiling; SqlArtisan's `In(...)` emits the per-element list form on every dialect, so reach for a CTE of the values (or the raw `= ANY` idiom) when the list is both large and size-varying on PostgreSQL.

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

On MySQL, `LIMIT` directly inside an `IN` / `ALL` / `ANY` / `SOME` subquery is rejected — route the limited query through a CTE instead. See the [Pagination dialect caveat](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#pagination) for the full per-position rule.

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

#### Case 3: Every Condition Excluded

When *every* condition is excluded, the `WHERE` has nothing left to run. Rather than silently drop the clause you wrote — an unintended full-table read is a real load risk — SqlArtisan throws at `Build()`:

```csharp
bool filterPositive = false;
bool filterUnderTen = false;

UsersTable u = new();

// Throws ArgumentException:
// "The WHERE clause requires a condition; omit it for an unfiltered statement."
Select(u.Name)
    .From(u)
    .Where(
        ConditionIf(filterPositive, u.Id > 0)
        & ConditionIf(filterUnderTen, u.Id < 10))
    .Build();
```

To read every row on purpose, omit `.Where(...)` entirely. Any condition clause you write follows the same rule — it must carry a real condition, or it throws.

#### Folding a Variable-Length List

When the predicates come from a collection rather than fixed call sites, fold them with `NoCondition` as the seed — the neutral element for both `&` and `|`:

```csharp
UsersTable u = new();
List<SqlCondition> filters = BuildFilters();   // 0..n runtime predicates

SqlStatement sql =
    Select(u.Name)
    .From(u)
    .Where(filters.Aggregate(NoCondition, (a, b) => a & b))
    .Build();
```

`NoCondition` contributes nothing itself — every real condition combined onto it survives, and each drops the seed out cleanly, so an `n`-element list yields exactly `n` predicates. It reads as its meaning: `NoCondition` is *absence*, not `TRUE`. That distinction matters — a `TRUE` seed would absorb an `|` fold (`TRUE | x` ≡ `TRUE`), whereas `NoCondition` is skipped in both `&` and `|` chains, so the same seed folds an `AND` list and an `OR` list identically. Prefer it over a dummy `ConditionIf(false, ...)` seed or a nullable accumulator.

An empty list folds to `NoCondition` alone — an empty `WHERE`, which throws at `Build()` exactly like Case 3 above. For a screen where the filter list can legitimately be empty, build the chain with or without `.Where(...)` keyed on whether any filter survived.

---

## JSON Operators

Access JSON elements with the `->`, `->>`, `#>`, and `#>>` infix operators. The key or path on the right side is parameterized normally.

### Element Access (`->` / `->>`)

```csharp
SqlStatement sql =
    Select(
        JsonArrow(u.Data, "address"),
        JsonArrowText(u.Data, "name"))
    .From(u)
    .Build(Dbms.PostgreSql);

// SELECT (data -> :0), (data ->> :1)
// FROM users
```

`JsonArrow` (`->`) returns the JSON type; `JsonArrowText` (`->>`) returns text. Both work on MySQL, PostgreSQL, and SQLite.

Chaining is natural — the result is a `SqlExpression`:

```csharp
// Nested access: (data -> :0) ->> :1
JsonArrowText(JsonArrow(u.Data, "address"), "city")
```

### Path Access (`#>` / `#>>`)

```csharp
SqlStatement sql =
    Select(
        JsonHashArrow(u.Data, "{a,b}"),
        JsonHashArrowText(u.Data, "{a,b}"))
    .From(u)
    .Build(Dbms.PostgreSql);

// SELECT (data #> :0), (data #>> :1)
// FROM users
```

PostgreSQL only. `JsonHashArrow` (`#>`) returns JSON; `JsonHashArrowText` (`#>>`) returns text.

---

## Full-Text Search

Full-text search has no common syntax across the five engines, so SqlArtisan exposes it per dialect (no unified rewrite): you call the API your target DBMS supports, and the SQL you write is the SQL that runs. Every engine requires a full-text index on the searched columns before these run — a `FULLTEXT` index (MySQL), an Oracle Text `CONTEXT` index, a `GIN` index over the tsvector (PostgreSQL), an FTS5 virtual table (SQLite), or a full-text index and catalog (SQL Server).

The examples below assume a `posts` table class with `Title` / `Body` columns.

### MATCH ... AGAINST (MySQL)

`Match(columns...)` is pending its mandatory `AGAINST` clause: complete it with `.Against(text)` (a `WHERE` predicate) or `.AgainstScore(text)` (the numeric relevance score, for a select list or `ORDER BY`). Both take an optional `SearchModifier` — `InNaturalLanguageMode`, `InBooleanMode`, or `WithQueryExpansion`; omitted, MySQL defaults to natural language mode.

```csharp
SqlStatement sql =
    Select(post.Title)
    .From(post)
    .Where(Match(post.Title, post.Body).Against("+database -orm", SearchModifier.InBooleanMode))
    .Build(Dbms.MySql);

// SELECT title FROM posts
// WHERE MATCH (title, body) AGAINST (?0 IN BOOLEAN MODE)
```

### CONTAINS / SCORE (Oracle)

`ContainsScore(column, query)` emits Oracle Text's `CONTAINS`, which returns a relevance score (0–100; 0 = no match) — compare it in `WHERE`. Pass a `label` to read the score elsewhere via `Score(label)`.

```csharp
SqlStatement sql =
    Select(post.Title, Score(1).As("relevance"))
    .From(post)
    .Where(ContainsScore(post.Body, "database", 1) > 0)
    .OrderBy(Score(1).Desc)
    .Build(Dbms.Oracle);

// SELECT title, SCORE(1) "relevance" FROM posts
// WHERE CONTAINS(body, :0, 1) > :1 ORDER BY SCORE(1) DESC
```

### TO_TSVECTOR @@ TO_TSQUERY (PostgreSQL)

`TsMatch(vector, query)` emits the `@@` match predicate. Build its sides with `ToTsvector([config,] document)`, and `ToTsquery([config,] text)` (tsquery syntax: `&`, `|`, `!`) or `PlaintoTsquery([config,] text)` (plain text, terms ANDed). The text-search configuration is emitted as an inline string literal.

```csharp
SqlStatement sql =
    Select(post.Title)
    .From(post)
    .Where(TsMatch(
        ToTsvector("english", post.Body),
        PlaintoTsquery("english", "database query")))
    .Build();

// SELECT title FROM posts
// WHERE TO_TSVECTOR('english', body) @@ PLAINTO_TSQUERY('english', :0)
```

### FTS5 MATCH (SQLite)

`Match(table, pattern)` emits the FTS5 `table MATCH pattern` predicate against an FTS5 virtual table. The table renders as its bare name, qualified by its alias when one is declared (`"a".posts_fts MATCH ...`).

```csharp
DbTable fts = new("posts_fts");

SqlStatement sql =
    Select(fts.Column("title"))
    .From(fts)
    .Where(Match(fts, "database"))
    .Build(Dbms.Sqlite);

// SELECT title FROM posts_fts
// WHERE posts_fts MATCH :0
```

### CONTAINS / FREETEXT (SQL Server)

`Contains(column, searchCondition)` matches words, prefixes, and boolean combinations; `Freetext(column, freetext)` matches by meaning rather than exact wording. Both are `WHERE` predicates.

```csharp
SqlStatement sql =
    Select(post.Title)
    .From(post)
    .Where(Contains(post.Body, "database AND query"))
    .Build(Dbms.SqlServer);

// SELECT title FROM posts
// WHERE CONTAINS(body, @0)
```

---

## Scalar Subquery

A `SELECT` builder can be used directly as a scalar value — in a `SELECT` list, a `WHERE` comparison, or arithmetic. Chain `.As("alias")` for an aliased column.

```csharp
UsersTable u = new("u");
UsersTable s = new("s");

// In a SELECT list with alias
SqlStatement sql =
    Select(
        u.Name,
        Select(Max(s.Age)).From(s).As("max_age"))
    .From(u)
    .Build();

// SELECT "u".name, (SELECT MAX("s".age) FROM users "s") "max_age"
// FROM users "u"
```

```csharp
// In a WHERE comparison
SqlStatement sql =
    Select(u.Name)
    .From(u)
    .Where(u.Age > Select(Avg(s.Age)).From(s))
    .Build();

// SELECT "u".name
// FROM users "u"
// WHERE "u".age > (SELECT AVG("s".age) FROM users "s")
```

```csharp
// Correlated subquery
SqlStatement sql =
    Select(u.Name)
    .From(u)
    .Where(u.Age > Select(Max(s.Age)).From(s).Where(s.Name == u.Name))
    .Build();

// SELECT "u".name
// FROM users "u"
// WHERE "u".age > (SELECT MAX("s".age) FROM users "s"
// WHERE "s".name = "u".name)
```

---

## ALL / ANY / SOME

The quantified comparison operators `ALL`, `ANY`, and `SOME` compare a scalar value against every row returned by a subquery. `SOME` is a synonym for `ANY` — `Some(subquery)` emits `SOME (...)` exactly as `Any(subquery)` emits `ANY (...)`.

Supported on MySQL, Oracle, PostgreSQL, and SQL Server — SQLite's grammar has no quantified comparisons and rejects all three.

```csharp
UsersTable u = new("u");
UsersTable s = new("s");

// col > ALL (subquery)
SqlStatement sql =
    Select(u.Name)
    .From(u)
    .Where(u.Age > All(Select(s.Age).From(s)))
    .Build();

// SELECT "u".name
// FROM users "u"
// WHERE "u".age > ALL (SELECT "s".age FROM users "s")
```

```csharp
// col > ANY (subquery)
SqlStatement sql =
    Select(u.Name)
    .From(u)
    .Where(u.Age > Any(Select(s.Age).From(s)))
    .Build();

// SELECT "u".name
// FROM users "u"
// WHERE "u".age > ANY (SELECT "s".age FROM users "s")
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

### Bind-Parameter Count in CASE

Every `WHEN`, `THEN`, and `ELSE` literal becomes a bind parameter — the 3-arm simple CASE above produces 7 binds (3 match values + 3 result values + 1 ELSE). A 3-column pivot built from single-arm CASEs produces 9. This is the same literals-are-binds design used everywhere else: it guarantees injection safety and lets the engine reuse the execution plan when only the label values change.

Most engines handle this comfortably — older SQLite versions cap at 999 parameters per statement (modern SQLite allows 32,766) and SQL Server caps at 2,100. A CASE in a `SELECT` list that also appears in `GROUP BY` doubles the bind count because the expression is repeated; wrapping the CASE in a CTE or subquery and grouping on its alias avoids the duplication.

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
- Requires MySQL 8.0+, Oracle, PostgreSQL, SQLite 3.25+, or SQL Server 2012+.

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

## Conditional Aggregation (FILTER)

Restrict an aggregate to the rows matching a condition with `.Filter(...)` — `agg(...) FILTER (WHERE ...)`.

```csharp
UsersTable u = new();
SqlStatement sql =
    Select(
        Count(u.Id).Filter(u.IsActive == true).As("active"),
        Count(u.Id).As("total"))
    .From(u)
    .Build();

// SELECT COUNT(id) FILTER (WHERE is_active = :0) "active", COUNT(id) "total"
// FROM users
```

Chain `.Over(...)` afterwards for a filtered window function — `SUM(amount) FILTER (WHERE ...) OVER (PARTITION BY ...)`.

- Native on PostgreSQL and SQLite. It is emitted faithfully on every dialect (never rewritten to a `CASE` expression); engines without it reject it, which the analyzer can flag.

---

## String Aggregation

String aggregation flattens the values of a group into one delimited string. This is the most syntactically divergent feature in scope, so SqlArtisan exposes it per dialect (no unified rewrite): you call the function your target DBMS supports, and the SQL you write is the SQL that runs.

### STRING_AGG (PostgreSQL / SQLite 3.44+ / SQL Server)

```csharp
// PostgreSQL / SQLite (3.44+): ordering is inline inside the call, so pass OrderBy(...) as an argument
Select(StringAgg(u.Name, ", ", OrderBy(u.Name)))
    .From(u)
    .Build(Dbms.PostgreSql);
// SELECT STRING_AGG(name, ', ' ORDER BY name) FROM users

// SQL Server: ordering uses WITHIN GROUP
Select(StringAgg(u.Name, ", ").WithinGroup(OrderBy(u.Name)))
    .From(u)
    .Build(Dbms.SqlServer);
// SELECT STRING_AGG(name, ', ') WITHIN GROUP (ORDER BY name) FROM users
```

The separator is emitted as an inline single-quote-escaped string literal rather
than a bind parameter, because SQL Server requires `STRING_AGG`'s separator to be
a literal (a bind parameter is rejected).

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
