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
- `Mod()` for `MOD` (not supported by SQL Server — use the `%` operator there)
- `Power()` for `POWER`
- `Round()` for `ROUND` (single-argument `Round(expr)` is not supported by SQL Server — pass the scale explicitly there)
- `Sign()` for `SIGN`
- `Sqrt()` for `SQRT`
- `Trunc()` for `TRUNC` (Numeric Overload; Oracle, PostgreSQL, SQLite — the two-argument scale form is Oracle/PostgreSQL only)

> [!WARNING]
> **`LOG` silently changes meaning with the target, in both forms.** `SQLA0100` catches Oracle's rejection of `Log(x)` and every 2-arg `Log` on SQL Server; every other cell below runs, right or silently wrong.
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
- `Position()` for `POSITION(substr IN str)`
- `Right()` for `RIGHT` (MySQL, PostgreSQL, SQL Server)
- `Rpad()` for `RPAD` (MySQL, Oracle, PostgreSQL; the 2-argument form is Oracle/PostgreSQL only)
- `Rtrim()` for `RTRIM` (two-argument trim-set form: Oracle, PostgreSQL, SQLite, SQL Server)
- `RegexpCount()` for `REGEXP_COUNT` (Oracle, PostgreSQL)
- `RegexpInstr()` for `REGEXP_INSTR` (MySQL, Oracle, PostgreSQL; the 7-argument `subPatternPos` form is Oracle/PostgreSQL only)
- `RegexpReplace()` for `REGEXP_REPLACE` (MySQL, Oracle, PostgreSQL)
- `RegexpSubstr()` for `REGEXP_SUBSTR` (MySQL, Oracle, PostgreSQL; the 6-argument `subPatternPos` form is Oracle/PostgreSQL only)
- `Replace()` for `REPLACE`
- `Strpos()` for `STRPOS`
- `Substr()` for `SUBSTR` (not supported by SQL Server — use `Substring()` there)
- `Substrb()` for `SUBSTRB` (Oracle)
- `Substring()` for `SUBSTRING` (MySQL, PostgreSQL, SQLite, SQL Server; on Oracle use `Substr()`)
- `Trim()` for `TRIM` (the two-argument trim-set form is not supported by SQLite — nest `Ltrim()`/`Rtrim()` there)
- `Upper()` for `UPPER`

