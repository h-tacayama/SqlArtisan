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
- `Floor()` for `FLOOR`
- `Mod()` for `MOD`
- `Power()` for `POWER`
- `Round()` for `ROUND`
- `Sign()` for `SIGN`
- `Sqrt()` for `SQRT`
- `Trunc()` for `TRUNC` (Numeric Overload)

---

## Character Functions

- `Concat()` for `CONCAT`
- `Instr()` for `INSTR`
- `Lpad()` for `LPAD`
- `Ltrim()` for `LTRIM`
- `Length()` for `LENGTH`
- `Lengthb()` for `LENGTHB`
- `Lower()` for `LOWER`
- `Rpad()` for `RPAD`
- `Rtrim()` for `RTRIM`
- `RegexpCount()` for `REGEXP_COUNT`
- `RegexpReplace()` for `REGEXP_REPLACE`
- `RegexpSubstr()` for `REGEXP_SUBSTR`
- `Replace()` for `REPLACE`
- `Substr()` for `SUBSTR`
- `Substrb()` for `SUBSTRB`
- `Trim()` for `TRIM`
- `Upper()` for `UPPER`

---

## Date and Time Functions

- `AddMonths()` for `ADD_MONTHS`
- `CurrentDate` for `CURRENT_DATE`
- `CurrentTime` for `CURRENT_TIME`
- `CurrentTimestamp` for `CURRENT_TIMESTAMP`
- `Dateadd()` for `DATEADD` (SQL Server)
- `Datediff()` for `DATEDIFF` (SQL Server)
- `DateFormat()` for `DATE_FORMAT` (MySQL)
- `Datepart()` for `DATEPART` (SQL Server)
- `DateTrunc()` for `DATE_TRUNC` (PostgreSQL)
- `Datetrunc()` for `DATETRUNC` (SQL Server 2022+; use `Format()` on earlier versions)
- `Extract()` for `EXTRACT` (Date/Time Overload)
- `LastDay()` for `LAST_DAY`
- `MonthsBetween()` for `MONTHS_BETWEEN`
- `Sysdate` for `SYSDATE`
- `Systimestamp` for `SYSTIMESTAMP`
- `Trunc()` for `TRUNC` (Date/Time Overload)

---

## Conversion Functions

- `Coalesce()` for `COALESCE`
- `Decode()` for `DECODE`
- `Format(value, format[, culture])` for `FORMAT(value, format[, culture])` (SQL Server)
- `Nullif()` for `NULLIF`
- `Nvl()` for `NVL`
- `ToChar()` for `TO_CHAR`
- `ToDate()` for `TO_DATE`
- `ToNumber()` for `TO_NUMBER`
- `ToTimestamp()` for `TO_TIMESTAMP`

> [!NOTE]
> SQLite (3.38+) has its own `FORMAT()` function — a `printf()` alias using
> substitution directives (`%s`, `%d`), not SQL Server's .NET-style
> (`"yyyy-MM-dd"`) format strings. The call executes there without erroring,
> but not with the semantics this factory targets; there is no SQLite
> equivalent of SQL Server's `Format(...)`.

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
> JSON **operators** (`->`, `->>`, `#>`, `#>>`) live in
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
- `Count()` for `COUNT(*)`; `Count(expr)` for `COUNT(expr)`
- `Max()` for `MAX`
- `Min()` for `MIN`
- `Sum()` for `SUM`

Chain `.Filter(condition)` on any of these for conditional aggregation — `SUM(x) FILTER (WHERE ...)` (see [Expressions: Conditional Aggregation](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#conditional-aggregation-filter)).

> [!NOTE]
> `COUNT(expr)` skips `NULL` values in `expr`; `COUNT(*)` counts every row.
> Pick `Count()` for a row count and `Count(expr)` only when `NULL`-skipping is
> the behavior you want — `COUNT(*)` carries no performance penalty on modern
> engines, all of which optimize it to the smallest usable index.

---

## String Aggregation Functions

Exposed per dialect (no unified rewrite); each emits its dialect-native syntax verbatim.

- `StringAgg(expr, sep)` for `STRING_AGG(expr, sep)` (PostgreSQL/SQL Server). Order with an `OrderBy(...)` argument — `StringAgg(expr, sep, OrderBy(...))` (PostgreSQL, inline) — or chain `.WithinGroup(OrderBy(...))` (SQL Server)
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

SqlArtisan automatically converts C# literal values into bind parameters. Supported types are as follows:

- **Boolean**: `bool`
- **Character/String**: `char`, `string`
- **Date/Time**: `DateTime`, `DateOnly`, `TimeOnly`
- **Numeric**: `sbyte`, `byte`, `short`, `ushort`, `int`, `uint`, `nint`, `nuint`, `long`, `ulong`, `float`, `double`, `decimal`, `Complex`
- **Enum**: Any `enum` type
