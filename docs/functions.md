# Functions

[← Back to README](https://github.com/h-tacayama/SqlArtisan/blob/main/README.md) ·
[Reference Home](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/README.md)

> **How to read this reference.** Each entry maps a C# API to the SQL token it emits.
> Pick the API for your target DBMS; a single call is not rewritten per dialect.
> Dialect notes list databases in the order **MySQL, Oracle, PostgreSQL, SQLite, SQL Server**.
> Notes say which dialects accept a construct, never from which version; where
> a minimum engine version is recorded, it lives in the
> [version-bound register](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/analyzer.md#version-bound-constructs),
> kept in sync by a test.

## Contents

- [Numeric](#numeric-functions) · [Character](#character-functions) · [Date & Time](#date-and-time-functions) · [Conversion](#conversion-functions) · [Comparison](#comparison-functions) · [JSON](#json-functions) · [Full-Text Search](#full-text-search-functions) · [Aggregate](#aggregate-functions) · [String Aggregation](#string-aggregation-functions) · [Window / Analytic](#window-functions)
- [Bind Parameter Types](#bind-parameter-types)

---

SqlArtisan provides C# APIs that map to various SQL functions, enabling you to use them seamlessly within your queries. Here's a list of supported functions by category:

## Numeric Functions

- `Abs()` for `ABS`
- `Ceil()` for `CEIL` (Oracle; MySQL/PostgreSQL/SQLite accept both spellings)
- `Ceiling()` for `CEILING` (SQL Server; MySQL/PostgreSQL/SQLite accept both spellings)
- `Exp()` for `EXP`
- `Floor()` for `FLOOR`
- `Ln()` for `LN` (not supported by SQL Server — its `LOG(x)` is the natural logarithm)
- `Log(x)` for `LOG(x)`; `Log(base, x)` for `LOG(base, x)` — **the base differs per dialect, see below**
- `Log10()` for `LOG10` (not supported by Oracle — spell it `Log(10, x)` there)
- `Mod()` for `MOD` (not supported by SQL Server — use the `%` operator there; PostgreSQL rejects a `double precision` argument, taking `numeric` — observed on 16.15)
- `Power()` for `POWER`
- `Round()` for `ROUND` (single-argument `Round(expr)` is not supported by SQL Server — pass the scale explicitly there; PostgreSQL rejects `Round(x, n)` on a `double precision` `x`, taking `numeric` — observed on 16.15; **a negative scale differs on SQLite, see below**)
- `Sign()` for `SIGN`
- `Sqrt()` for `SQRT`
- `Trunc()` for `TRUNC` (numeric argument; Oracle, PostgreSQL, SQLite — the two-argument scale form is Oracle/PostgreSQL only)

> [!WARNING]
> **`Log(x)` silently changes its base with the target.** `SQLA0100` catches Oracle's rejection of `Log(x)` and every 2-arg `Log` on SQL Server; every other cell below runs, right or silently wrong.
>
> | Call | MySQL | Oracle | PostgreSQL | SQLite | SQL Server |
> |---|---|---|---|---|---|
> | `Log(x)` → `LOG(x)` | base e | *(rejected)* | base 10 | base 10 | base e |
> | `Log(b, x)` → `LOG(b, x)` | log<sub>b</sub>x | log<sub>b</sub>x | log<sub>b</sub>x | log<sub>b</sub>x | log<sub>x</sub>b |
>
> SQL Server's row inverts (value first, base second). For a base that does not vary: `Ln(x)` (MySQL, Oracle, PostgreSQL, SQLite), `Log10(x)` (MySQL, PostgreSQL, SQLite, SQL Server), or `Log(base, x)` on the four base-first engines.

`SQLA0100` also flags every two-argument `Log` call on SQL Server — blind to
argument order — including the correct T-SQL spelling: call `Log(base, x)`
with the arguments swapped, `Log(x, base)`, to reach an arbitrary base there.
To allow that one call, wrap it in `#pragma warning disable SQLA0100`;
`sqlartisan_construct_log_arity2 = supported` silences every 2-arg `Log` call
in its `.editorconfig` scope instead. On PostgreSQL, `Log(base, x)` is defined
for `numeric` only, so a `double precision` argument fails at the database,
not at the SqlArtisan layer.

> [!WARNING]
> **`Round(x, n)` with a negative `n` rounds to a whole number on SQLite**, which reads a negative scale as 0: `Round(o.Amount, -1)` returns `75` for `75.00` there, where MySQL, Oracle, PostgreSQL and SQL Server round to the tens and return `80`. `SQLA0104` reports a negative scale written as an integer-typed constant on SQLite; a `double`, `float` or `decimal` constant such as `-1.0`, or a scale held in a variable, is not checked. Rounding left of the decimal point is not available there.

---

## Character Functions

- `Concat(a, b)` for `CONCAT(a, b)`; `Concat(a, b, c, ...)` for `CONCAT(a, b, c, ...)` (Oracle takes only the two-argument form — see below)
- `ConcatWs(sep, a, b, ...)` for `CONCAT_WS(sep, a, b, ...)` (MySQL, PostgreSQL, SQLite, SQL Server)
- `CharLength()` for `CHAR_LENGTH` (MySQL, PostgreSQL)
- `Instr()` for `INSTR` (MySQL, Oracle, SQLite; the 3- and 4-argument forms are Oracle-only)
- `Left()` for `LEFT` (MySQL, PostgreSQL, SQL Server)
- `Lpad()` for `LPAD` (MySQL, Oracle, PostgreSQL; the 2-argument form is Oracle/PostgreSQL only)
- `Ltrim()` for `LTRIM` (two-argument trim-set form: Oracle, PostgreSQL, SQLite, SQL Server)
- `Length()` for `LENGTH` (MySQL, Oracle, PostgreSQL, SQLite)
- `Lengthb()` for `LENGTHB` (Oracle)
- `Lower()` for `LOWER`
- `Position()` for `POSITION(substr IN str)` (MySQL, PostgreSQL)
- `Right()` for `RIGHT` (MySQL, PostgreSQL, SQL Server)
- `Rpad()` for `RPAD` (MySQL, Oracle, PostgreSQL; the 2-argument form is Oracle/PostgreSQL only)
- `Rtrim()` for `RTRIM` (two-argument trim-set form: Oracle, PostgreSQL, SQLite, SQL Server)
- `RegexpCount()` for `REGEXP_COUNT` (Oracle, PostgreSQL, SQL Server)
- `RegexpInstr()` for `REGEXP_INSTR` (MySQL, Oracle, PostgreSQL, SQL Server; the 7-argument `subPatternPos` form is Oracle/PostgreSQL/SQL Server only)
- `RegexpReplace()` for `REGEXP_REPLACE` (MySQL, Oracle, PostgreSQL, SQL Server)
- `RegexpSubstr()` for `REGEXP_SUBSTR` (MySQL, Oracle, PostgreSQL, SQL Server; the 6-argument `subPatternPos` form is Oracle/PostgreSQL/SQL Server only)
- `Replace()` for `REPLACE`
- `Strpos()` for `STRPOS` (PostgreSQL)
- `Substr()` for `SUBSTR` (not supported by SQL Server — use `Substring()` there)
- `Substrb()` for `SUBSTRB` (Oracle)
- `Substring()` for `SUBSTRING` (MySQL, PostgreSQL, SQLite, SQL Server; on Oracle use `Substr()`)
- `Trim()` for `TRIM` (the two-argument trim-set form is not supported by SQLite)
- `Upper()` for `UPPER`

PostgreSQL and SQL Server gained the `REGEXP_*` family in specific releases — see the [version-bound register](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/analyzer.md#version-bound-constructs).

> [!WARNING]
> **`Length(x)` counts bytes on MySQL and characters on Oracle, PostgreSQL and
> SQLite.** A non-ASCII character counts more than once on MySQL; use
> `CharLength(x)` there to count characters.

> [!WARNING]
> **`RegexpReplace(source, pattern, replacement)` and its `position` form replace
> the first match on PostgreSQL and every match on MySQL, Oracle and SQL Server.**
> To replace every match on each, pass an occurrence of `0`:
> `RegexpReplace(source, pattern, replacement, 1, 0)`. See
> [PostgreSQL: `regexp_replace`](https://www.postgresql.org/docs/current/functions-matching.html#FUNCTIONS-POSIX-REGEXP).

> [!WARNING]
> **On PostgreSQL, `RegexpOptions.NewLine` and `RegexpOptions.MultipleLines` both
> stop `.` from matching a newline.** There `'n'` and `'m'` mean newline-sensitive
> matching, the opposite of `NewLine`'s meaning on MySQL and Oracle, and PostgreSQL's
> `.` matches a newline by default — so leave `NewLine` off there. See
> [PostgreSQL: embedded options](https://www.postgresql.org/docs/current/functions-matching.html#POSIX-EMBEDDED-OPTIONS-TABLE).

On Oracle, `Concat` takes two arguments; for three or more, use `DoublePipe(a, b, ...)`
(the `||` operator) — see
[Expressions: String Concatenation](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#string-concatenation)
for the full per-dialect guide, including a MySQL semantics trap `||` has that
`Concat` doesn't.

> [!NOTE]
> `Position()` and `Strpos()` both give a substring's 1-based index, but take
> their arguments in **reversed order** — `POSITION(substr IN str)` on MySQL and
> PostgreSQL, `STRPOS(str, substr)` on PostgreSQL alone.

On Oracle and SQLite, use `Instr()` above instead.

---

## Date and Time Functions

- `AddMonths()` for `ADD_MONTHS` (Oracle)
- `CurrentDate` for `CURRENT_DATE` (SQL Server gained it in a specific release — see the [version-bound register](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/analyzer.md#version-bound-constructs))
- `CurrentTime()` for `CURRENT_TIME` (not supported by Oracle or SQL Server); `CurrentTime(precision)` for `CURRENT_TIME(precision)` (MySQL, PostgreSQL)
- `CurrentTimestamp()` for `CURRENT_TIMESTAMP`; `CurrentTimestamp(precision)` for `CURRENT_TIMESTAMP(precision)` (MySQL, Oracle, PostgreSQL — MySQL's bare form has whole-second precision)
- `Date(timevalue[, modifier, ...])` for `DATE` (SQLite; 1-argument form also MySQL/PostgreSQL)
- `DateAdd()` for `DATE_ADD` (MySQL; PostgreSQL gained it in a specific release, with `IntervalLiteral(...)` only — see the [version-bound register](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/analyzer.md#version-bound-constructs); not `Dateadd()`, SQL Server's `DATEADD` below)
- `Dateadd()` for `DATEADD` (SQL Server)
- `Datediff()` for `DATEDIFF` (SQL Server)
- `DateFormat()` for `DATE_FORMAT` (MySQL)
- `Datepart()` for `DATEPART` (SQL Server)
- `DateSub()` for `DATE_SUB` (MySQL)
- `DateTrunc()` for `DATE_TRUNC` (PostgreSQL)
- `Datetime(timevalue[, modifier, ...])` for `DATETIME` (SQLite)
- `Datetrunc()` for `DATETRUNC` (SQL Server)
- `Extract()` for `EXTRACT` (MySQL, Oracle, PostgreSQL)
- `Julianday(timevalue[, modifier, ...])` for `JULIANDAY` (SQLite)
- `LastDay()` for `LAST_DAY` (MySQL, Oracle)
- `MonthsBetween()` for `MONTHS_BETWEEN` (Oracle)
- `Strftime(format, timevalue[, modifier, ...])` for `STRFTIME` (SQLite)
- `Sysdate` for `SYSDATE` (Oracle)
- `Systimestamp` for `SYSTIMESTAMP` (Oracle)
- `Timestampadd()` for `TIMESTAMPADD` (MySQL)
- `Timestampdiff()` for `TIMESTAMPDIFF` (MySQL)
- `Trunc()` for `TRUNC` (date argument; Oracle)

`DateAdd()`/`DateSub()` above take their shift amount from an `INTERVAL`
expression; for Oracle/PostgreSQL date-shift arithmetic (and MySQL's own
`+`/`-` spelling), see
[Expressions: Interval Expressions](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#interval-expressions).

> [!NOTE]
> `Date()`/`Datetime()`/`Julianday()`/`Strftime()` apply each modifier in order
> (`"start of month"`, `"+1 month"`). SQLite yields `NULL` rather than an error
> for a modifier it doesn't recognize or a time value it can't parse, so a typo
> surfaces as a silently empty column instead of a failed query.

> [!NOTE]
> On PostgreSQL, `Extract(DateTimePart.Microsecond, ...)` and `Extract(DateTimePart.Millisecond, ...)`
> return the plural fields — the seconds field, fractional part included, times 1,000,000 or
> 1,000 — not the component those members name: PostgreSQL 16 reads `MICROSECOND` and
> `MILLISECOND` as `MICROSECONDS` and `MILLISECONDS`. `SQLA0104` reports both there.

---

## Conversion Functions

- `Coalesce()` for `COALESCE`
- `Decode()` for `DECODE` (Oracle)
- `Format(value, format[, culture])` for `FORMAT(value, format[, culture])` (SQL Server)
- `If(condition, then, else)` for `IF(condition, then, else)` (MySQL; SQLite accepts it as a second name for `IIF`)
- `Ifnull(expr, alt)` for `IFNULL(expr, alt)` (MySQL, SQLite)
- `Iif(condition, then, else)` for `IIF(condition, then, else)` (SQLite, SQL Server)
- `Isnull(expr, alt)` for `ISNULL(expr, alt)` (SQL Server)
- `Nullif()` for `NULLIF`
- `Numtodsinterval()` for `NUMTODSINTERVAL` (Oracle)
- `Numtoyminterval()` for `NUMTOYMINTERVAL` (Oracle)
- `Nvl()` for `NVL` (Oracle)
- `ToChar()` for `TO_CHAR` (Oracle, PostgreSQL; the 1-argument form is Oracle-only)
- `ToDate()` for `TO_DATE` (Oracle, PostgreSQL)
- `ToNumber()` for `TO_NUMBER` (Oracle, PostgreSQL; the 1-argument form is Oracle-only)
- `ToTimestamp()` for `TO_TIMESTAMP` (Oracle, PostgreSQL)

> [!NOTE]
> MySQL, PostgreSQL, and SQLite each have their own `FORMAT()`, none matching SQL Server's
> .NET-style format strings: MySQL 8.0 formats a number (`Format(u.CreatedAt, "yyyy-MM")`
> returns `20,200,315,103,000`), SQLite 3.50's `printf()`-style one returns the timestamp
> unchanged, and PostgreSQL's takes `text` first, so a number or a date errors outright.

> [!NOTE]
> Both names have a near-twin elsewhere in the API: `Isnull(expr, alt)` is this
> fallback function, not the `expr IS NULL` predicate (that is the `IsNull`
> property); `If(...)` emits a SQL `IF(...)` value, not `ConditionIf(...)`, the
> C#-side helper that drops a `WHERE` condition and emits no SQL.

---

## Comparison Functions

- `Greatest()` for `GREATEST` (MySQL, Oracle, PostgreSQL, SQL Server)
- `Least()` for `LEAST` (MySQL, Oracle, PostgreSQL, SQL Server)

> [!WARNING]
> **`Greatest(...)` and `Least(...)` skip a NULL argument on PostgreSQL and SQL
> Server, and return NULL on MySQL and Oracle.** Where an argument can be NULL,
> give it a value with `Coalesce(...)` so every target compares the same
> arguments.

---

## JSON Functions

JSON paths are emitted as inline string literals: an expression index over a JSON field matches only the literal path, and Oracle requires a literal there in any case. On SQL Server a path holding a non-ASCII character is written `N'...'`, which keeps the character whatever the database's code page.

- `JsonExtract(jsonDoc, path)` for `JSON_EXTRACT(jsonDoc, 'path')` (MySQL, SQLite)
- `JsonValue(jsonDoc, path)` for `JSON_VALUE(jsonDoc, 'path')` (MySQL, Oracle, PostgreSQL, SQL Server)
- `JsonQuery(jsonDoc, path)` for `JSON_QUERY(jsonDoc, 'path')` (Oracle, PostgreSQL, SQL Server)

`JsonValue` (MySQL, PostgreSQL) and `JsonQuery` (PostgreSQL) have a minimum engine version there — see the [version-bound register](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/analyzer.md#version-bound-constructs).

JSON **operators** (`->`, `->>`, `#>`, `#>>`, and the JSONB predicates
`@>` / `?` / `?|` / `?&`) live in
[Expressions: JSON Operators](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#json-operators)
because they are infix operators, not function calls.

---

## Full-Text Search Functions

Exposed per dialect (no unified rewrite); each emits its dialect-native syntax verbatim. Search text is parameterized; the PostgreSQL text-search configuration is emitted as an inline string literal.

- `Match(columns...).Against(text[, modifier])` for `MATCH (...) AGAINST (... [modifier])` (MySQL, predicate); `.AgainstScore(...)` emits the same construct as the relevance score
- `ContainsScore(column, query[, label])` for `CONTAINS(column, query[, label])` (Oracle, relevance score)
- `Score(label)` for `SCORE(label)` (Oracle)
- `ToTsvector([config,] document)` for `TO_TSVECTOR` (PostgreSQL)
- `ToTsquery([config,] text)` for `TO_TSQUERY` (PostgreSQL)
- `PlaintoTsquery([config,] text)` for `PLAINTO_TSQUERY` (PostgreSQL)
- `TsMatch(vector, query)` for the `@@` match predicate (PostgreSQL)
- `Match(table, pattern)` for FTS5 `table MATCH pattern` (SQLite, predicate)
- `Contains(column, searchCondition)` for `CONTAINS(column, searchCondition)` (SQL Server, predicate)
- `Freetext(column, freetext)` for `FREETEXT(column, freetext)` (SQL Server, predicate)

Usage examples and each engine's index requirements live in
[Expressions: Full-Text Search](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#full-text-search).

---

## Aggregate Functions

- `Avg()` for `AVG`
- `Count(Asterisk)` for `COUNT(*)`; `Count(expr)` for `COUNT(expr)`
- `Max()` for `MAX`
- `Min()` for `MIN`
- `Sum()` for `SUM`
- `StddevPop()` for `STDDEV_POP` (MySQL, Oracle, PostgreSQL; SQL Server spells this `Stdevp()`)
- `StddevSamp()` for `STDDEV_SAMP` (MySQL, Oracle, PostgreSQL; SQL Server spells this `Stdev()`)
- `Stddev()` for `STDDEV` (MySQL, Oracle, PostgreSQL — **not the same statistic on all three, see below**)
- `Stdev()` for `STDEV`; `Stdevp()` for `STDEVP` (SQL Server — no double-D spelling there)
- `VarPop()` for `VAR_POP` (MySQL, Oracle, PostgreSQL; SQL Server spells this `Varp()`)
- `VarSamp()` for `VAR_SAMP` (MySQL, Oracle, PostgreSQL; SQL Server spells this `Var()`)
- `Variance()` for `VARIANCE` (MySQL, Oracle, PostgreSQL — **not the same statistic on all three, see below**)
- `Var()` for `VAR`; `Varp()` for `VARP` (SQL Server)

Chain `.Filter(condition)` on any of these for conditional aggregation —
`SUM(x) FILTER (WHERE ...)` (see
[Expressions: Conditional Aggregation](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#conditional-aggregation-filter)).
Chain `.Over(...)` to turn any of them into a window function (see
[Window Functions](#window-functions) below).

> [!NOTE]
> `COUNT(expr)` skips `NULL` values in `expr`; `COUNT(*)` counts every row.
> Pick `Count(Asterisk)` for a row count and `Count(expr)` only when `NULL`-skipping is
> the behavior you want.

> [!WARNING]
> **`STDDEV`/`VARIANCE` silently change which statistic they compute with the target.** All three engines accept the call and return a number, so `SQLA0100` cannot catch this — only the value differs:
>
> | Call | MySQL | Oracle | PostgreSQL |
> |---|---|---|---|
> | `Stddev(x)` → `STDDEV(x)` | population | sample | sample |
> | `Variance(x)` → `VARIANCE(x)` | population | sample | sample |
>
> Name the statistic instead: `StddevPop()`/`StddevSamp()`, `VarPop()`/`VarSamp()` — or, on SQL Server, `Stdevp()`/`Stdev()`/`Varp()`/`Var()`.

---

## String Aggregation Functions

Exposed per dialect (no unified rewrite); each emits its dialect-native syntax verbatim.

- `StringAgg(expr, sep)` for `STRING_AGG(expr, sep)` (PostgreSQL/SQLite/SQL Server). Order with an `OrderBy(...)` argument — `StringAgg(expr, sep, OrderBy(...))` (PostgreSQL/SQLite, inline) — or chain `.WithinGroup(OrderBy(...))` (SQL Server)
- `Listagg(expr, sep).WithinGroup(OrderBy(...))` for `LISTAGG(expr, sep) WITHIN GROUP (ORDER BY ...)` (Oracle)
- `GroupConcat(expr)` for `GROUP_CONCAT(expr)` (MySQL/SQLite)
- `GroupConcat(expr, sep)` for `GROUP_CONCAT(expr, sep)` (SQLite, positional separator)
- `GroupConcat(expr, Separator(sep))` for `GROUP_CONCAT(expr SEPARATOR 'sep')` (MySQL); `sep` is emitted as an inline escaped string literal (MySQL requires a literal here). Pass an `OrderBy(...)` argument to order the values — `GroupConcat(expr, OrderBy(...), Separator(sep))` (MySQL)
- `GroupConcat(Distinct, expr)` / `GroupConcat(Distinct, expr, Separator(sep))` for `GROUP_CONCAT(DISTINCT ...)`; `DISTINCT` works on both (SQLite only in the single-argument form)

> [!WARNING]
> **`GroupConcat(expr, sep)` silently changes meaning on MySQL.** MySQL reads the second positional argument as another concatenated value per row, not a separator, so the call runs and returns each element with `sep` appended, joined by the default comma. On MySQL, spell the separator with `GroupConcat(expr, Separator(sep))`; the positional form is SQLite's.

> [!WARNING]
> **On SQL Server, a `StringAgg` separator character the database's code page lacks arrives as `?`.** The separator is a plain literal, which SQL Server converts to the code page; `N'...'` would keep the character but is rejected over a `varchar` input, so it is not emitted. Under `SQL_Latin1_General_CP1_CI_AS`, `StringAgg(name, "、")` joins `a` and `b` as `a?b`. Use a separator the code page holds.

> [!NOTE]
> MySQL silently truncates `GROUP_CONCAT` output at `group_concat_max_len` (1024 bytes by default). Raise that session/global variable for large groups.

---

## Window Functions

- `CumeDist()` for `CUME_DIST()`
- `DenseRank()` for `DENSE_RANK()`
- `FirstValue(expr)` for `FIRST_VALUE(expr)`
- `Lag(expr[, offset[, default]])` for `LAG(...)`
- `LastValue(expr)` for `LAST_VALUE(expr)`
- `Lead(expr[, offset[, default]])` for `LEAD(...)`
- `NthValue(expr, n)` for `NTH_VALUE(expr, n)` (not supported by SQL Server)
- `Ntile(buckets)` for `NTILE(n)`
- `PercentileCont(fraction).WithinGroup(OrderBy(...))` for `PERCENTILE_CONT(fraction) WITHIN GROUP (ORDER BY ...)` (Oracle, PostgreSQL, SQL Server — on SQL Server chain `.Over(...)` after `.WithinGroup(...)`)
- `PercentileDisc(fraction).WithinGroup(OrderBy(...))` for `PERCENTILE_DISC(fraction) WITHIN GROUP (ORDER BY ...)` (Oracle, PostgreSQL, SQL Server — on SQL Server chain `.Over(...)` after `.WithinGroup(...)`)
- `PercentRank()` for `PERCENT_RANK()`
- `Rank()` for `RANK()`
- `RowNumber()` for `ROW_NUMBER()`

A window function is invalid without `OVER`, so complete each one with
`.Over(...)` — except `PercentileCont` / `PercentileDisc`, which
`.WithinGroup(...)` completes, as their entries above say (see [Expressions: Window Functions](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#window-functions)).
The library enforces this: a bare `Rank()`, `RowNumber()`, etc. is not a usable
expression, so passing one to `Select(...)` is rejected rather than emitting an
`OVER`-less token the database would reject.

---

## Bind Parameter Types

SqlArtisan automatically converts C# literal values into bind parameters (to bind one explicitly instead — so its marker can be shared across clauses — use `Sql.Bind(value)`; see [GROUP BY and HAVING Clause](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#group-by-and-having-clause)). Supported types are as follows:

- **Boolean**: `bool`
- **Character/String**: `char`, `string`
- **Date/Time**: `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`
- **Numeric**: `sbyte`, `byte`, `short`, `ushort`, `int`, `uint`, `nint`, `nuint`, `long`, `ulong`, `float`, `double`, `decimal`, `Complex`
- **GUID**: `Guid`
- **Enum**: Any `enum` type, except the function-argument enums `DateTimePart`, `RegexpOptions` and `SearchModifier`, which throw `ArgumentException` in a value position. Passed there, one is a slip — an argument given at an arity with no slot of its type, as `RegexpCount(s, p, RegexpOptions.CaseInsensitive)` gives the options as the position — and bound, it is the value — through `SqlArtisan.Dapper`, its integer. To store or compare one deliberately, bind it with `Sql.Bind(value)`.

That list is what SqlArtisan accepts as a bind value; whether a value reaches the database is the execution layer's and the provider's. Through `SqlArtisan.Dapper`, Dapper rejects `DateOnly`, `TimeOnly`, `nint`, `nuint` and `Complex` before the provider sees them (`NotSupportedException`, observed with Dapper 2.1.66), and the provider decides the rest — Npgsql rejects `sbyte`, `ushort`, `uint`, `ulong` and a `DateTimeOffset` whose offset is not zero (observed with Npgsql 9.0.3).

A `byte[]` is not in the list: `In(...)` reads an array as one value per element, so bind binary data with `new BindValue(bytes)`.

`Sql.Bind(value)` rejects `null` and `DBNull.Value`. For a bound `NULL`, use `Sql.BindNull()`: `Sql.Null` inlines the `NULL` keyword into the SQL text — as does a bare C# `null` in a `VALUES` row, the one position that takes one; anywhere else it throws — while `BindNull()` reserves a real parameter marker, so the statement's shape stays the same whether or not the value is null.

Two kinds of string never become a bind parameter, and they behave differently:

- **Forced literals** — positions that need a literal, because the dialect rejects a bind marker there or because a marker would change how the engine resolves or plans the statement: the `LIKE ... ESCAPE` character, MySQL's `GROUP_CONCAT ... SEPARATOR` and the `STRING_AGG` separator (SQL Server rejects a bound one over a `varchar` input), `IntervalLiteral` text, a JSON function's path (`JSON_EXTRACT` / `JSON_VALUE` / `JSON_QUERY`), a `JsonArrow` / `JsonArrowText` string key, a PostgreSQL text-search configuration, and the sequence name in `NEXTVAL('seq')` / `CURRVAL('seq')`. These are emitted inline as single-quoted literals with the quote character escaped (and the backslash too, on MySQL; on PostgreSQL a literal holding a backslash is written `E'...'` with it doubled, so it reads the same whatever the session's `standard_conforming_strings`). On SQL Server a literal holding a non-ASCII character is written `N'...'`, so the database's code page cannot replace it; the `STRING_AGG` separator stays plain, since SQL Server rejects `N'...'` there over a `varchar` input, so a separator character the code page lacks arrives as `?`. On SQL Server a backslash before a line break in such a literal gets a spare backslash and line break between the backslash and the line break, which SQL Server drops as a line continuation, so the literal keeps both characters.

> [!WARNING]
> **On MySQL with `NO_BACKSLASH_ESCAPES` in the session's `sql_mode`, a forced
> literal holding `\` keeps both of the backslashes SqlArtisan writes for it.**
> The value then carries two backslashes; an `ESCAPE` character fails instead
> (observed on MySQL 8.0.46).
> SqlArtisan writes MySQL literals for the default mode, so run these statements
> in a session without it.
- **Identifiers** — an alias (of a select item, a table, or a derived table: a subquery's `AsTable(...)`, `Values(...)`, `Unnest(...).AsTable(...)`, or a `DerivedTable` or other `DerivedTableBase` subclass), a CTE name, a table or column name (a CTE's or derived table's column list included), a `Cast(...)` target type, the sequence name in the Oracle (`Sequence("s").Nextval`) and SQL Server (`NextValueFor("s")`) spellings, and the `RETURNING ... INTO` output variable of an `OutputParameter`, whose name must start with a letter and hold only letters, digits, `_`, `$` and `#`. These are emitted **exactly as written**, with no escaping: a type name like `DECIMAL(10,2)` is not a name that could be quoted, and rewriting an alias you spelled would break the guarantee that the SQL you write is the SQL that runs. `Sql.Hints(...)` is not an identifier but behaves the same way — it is a raw-SQL escape hatch, emitted verbatim by definition.

So automatic parameterization prevents injection **through values**. Build identifiers from constants or an allowlist you control — never straight from request input.
