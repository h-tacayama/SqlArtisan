# Functions

[← Back to README](https://github.com/h-tacayama/SqlArtisan/blob/main/README.md) ·
[Reference Home](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/README.md)

> **How to read this reference.** Each entry maps a C# API to the SQL token it emits.
> Pick the API for your target DBMS; a single call is not rewritten per dialect.
> Dialect notes list databases in the order **MySQL, Oracle, PostgreSQL, SQLite, SQL Server**.

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
- `Round()` for `ROUND`
- `Sign()` for `SIGN`
- `Sqrt()` for `SQRT`
- `Trunc()` for `TRUNC` (Numeric Overload)

> [!WARNING]
> **`LOG` silently changes meaning with the target — in both of its forms.**
> Nothing catches the single-argument form's base change: the matrix marks
> `Log(x)` supported on all four engines that run it, so no diagnostic fires.
>
> | Call | MySQL | Oracle | PostgreSQL | SQLite | SQL Server |
> |---|---|---|---|---|---|
> | `Log(x)` → `LOG(x)` | base e | *(rejected)* | base 10 | base 10 | base e |
> | `Log(b, x)` → `LOG(b, x)` | log<sub>b</sub>x | log<sub>b</sub>x | log<sub>b</sub>x | log<sub>b</sub>x | log<sub>x</sub>b |
>
> Only Oracle rejects `Log(x)`; every other cell runs and returns a number,
> right or silently wrong. The bottom row inverts on SQL Server because T-SQL
> declares `LOG(float_expression [, base])` — value first, base second.
>
> `SQLA0100` flags `Log(x)` on Oracle, and — blind to argument order — *every*
> two-argument `Log` on SQL Server, including the correct T-SQL spelling: call
> `Log(base, x)` with the arguments swapped, `Log(x, base)`, to reach an
> arbitrary base there. To allow that one call, wrap it in
> `#pragma warning disable SQLA0100`; `sqlartisan_construct_log_arity2 = supported`
> silences every 2-arg `Log` call in its `.editorconfig` scope instead.
>
> **For a base that does not vary by target:** `Ln(x)` (MySQL, Oracle,
> PostgreSQL, SQLite 3.35+), `Log10(x)` (MySQL, PostgreSQL 12+, SQLite 3.35+,
> SQL Server), or `Log(base, x)` on the four base-first engines — where
> PostgreSQL defines that form for `numeric` only, so a `double precision`
> argument fails at the database, not at the SqlArtisan layer.

---

## Character Functions

- `Concat(a, b)` for `CONCAT(a, b)`; `Concat(a, b, c, ...)` for `CONCAT(a, b, c, ...)`
- `ConcatWs(sep, a, b, ...)` for `CONCAT_WS(sep, a, b, ...)`
- `CharLength()` for `CHAR_LENGTH`
- `Instr()` for `INSTR`
- `Left()` for `LEFT`
- `Lpad()` for `LPAD`
- `Ltrim()` for `LTRIM`
- `Length()` for `LENGTH`
- `Lengthb()` for `LENGTHB`
- `Lower()` for `LOWER`
- `Position()` for `POSITION(substr IN str)`
- `Right()` for `RIGHT`
- `Rpad()` for `RPAD`
- `Rtrim()` for `RTRIM`
- `RegexpCount()` for `REGEXP_COUNT`
- `RegexpReplace()` for `REGEXP_REPLACE`
- `RegexpSubstr()` for `REGEXP_SUBSTR`
- `Replace()` for `REPLACE`
- `Strpos()` for `STRPOS`
- `Substr()` for `SUBSTR`
- `Substrb()` for `SUBSTRB`
- `Trim()` for `TRIM`
- `Upper()` for `UPPER`

> [!NOTE]
> On Oracle, chain two-argument `Concat(a, b)` calls (`Concat(Concat(a, b), c)`)
> for three or more arguments, or use the `||` operator instead — see
> [Expressions: String Concatenation](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#string-concatenation)
> for the full per-dialect guide, including a MySQL semantics trap `||` has that
> `Concat` doesn't.

> [!NOTE]
> `Position()` and `Strpos()` both give a substring's 1-based index, but take
> their arguments in **reversed order** — `POSITION(substr IN str)` on MySQL and
> PostgreSQL, `STRPOS(str, substr)` on PostgreSQL alone. On Oracle and SQLite,
> use `Instr()` above instead.

---

## Date and Time Functions

- `AddMonths()` for `ADD_MONTHS`
- `CurrentDate` for `CURRENT_DATE`
- `CurrentTime` for `CURRENT_TIME`
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
- `Datetrunc()` for `DATETRUNC` (SQL Server 2022+; use `Format()` on earlier versions)
- `Extract()` for `EXTRACT` (Date/Time Overload)
- `Julianday(timevalue[, modifier, ...])` for `JULIANDAY` (SQLite)
- `LastDay()` for `LAST_DAY`
- `MonthsBetween()` for `MONTHS_BETWEEN`
- `Strftime(format, timevalue[, modifier, ...])` for `STRFTIME` (SQLite)
- `Sysdate` for `SYSDATE`
- `Systimestamp` for `SYSTIMESTAMP`
- `Timestampadd()` for `TIMESTAMPADD` (MySQL)
- `Timestampdiff()` for `TIMESTAMPDIFF` (MySQL)
- `Trunc()` for `TRUNC` (Date/Time Overload)

> [!NOTE]
> `DateAdd()`/`DateSub()` above take their shift amount from an `INTERVAL`
> expression; for Oracle/PostgreSQL date-shift arithmetic (and MySQL's own
> `+`/`-` spelling), see
> [Expressions: Interval Expressions](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#interval-expressions).

> [!NOTE]
> `Date()`/`Datetime()`/`Julianday()`/`Strftime()` apply each modifier in order
> (`"start of month"`, `"+1 month"`). SQLite yields `NULL` rather than an error
> for a modifier it doesn't recognize or a time value it can't parse, so a typo
> surfaces as a silently empty column instead of a failed query.

---

## Conversion Functions

- `Coalesce()` for `COALESCE`
- `Decode()` for `DECODE`
- `Format(value, format[, culture])` for `FORMAT(value, format[, culture])` (SQL Server)
- `If(condition, then, else)` for `IF(condition, then, else)` (MySQL; SQLite 3.48+ accepts it as a second name for `IIF`)
- `Ifnull(expr, alt)` for `IFNULL(expr, alt)` (MySQL, SQLite)
- `Iif(condition, then, else)` for `IIF(condition, then, else)` (SQLite 3.32+, SQL Server 2012+)
- `Isnull(expr, alt)` for `ISNULL(expr, alt)` (SQL Server)
- `Nullif()` for `NULLIF`
- `Numtodsinterval()` for `NUMTODSINTERVAL`
- `Numtoyminterval()` for `NUMTOYMINTERVAL`
- `Nvl()` for `NVL`
- `ToChar()` for `TO_CHAR`
- `ToDate()` for `TO_DATE`
- `ToNumber()` for `TO_NUMBER`
- `ToTimestamp()` for `TO_TIMESTAMP`

> [!NOTE]
> MySQL and SQLite each have their own same-named but incompatible `FORMAT()`.
> MySQL's formats a number to a fixed decimal count (`FORMAT(number, decimals[, locale])`);
> SQLite's (3.38+) is a `printf()` alias using substitution directives (`%s`, `%d`).
> Neither matches SQL Server's .NET-style (`"yyyy-MM-dd"`) format strings, so a
> call executes on both without erroring but not with the semantics this factory
> targets — there is no MySQL or SQLite equivalent of SQL Server's `Format(...)`.

> [!NOTE]
> Both names have a near-twin elsewhere in the API: `Isnull(expr, alt)` is this
> fallback function, not the `expr IS NULL` predicate (that is the `IsNull`
> property); `If(...)` emits a SQL `IF(...)` value, not `ConditionIf(...)`, the
> C#-side helper that drops a `WHERE` condition and emits no SQL.

---

## Comparison Functions

- `Greatest()` for `GREATEST`
- `Least()` for `LEAST`

---

## JSON Functions

JSON paths are emitted as inline string literals (SQL Server and Oracle require the path to be a literal, not a bind parameter).

- `JsonExtract(jsonDoc, path)` for `JSON_EXTRACT(jsonDoc, 'path')` (MySQL, SQLite)
- `JsonValue(jsonDoc, path)` for `JSON_VALUE(jsonDoc, 'path')` (MySQL 8.0.21+, Oracle, SQL Server)
- `JsonQuery(jsonDoc, path)` for `JSON_QUERY(jsonDoc, 'path')` (Oracle, SQL Server)

> [!NOTE]
> JSON **operators** (`->`, `->>`, `#>`, `#>>`, and the JSONB predicates
> `@>` / `?` / `?|` / `?&`) live in
> [Expressions: JSON Operators](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#json-operators)
> because they are infix operators, not function calls.

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

> [!NOTE]
> Usage examples and each engine's full-text index prerequisite live in
> [Expressions: Full-Text Search](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#full-text-search).

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

> [!NOTE]
> Chain `.Filter(condition)` on any of these for conditional aggregation —
> `SUM(x) FILTER (WHERE ...)` (see
> [Expressions: Conditional Aggregation](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#conditional-aggregation-filter)).
> Chain `.Over(...)` to turn any of them into a window function (see
> [Window Functions](#window-functions) below).

> [!NOTE]
> `COUNT(expr)` skips `NULL` values in `expr`; `COUNT(*)` counts every row.
> Pick `Count(Asterisk)` for a row count and `Count(expr)` only when `NULL`-skipping is
> the behavior you want — modern engines optimize `COUNT(*)` to the smallest
> usable index rather than materializing every column.

> [!WARNING]
> **`STDDEV`/`VARIANCE` silently change which statistic they compute with the target.**
> Both run on MySQL, Oracle, and PostgreSQL, but not as the same statistic:
>
> | Call | MySQL | Oracle | PostgreSQL |
> |---|---|---|---|
> | `Stddev(x)` → `STDDEV(x)` | population | sample | sample |
> | `Variance(x)` → `VARIANCE(x)` | population | sample | sample |
>
> All three engines accept the call and return a number, so `SQLA0100` cannot
> catch this — the construct is grammatically valid everywhere it runs; only
> the value it computes differs. Name the statistic instead:
> `StddevPop()`/`StddevSamp()`, `VarPop()`/`VarSamp()` — or, on SQL Server
> (which has no `STDDEV`/`VARIANCE` spelling at all), the `Stdevp()`/`Stdev()`/
> `Varp()`/`Var()` forms above.

---

## String Aggregation Functions

Exposed per dialect (no unified rewrite); each emits its dialect-native syntax verbatim.

- `StringAgg(expr, sep)` for `STRING_AGG(expr, sep)` (PostgreSQL/SQLite 3.44+/SQL Server). Order with an `OrderBy(...)` argument — `StringAgg(expr, sep, OrderBy(...))` (PostgreSQL/SQLite 3.44+, inline) — or chain `.WithinGroup(OrderBy(...))` (SQL Server)
- `Listagg(expr, sep).WithinGroup(OrderBy(...))` for `LISTAGG(expr, sep) WITHIN GROUP (ORDER BY ...)` (Oracle)
- `GroupConcat(expr)` for `GROUP_CONCAT(expr)` (MySQL/SQLite)
- `GroupConcat(expr, sep)` for `GROUP_CONCAT(expr, sep)` (SQLite, positional separator)
- `GroupConcat(expr, Separator(sep))` for `GROUP_CONCAT(expr SEPARATOR 'sep')` (MySQL); `sep` is emitted as an inline escaped string literal (MySQL requires a literal here). Pass an `OrderBy(...)` argument to order the values — `GroupConcat(expr, OrderBy(...), Separator(sep))` (MySQL)
- `GroupConcat(Distinct, expr)` / `GroupConcat(Distinct, expr, Separator(sep))` for `GROUP_CONCAT(DISTINCT ...)`; `DISTINCT` works on both (SQLite only in the single-argument form)

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
- `PercentileCont(fraction).WithinGroup(OrderBy(...))` for `PERCENTILE_CONT(fraction) WITHIN GROUP (ORDER BY ...)`
- `PercentileDisc(fraction).WithinGroup(OrderBy(...))` for `PERCENTILE_DISC(fraction) WITHIN GROUP (ORDER BY ...)`
- `PercentRank()` for `PERCENT_RANK()`
- `Rank()` for `RANK()`
- `RowNumber()` for `ROW_NUMBER()`

> [!NOTE]
> A window function is invalid without `OVER`, so complete each one with
> `.Over(...)` (see [Expressions: Window Functions](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#window-functions)).
> The library enforces this: a bare `Rank()`, `RowNumber()`, etc. is not a usable
> expression, so passing one to `Select(...)` is rejected rather than emitting an
> `OVER`-less token the database would reject.

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