On Oracle, chain two-argument `Concat(a, b)` calls (`Concat(Concat(a, b), c)`)
for three or more arguments, or use the `||` operator instead — see
[Expressions: String Concatenation](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#string-concatenation)
for the full per-dialect guide, including a MySQL semantics trap `||` has that
`Concat` doesn't.

> [!NOTE]
> `Position()` and `Strpos()` both give a substring's 1-based index, but take
> their arguments in **reversed order** — `POSITION(substr IN str)` on MySQL and
> PostgreSQL, `STRPOS(str, substr)` on PostgreSQL alone. On Oracle and SQLite,
> use `Instr()` above instead.

---

## Date and Time Functions

- `AddMonths()` for `ADD_MONTHS` (Oracle)
- `CurrentDate` for `CURRENT_DATE` (not supported by SQL Server — use `CurrentTimestamp` there)
- `CurrentTime` for `CURRENT_TIME` (not supported by Oracle or SQL Server — use `CurrentTimestamp` there)
- `CurrentTimestamp` for `CURRENT_TIMESTAMP`
- `Date(timevalue[, modifier, ...])` for `DATE` (SQLite; 1-argument form also MySQL/PostgreSQL)
- `DateAdd()` for `DATE_ADD` (MySQL — not `Dateadd()`, SQL Server's `DATEADD` below)
- `Dateadd()` for `DATEADD` (SQL Server)
- `Datediff()` for `DATEDIFF` (SQL Server)
- `DateFormat()` for `DATE_FORMAT` (MySQL)
- `Datepart()` for `DATEPART` (SQL Server)
- `DateSub()` for `DATE_SUB` (MySQL)
- `DateTrunc()` for `DATE_TRUNC` (PostgreSQL)
- `Datetime(timevalue[, modifier, ...])` for `DATETIME` (SQLite)
- `Datetrunc()` for `DATETRUNC` (SQL Server)
- `Extract()` for `EXTRACT` (Date/Time Overload; MySQL, Oracle, PostgreSQL)
- `Julianday(timevalue[, modifier, ...])` for `JULIANDAY` (SQLite)
- `LastDay()` for `LAST_DAY` (MySQL, Oracle)
- `MonthsBetween()` for `MONTHS_BETWEEN` (Oracle)
- `Strftime(format, timevalue[, modifier, ...])` for `STRFTIME` (SQLite)
- `Sysdate` for `SYSDATE` (Oracle)
- `Systimestamp` for `SYSTIMESTAMP` (Oracle)
- `Timestampadd()` for `TIMESTAMPADD` (MySQL)
- `Timestampdiff()` for `TIMESTAMPDIFF` (MySQL)
- `Trunc()` for `TRUNC` (Date/Time Overload; Oracle)

`DateAdd()`/`DateSub()` above take their shift amount from an `INTERVAL`
expression; for Oracle/PostgreSQL date-shift arithmetic (and MySQL's own
`+`/`-` spelling), see
[Expressions: Interval Expressions](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#interval-expressions).

> [!NOTE]
> `Date()`/`Datetime()`/`Julianday()`/`Strftime()` apply each modifier in order
> (`"start of month"`, `"+1 month"`). SQLite yields `NULL` rather than an error
> for a modifier it doesn't recognize or a time value it can't parse, so a typo
> surfaces as a silently empty column instead of a failed query.

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
> MySQL, PostgreSQL, and SQLite each have their own same-named but incompatible
> `FORMAT()` (MySQL: fixed decimal count; PostgreSQL and SQLite: `printf()`-style
> substitution). None matches SQL Server's .NET-style format strings — a call
> executes on all three without erroring, but with different semantics.

> [!NOTE]
> Both names have a near-twin elsewhere in the API: `Isnull(expr, alt)` is this
> fallback function, not the `expr IS NULL` predicate (that is the `IsNull`
> property); `If(...)` emits a SQL `IF(...)` value, not `ConditionIf(...)`, the
> C#-side helper that drops a `WHERE` condition and emits no SQL.

---

## Comparison Functions

- `Greatest()` for `GREATEST` (MySQL, Oracle, PostgreSQL, SQL Server)
- `Least()` for `LEAST` (MySQL, Oracle, PostgreSQL, SQL Server)

---

## JSON Functions

JSON paths are emitted as inline string literals (Oracle and SQL Server require the path to be a literal, not a bind parameter).

- `JsonExtract(jsonDoc, path)` for `JSON_EXTRACT(jsonDoc, 'path')` (MySQL, SQLite)
- `JsonValue(jsonDoc, path)` for `JSON_VALUE(jsonDoc, 'path')` (MySQL, Oracle, SQL Server)
- `JsonQuery(jsonDoc, path)` for `JSON_QUERY(jsonDoc, 'path')` (Oracle, SQL Server)

JSON **operators** (`->`, `->>`, `#>`, `#>>`, and the JSONB predicates
`@>` / `?` / `?|` / `?&`) live in
[Expressions: JSON Operators](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#json-operators)
because they are infix operators, not function calls.

---

## Full-Text Search Functions

Exposed per dialect (no unified rewrite); each emits its dialect-native syntax verbatim. Search text is parameterized; the PostgreSQL text-search configuration is emitted as an inline string literal.

- `Match(columns...).Against(text[, modifier])` for `MATCH (...) AGAINST (... [modifier])` (MySQL, predicate); `.AgainstScore(...)` emits the same construct as the relevance score
- `ContainsScore(column, query[, label])` for `CONTAINS(column, query[, label])` (Oracle, relevance score 0–100)
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
> the behavior you want — modern engines optimize `COUNT(*)` to the smallest
> usable index rather than materializing every column.

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
`.Over(...)` (see [Expressions: Window Functions](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#window-functions)).
The library enforces this: a bare `Rank()`, `RowNumber()`, etc. is not a usable
expression, so passing one to `Select(...)` is rejected rather than emitting an
`OVER`-less token the database would reject.

---

## Bind Parameter Types

SqlArtisan automatically converts C# literal values into bind parameters (to bind one explicitly instead — so its marker can be shared across clauses — use `Sql.Bind(value)`; see [GROUP BY and HAVING Clause](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#group-by-and-having-clause)). Supported types are as follows:

- **Boolean**: `bool`
- **Character/String**: `char`, `string`
- **Date/Time**: `DateTime`, `DateOnly`, `TimeOnly`
- **Numeric**: `sbyte`, `byte`, `short`, `ushort`, `int`, `uint`, `nint`, `nuint`, `long`, `ulong`, `float`, `double`, `decimal`, `Complex`
- **Enum**: Any `enum` type

`Sql.Bind(value)` rejects `null`. For a bound `NULL`, use `Sql.BindNull()`: a bare `null` literal inlines the `NULL` keyword into the SQL text, while `BindNull()` reserves a real parameter marker, so the statement's shape stays the same whether or not the value is null.

Two kinds of string never become a bind parameter, and they behave differently:

- **Grammar-forced literals** — positions where the dialect rejects a bind marker: the `LIKE ... ESCAPE` character, MySQL's `GROUP_CONCAT ... SEPARATOR` and SQL Server's `STRING_AGG` separator, a JSON path, a PostgreSQL text-search configuration, and the sequence name in `NEXTVAL('seq')` / `CURRVAL('seq')`. These are emitted inline as single-quoted literals with the quote character escaped (and the backslash too, on MySQL).
- **Identifiers** — an alias, a table or column name, a `Cast(...)` target type, and the sequence name in the Oracle (`Sequence("s").Nextval`) and SQL Server (`NextValueFor("s")`) spellings. These are emitted **exactly as written**, with no escaping: a type name like `DECIMAL(10,2)` is not a name that could be quoted, and rewriting an alias you spelled would break the guarantee that the SQL you write is the SQL that runs. `Sql.Hints(...)` is not an identifier but behaves the same way — it is a raw-SQL escape hatch, emitted verbatim by definition.

So automatic parameterization prevents injection **through values**. Build identifiers from constants or an allowlist you control — never straight from request input.
