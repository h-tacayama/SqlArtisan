# Change Log
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

## [Unreleased]
### Added
- `SqlStatement.ToString()` now returns `Text` (the parameter-marked SQL), so logging or inspecting a built statement shows the SQL instead of the type name. Parameter values are not included (they may be sensitive); read `Parameters` explicitly when you need them. (#147)
### Added
- Added `DbTable`, an ad-hoc table reference: name a table inline (`new DbTable("users", "u")`) and read its columns by name with `Column(name)`, without declaring a typed `DbTableBase` subclass — mirroring `Cte` / `DerivedTable`. Columns are qualified by the alias (unqualified when none); usable in `SELECT` / `FROM` / `JOIN` and as an `INSERT` / `UPDATE` / `DELETE` target. (#145)
### Changed
- Split the API reference out of the README into `docs/` to keep the README a lean landing page (it ships in the NuGet package and renders on nuget.org). The README now carries a capability-map index; usage examples, expressions, and the function catalog moved to `docs/query-statements.md`, `docs/expressions.md`, and `docs/functions.md`, indexed by `docs/README.md`. README→docs links are absolute GitHub URLs since nuget.org does not resolve relative links. Added `llms.txt` as an LLM-friendly index for AI coding tools. (#143)
- Documented the `GREATEST` / `LEAST` functions, which were public but absent from the function reference. (#143)
### Build
- The published packages now enable Source Link (`Microsoft.SourceLink.GitHub`), a deterministic build on CI (`ContinuousIntegrationBuild`), and ship debugging symbols as a separate `.snupkg` (`DebugType=portable`) — so consumers can step straight into SqlArtisan source while debugging, and the assemblies are reproducible from this exact commit. Applies to `SqlArtisan`, `SqlArtisan.Dapper`, and `SqlArtisan.TableClassGen`. (#157)
- Declared the core `SqlArtisan` package trim- and AOT-compatible (`IsAotCompatible`, which turns on the trim/single-file/AOT analyzers): the builder uses no reflection or dynamic codegen, so it produces zero trim/AOT warnings — verified by a native-AOT smoke test — a trust signal for container, CLI, and serverless deployments. Scoped to the core; `SqlArtisan.Dapper` depends on Dapper and is out of scope. (#158)

## [0.4.0-beta.1] - 2026-06-27
### Added
- Added support for joining a correlated derived table via `APPLY` / `LATERAL`, each grammar exposed as its own method per ADR 0002: `CrossApply` / `OuterApply` (`CROSS APPLY` / `OUTER APPLY`, SQL Server / Oracle) and `CrossJoinLateral` / `LeftJoinLateral` / `JoinLateral(...).On(...)` (`CROSS JOIN LATERAL` / `LEFT JOIN LATERAL ... ON TRUE` / `JOIN LATERAL ... ON ...`, PostgreSQL / MySQL). The derived table is named by a `DerivedTableBase` handle — subclass it for typed `DbColumn` members, or use the inline `DerivedTable` and read columns with `Column(...)`. Per ADR 0003, `Build(Dbms)` emits faithfully and never rewrites one form into the other; availability is left to the database and the analyzer. (#122)
- Added support for the GROUP BY grouping extensions `Rollup(...)`, `Cube(...)`, and `GroupingSets(...)`. `Group(...)` forms a composite grouping element — a multi-column set inside `GroupingSets(...)`, or a parenthesized composite column inside `Rollup(...)` / `Cube(...)` (e.g. `Rollup(Group(a, b), c)` → `ROLLUP((a, b), c)`) — with `Group()` being the grand total; a single-column `Group(x)` renders bare as `x`. All three always emit their standard function forms (`ROLLUP(...)` / `CUBE(...)` / `GROUPING SETS(...)`) on every dialect. Per ADR 0003, `Build(Dbms)` emits faithfully and does not gate on DBMS availability — an unsupported combination (e.g. `Cube` or the function-form `Rollup(...)` on MySQL, any extension on SQLite) is emitted as written for the database/analyzer to flag, not thrown. (#121)
- Added MySQL's `WITH ROLLUP` GROUP BY suffix as a dedicated builder step: chain `.WithRollup()` onto `GroupBy(...)` (e.g. `GroupBy(a, b).WithRollup()` → `GROUP BY a, b WITH ROLLUP`). This is MySQL's grouping syntax; other dialects use the standard `Sql.Rollup(...)` function form. (#121)
- Added support for the `ESCAPE` clause on `LIKE` / `NOT LIKE`: chain `.Escape(escapeChar)` onto a `Like(...)` / `NotLike(...)` condition (e.g. `Like("100%_off").Escape('!')`) to match wildcards (`%`, `_`) literally. The escape character is emitted as an inline string literal (single-quote- and, on MySQL, backslash-escaped) rather than a bind parameter, since MySQL rejects a parameter marker after `ESCAPE`; the clause is valid identically across all dialects. (#123)
- Documented the entire public API with XML doc comments and enabled `GenerateDocumentationFile`, so the published packages now ship IntelliSense — summaries, parameters, the emitted-SQL form, and dialect notes — for every public `Sql.*` factory, fluent builder step, and type (the internal function-node classes remain implementation detail and are not documented). (#120)
- Documented the public `SqlArtisan.Dapper` surface and enabled `GenerateDocumentationFile` for that package, so its `IDbConnection` extensions (`Execute` / `Query` / `QuerySingle` / `QueryFirst` / `ExecuteScalar` / `QueryMultiple` / `ExecuteReader` and their async variants) and `ToDynamicParameters` now ship IntelliSense. (#133)

### Changed
- **[BREAKING CHANGE]** Renamed the CTE schema base class `CteSchemaBase` to `CteBase` (and named the new derived-table base `DerivedTableBase`, not `DerivedTableSchemaBase`), dropping the `Schema` term that read inconsistently with `DbTableBase` and invited confusion with a database schema. CTE subclasses update only the base type name (e.g. `class MyCte : CteSchemaBase` → `: CteBase`); emitted SQL is unchanged. (#122)
- Reworked the benchmark suite into a fair builder comparison (every entrant emits SQL plus bind parameters for the same query), added linq2db and a labeled EF Core reference, and refreshed the README Performance section. (#79)

### Fixed
- CTE and derived-table names are now alias-quoted at their definition (`WITH "cte" AS ...`, `... ) "x"`) and at every reference (`FROM "cte"`, `... JOIN ... "x"`), consistent with the already-quoted column references through their handles (`"cte".col` / `"x".col`). Previously the name was emitted bare, so on Oracle it case-folded (`cte` → `CTE`) while the quoted column reference stayed lowercase, yielding an unresolvable qualifier (ORA-00904) — most visibly for `CROSS APPLY` / `OUTER APPLY`, whose primary target is Oracle. Emitted SQL for CTEs changes accordingly (the name is now quoted). (#122)
- `GroupBy(...)` with a `null` grouping item now throws a clear `ArgumentNullException` instead of a `NullReferenceException`. (#121)
- `Rollup(...)`, `Cube(...)`, and `GroupingSets(...)` with a `null` trailing `params` array (e.g. `Rollup(a, null)`) now throw a clear `ArgumentNullException` instead of a `NullReferenceException`. (#121)
- **SqlArtisan.TableClassGen**: generated class/property names are now sanitized into valid C# identifiers — a column name starting with a digit, containing Oracle's `$` / `#`, or consisting only of separators previously produced uncompilable code. (#141)

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
