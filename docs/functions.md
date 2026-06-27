# Functions

[← Back to README](https://github.com/h-tacayama/SqlArtisan/blob/main/README.md) ·
[Reference Home](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/README.md)

> **How to read this reference.** Each entry maps a C# API to the SQL token it emits.
> Pick the API for your target DBMS; a single call is not rewritten per dialect.
> Dialect notes list databases in the order **MySQL, Oracle, PostgreSQL, SQLite, SQL Server**.

## Contents

- [Bind Parameter Types](#bind-parameter-types)
- [Numeric](#numeric-functions) · [Character](#character-functions) · [Date & Time](#date-and-time-functions) · [Conversion](#conversion-functions) · [Comparison](#comparison-functions) · [Aggregate](#aggregate-functions) · [String Aggregation](#string-aggregation-functions) · [Window / Analytic](#window-functions)

---

## Bind Parameter Types

SqlArtisan automatically converts C# literal values into bind parameters. Supported types are as follows:

- **Boolean**: `bool`
- **Character/String**: `char`, `string`
- **Date/Time**: `DateTime`, `DateOnly`, `TimeOnly`
- **Numeric**: `sbyte`, `byte`, `short`, `ushort`, `int`, `uint`, `nint`, `nuint`, `long`, `ulong`, `float`, `double`, `decimal`, `Complex`
- **Enum**: Any `enum` type

---

SqlArtisan provides C# APIs that map to various SQL functions, enabling you to use them seamlessly within your queries. Here's a list of supported functions by category:

## Numeric Functions

- `Abs()` for `ABS`
- `Ceil()` for `CEIL` (Oracle/SQLite; MySQL/PostgreSQL accept both spellings)
- `Ceiling()` for `CEILING` (SQL Server; MySQL/PostgreSQL accept both spellings)
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
- `Datepart()` for `DATEPART` (SQL Server)
- `DateTrunc()` for `DATE_TRUNC` (PostgreSQL)
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
- `Nullif()` for `NULLIF`
- `Nvl()` for `NVL`
- `ToChar()` for `TO_CHAR`
- `ToDate()` for `TO_DATE`
- `ToNumber()` for `TO_NUMBER`
- `ToTimestamp()` for `TO_TIMESTAMP`

---

## Comparison Functions

- `Greatest()` for `GREATEST`
- `Least()` for `LEAST`

---

## Aggregate Functions

- `Avg()` for `AVG`
- `Count()` for `COUNT`
- `Max()` for `MAX`
- `Min()` for `MIN`
- `Sum()` for `SUM`

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
