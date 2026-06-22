# Change Log
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

## [Unreleased]
### Added
- Added support for the GROUP BY grouping extensions `Rollup(...)`, `Cube(...)`, and `GroupingSets(...)`. `Group(...)` forms a composite grouping element — a multi-column set inside `GroupingSets(...)`, or a parenthesized composite column inside `Rollup(...)` / `Cube(...)` (e.g. `Rollup(Group(a, b), c)` → `ROLLUP((a, b), c)`) — with `Group()` being the grand total; a single-column `Group(x)` renders bare as `x`. PostgreSQL / Oracle / SQL Server emit the standard forms; MySQL emits `ROLLUP` only, in its `... WITH ROLLUP` suffix form, and throws `NotSupportedException` for `CUBE` / `GROUPING SETS`; SQLite throws for all three. (#121)
- Added support for the `ESCAPE` clause on `LIKE` / `NOT LIKE`: chain `.Escape(escapeChar)` onto a `Like(...)` / `NotLike(...)` condition (e.g. `Like("100%_off").Escape('!')`) to match wildcards (`%`, `_`) literally, supported identically across all dialects. (#123)

### Changed
- Reworked the benchmark suite into a fair builder comparison (every entrant emits SQL plus bind parameters for the same query), added linq2db and a labeled EF Core reference, and refreshed the README Performance section. (#79)

## [0.3.0-beta.1] - 2026-06-21
### Added
- Added support for the `MERGE` statement (Oracle / SQL Server, and PostgreSQL 15+): `MergeInto(...).Using(...).On(...)` with per-dialect `WhenMatched` / `WhenNotMatched` / `WhenNotMatchedBySource` branches; SQL Server output is automatically terminated with the required semicolon. (#89)
- Added support for per-dialect string aggregation: `StringAgg()` (PostgreSQL/SQL Server), `Listagg()` (Oracle), and `GroupConcat()` (MySQL/SQLite). (#88)
- Added support for the date/time arithmetic functions `DATEADD` / `DATEDIFF` (SQL Server) and `DATE_TRUNC` (PostgreSQL), as distinct per-dialect methods (`Dateadd()` / `Datediff()` / `DateTrunc()`). (#86)
- Added support for UPSERT on `INSERT`: `OnConflict(...).DoUpdateSet(...)` / `.DoNothing()` (PostgreSQL/SQLite) and `OnDuplicateKeyUpdate(...)` (MySQL), referencing the proposed row with `Sql.Excluded(column)`. (#85)
- Added support for the scalar functions `NULLIF`, `ROUND`, `CEIL`, `CEILING`, `FLOOR`, `POWER`, `SQRT`, and `SIGN`. (#84)

### Changed
- **[BREAKING CHANGE]** Renamed the `Datepart` enum to `DateTimePart` to avoid a `CS0119` collision with the `Sql.Datepart()` factory; emitted SQL is unchanged, callers update only the type name (e.g. `Datepart.Year` → `DateTimePart.Year`). (#99)

### Fixed
- Aliased DML now renders dialect-correctly. A table alias in `InsertInto` / `Update` / `DeleteFrom` is introduced with `AS` (PostgreSQL/SQLite/MySQL/SQL Server) or a bare space (Oracle), and target columns — the INSERT column list, the `ON CONFLICT` target, and `SET` / `DO UPDATE SET` left sides — are emitted unqualified. This makes the PostgreSQL `INSERT INTO t AS x ... ON CONFLICT ... DO UPDATE ... WHERE x.col` UPSERT idiom and aliased `UPDATE` / `DELETE` executable; previously they reused the SELECT-context aliasing (`t "x"` + `"x".col`) and produced invalid SQL. Aliasing the DML target itself remains engine-dependent and is not validated: SQL Server's structural single-table aliased `DELETE` / `UPDATE` (`DELETE x FROM t AS x` / `UPDATE x SET ... FROM t AS x`) is intentionally not emitted, and some engines reject an aliased INSERT target (e.g. MySQL has no alias slot in `INSERT INTO`). Since a single-table DML never requires an alias, omit it on those engines — `DeleteFrom(t).Where(...)` already renders valid unaliased SQL. (#96)
- `ORDER BY` can now follow `HAVING`: `GroupBy(...).Having(...).OrderBy(...)` previously did not compile, making `GROUP BY ... HAVING ... ORDER BY ...` inexpressible. (#111)

## [0.2.0-beta.4] - 2026-06-12
### Added
- Added support for pagination: `Limit`/`Offset` (PostgreSQL/MySQL/SQLite) and `OffsetRows`/`FetchFirst`/`FetchNext` (Oracle 12c+/SQL Server 2012+). (#49)
- Added support for the ANSI `CAST(expr AS type)` expression. (#52)
- Added support for multi-row `INSERT ... VALUES` by chaining `Values()`. (#54)
- Added support for aggregate window functions (`Sum`/`Count`/`Avg`/`Max`/`Min` with `Over(...)`). (#56)
- Added support for window frames (`ROWS` / `RANGE`) via `Rows(...)` / `Range(...)`. (#58)
- Added support for the `LAG` / `LEAD` offset window functions. (#60)
- Added support for the `NTILE(n)` window function. (#64)
- Added support for the `FIRST_VALUE` / `LAST_VALUE` / `NTH_VALUE` window functions. (#65)
- Added support for the `PERCENTILE_CONT` / `PERCENTILE_DISC` ordered-set aggregates (`WITHIN GROUP (ORDER BY ...)`). (#66)

### Changed
- Improved `SqlBuildingBuffer` memory efficiency and throughput. (#45)
- Documented the design philosophy and non-goals, and surfaced dialect-specific features in the README and via XML doc comments. (#47)
- Consolidated the ranking window functions' shared `Over(...)` overloads onto the `AnalyticFunction` base class (non-breaking). (#60)

### Fixed
- `PartitionBy` / `OrderBy` / `GroupBy` with no items (or a null array) now throw a clear exception at build time instead of generating invalid SQL such as `ORDER BY `. (#69)

## [0.2.0-beta.3] - 2026-06-07
### Added
- Added overloads to the `Decode` method to remove the array requirement. (#39)
- Added support for RETURNING clause. (#41)

## [0.2.0-beta.2] - 2025-07-19
### Added
- Added the ability to configure a global default DBMS. (#35)
- Added support for the `FOR UPDATE` clause to lock selected rows. (#37)

## [0.2.0-beta.1] - 2025-07-11
### Changed
- Promoted the project to Beta, removing the "Unstable" warning from the README.

## [0.2.0-alpha.2] - 2025-07-01
### Added
- Added support for the `WITH` clause (Common Table Expressions).
### Changed
- Updated README:
  - Added "Contributing" section to the README, providing guidance for feedback and issue reporting.

## [0.2.0-alpha.1] - 2025-06-19
### Changed
- **[BREAKING CHANGE]** Unified the C# API naming convention. (#27)
  - **New Rule:** Remove underscores from the SQL keyword and convert to PascalCase.
  - **Example:** `SysTimestamp` has been renamed to `Systimestamp`, and `LPad` has been renamed to `Lpad`.
- Updated README:
  - Added example and list of available Window Functions.
### Fixed
- Corrected table alias quoting for MySQL. (#31)
- **SqlArtisan.TableClassGen**: Fixed the generated base class to be `DbTableBase` instead of the incorrect `AbstractTable`. (#29)

## [0.1.0-alpha.17] - 2025-06-16
### Added
- Added support for `EXTRACT` function.
- Added support for the SQL Server `DATEPART` function.
- Added a "Why SqlArtisan?" section to the README.

## [0.1.0-alpha.16] - 2025-06-11
### Added
- Added support for boolean bind parameters.
- Added documentation for NULL Literal in the README.
- Added "Additional Query Details" section to the README, including documentation for Bind Parameter Types and Functions.
### Changed
- Omitted the optional `AS` keyword from column aliases in the generated SQL to minimize memory allocations.
- Updated benchmark results in the README to reflect the latest performance improvements.

## [0.1.0-alpha.15] - 2025-06-06
### Added
- Add support for SQL Server `NEXT VALUE FOR` sequence.
- Added `Sql.Case` overloads for more flexible `WHEN` clause definitions in Simple CASE and Searched CASE expressions.
### Changed
- Updated README:
  - Added examples for Set Operators (`UNION`, `EXCEPT`, `MINUS`, `INTERSECT` and their `ALL` versions) in "Usage Examples".
  - Added examples for Sequence (`CURRVAL`, `NEXTVAL`, `NEXT VALUE FOR`) in "Usage Examples".
  - Added examples for Arithmetic Operators (`+`, `-`, `*`, `/`, `%`) in "Usage Examples".
  - Added examples for `CASE` expressions (Simple CASE and Searched CASE) in "Usage Examples".
  
### Removed
- `SqlArtisan.DapperExtensions` has been removed. Users should migrate to `SqlArtisan.Dapper`.

## [0.1.0-alpha.14] - 2025-06-02
### Added
- Introduced `SqlArtisan.Dapper` as the new, recommended Dapper integration package.
### Changed
- `SqlArtisan.TableClassGen` now depends on the new `SqlArtisan.Dapper` package.
- Updated README:
  - Reorganized the Table of Contents to improve navigation.
  - Clarified bind parameter prefix handling in "Quick Start".
### Deprecated
- `SqlArtisan.DapperExtensions` is now deprecated and will be removed in a future release. Users should migrate to `SqlArtisan.Dapper`.

## [0.1.0-alpha.13] - 2025-05-29
### Added
- Added bind parameter prefix support for MySQL and SQL Server (#23).
- `SqlArtisan.DapperExtensions` now auto-detects the DBMS from `IDbConnection` to ensure correct bind parameter prefixes are used (#23).

## [0.1.0-alpha.12] - 2025-05-26
### Changed
- **[BREAKING CHANGE]** Rename `SqlWordbook` to `Sql` for conciseness and appropriateness (#21).

## [0.1.0-alpha.11] - 2025-05-23
### Added
- Add support for CURRVAL and NEXTVAL functions (#19).
### Changed
- Improved API discoverability by moving secondary public types (required by public interfaces) to sub-namespaces (#17).

## [0.1.0-alpha.10] - 2025-05-20
### Changed
- Improved documentation in README.

## [0.1.0-alpha.7] - 2025-05-18
### Added
- Added support for `CURRENT_DATE`, `CURRENT_TIME`, and `CURRENT_TIMESTAMP` functions.

## [0.1.0-alpha.5] - 2025-05-13
### Added
- Added support for the `TO_TIMESTAMP` function.

## [0.1.0-alpha.4] - 2025-05-12
### Changed
- Improved `SqlBuildingBuffer` performance and reduced its memory allocations by using `ArrayPool<T>`.

## [0.1.0-alpha.1] - 2025-05-05
### Added
- Initial alpha release.
