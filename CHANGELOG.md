# Change Log
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

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
