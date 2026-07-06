# Change Log
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

## [Unreleased]
### Added
- Published the project's trust baseline: `SECURITY.md` (private vulnerability reporting via GitHub Security Advisories, what counts as a vulnerability for a query builder, supported versions, response expectation) and a versioning & support policy (`docs/versioning.md`) — the SemVer commitment from 1.0, what counts as breaking (a change to the emitted SQL for the same input is at minimum a *minor* release, called out under a dedicated **Emitted SQL** CHANGELOG heading; builder-stage `I*Builder*` interfaces are not for user implementation, so adding members to them is *minor*), the deprecation process (`[Obsolete]` in a minor release, removal no earlier than the next major), supported runtimes, verified engine versions, and release cadence. Linked from the README. (#224)
- Opt-in Roslyn analyzer, shipped inside the `SqlArtisan` package with no extra reference. Set a target dialect — `sqlartisan_target_dbms` in `.editorconfig`, or `<SqlArtisanTargetDbms>` as an MSBuild property — and `SQLA0001` warns when a construct isn't supported there, per a curated, primary-source-verified dialect matrix (silent, and no false positive, for anything the matrix hasn't confirmed one way or the other). `SQLA0002` flags an unrecognized `.editorconfig` value. Every warning names the `sqlartisan_construct_<member>[_arity<N>]` override key that would silence it (`supported`) or force it (`unsupported`) — the arity-level key exists because a handful of overloaded members (e.g. `StringAgg`'s 3-argument inline-`ORDER BY` form) have narrower dialect support than their siblings. Severity is controlled the standard way (`dotnet_diagnostic.SQLA0001.severity`), so a mismatch can be promoted to a build error. See `docs/analyzer.md`. (#93)
### Changed
- **Breaking:** a query with a row-limiting clause (`Limit()` / `Offset()` / `OffsetRows()` / `FetchNext()` / `FetchFirst()`) can now be used as a subquery: a `LATERAL` / `APPLY` operand (per-group top-N), an aliased scalar select item (`.As("alias")`), a CTE body, or an `IN` / `NOT IN` / `EXISTS` / `ALL` / `ANY` / `SOME` operand. Source-compatible for callers but binary-breaking — the terminal `Offset()` / `FetchFirst()` / `FetchNext()` steps now return `ISelectBuilderPaginated` instead of `ISqlBuilder` — so rebuild code compiled against an earlier version. Recompiling also changes `In(...)` / `NotIn(...)` with a row-limited subquery, by design: previously they silently emitted a scalar-subquery comparison, `IN ((SELECT … LIMIT :0))` — wrong semantics that fail at runtime past one row — and now emit the list form `IN (SELECT … LIMIT :0)`. MySQL itself rejects `LIMIT` directly inside an `IN` / `ALL` / `ANY` / `SOME` subquery — route the limited query through a CTE there (see the pagination reference). (#240)
- **Breaking:** `SqlCondition`, `SqlExpression`, `ISubquery`, `TableReference`, `SortOrder`, `ExpressionAlias`, `CommonTableExpression`, and `DbSequence` moved from `SqlArtisan.Internal` to the root `SqlArtisan` namespace — dynamic-query idioms no longer need `using SqlArtisan.Internal;`. Binary-breaking: rebuild against this version. Fully-qualified references need a one-line namespace fix; a now-unnecessary `using SqlArtisan.Internal;` is harmless. `CommonTableExpression`'s constructor is now `internal` — obtain it via `Cte` / `CteBase`'s `.As(...)`. (#244)
### Fixed
- Corrected the `ALL` / `ANY` / `SOME` dialect claim: 0.5.0-beta.2's "supported on all five dialects" was wrong for SQLite, whose grammar has no quantified comparisons at all (`near "ALL": syntax error`) — caught by the new dialect sweep, which executes every analyzer-matrix entry against the live engines in both directions. The expression reference and the XML docs now name the four supporting dialects; emitted SQL is unchanged. (#93)
- Synced the dialect notes in the XML docs and the reference pages with the live-verified matrix — a mechanical audit found 19 remarks still carrying pre-verification claims: the `REGEXP_*` and `TO_*` families said “Oracle” though MySQL and/or PostgreSQL support them, `INSTR`'s 2-argument form is MySQL/Oracle/SQLite, `LAST_DAY` is MySQL too, SQLite accepts `CEILING` and `STRING_AGG` (3.44+), `JSON_VALUE` is MySQL 8.0.21+, the `LATERAL` joins are Oracle 12c+, `OFFSET … FETCH` includes PostgreSQL, `WHEN MATCHED THEN DELETE` is PostgreSQL 15+, and SQL Server accepts the legacy `WITH ROLLUP` form. Emitted SQL is unchanged. (#93)

## [0.5.0-beta.2] - 2026-07-03
### Added
- Full-text search, exposed per dialect (each engine's own grammar — no cross-DB rewrite). MySQL: `Match(columns...)` pending its mandatory `AGAINST` — complete with `.Against(text[, SearchModifier])` (a `WHERE` predicate) or `.AgainstScore(text[, SearchModifier])` (the numeric relevance score); modifiers `IN NATURAL LANGUAGE MODE` / `IN BOOLEAN MODE` / `WITH QUERY EXPANSION`. Oracle: `ContainsScore(column, query[, label])` for the score-returning `CONTAINS` (compare it, e.g. `> 0`) and `Score(label)` for `SCORE(label)`. PostgreSQL: `TsMatch(vector, query)` for the `@@` predicate with `ToTsvector` / `ToTsquery` / `PlaintoTsquery` (optional configuration emitted as an inline string literal). SQLite: `Match(table, pattern)` for the FTS5 `table MATCH pattern` predicate. SQL Server: `Contains(column, searchCondition)` and `Freetext(column, freetext)` predicates. Search text is parameterized on every dialect; each engine requires its full-text index prerequisite (documented in the expression reference). (#153)
- Scalar subqueries in expression position: a `SELECT` builder can now be used directly as a value — in a `SELECT` list, a `WHERE` comparison, or arithmetic — without an explicit wrapper. Chain `.As("alias")` for an aliased scalar subquery. Correlated subqueries (referencing outer-table columns) work naturally. (#156)
- `ALL` / `ANY` / `SOME` quantified comparison operators with subqueries: `col > All(subquery)`, `col > Any(subquery)`, `col = Some(subquery)`. Standard SQL, supported on all five dialects. (#196)
- JSON operations: `JsonExtract` (`JSON_EXTRACT` — MySQL, SQLite), `JsonValue` (`JSON_VALUE` — Oracle, SQL Server), `JsonQuery` (`JSON_QUERY` — Oracle, SQL Server) for function-call JSON access, and `JsonArrow` (`->`), `JsonArrowText` (`->>`), `JsonHashArrow` (`#>`), `JsonHashArrowText` (`#>>`) for infix JSON operators (MySQL, PostgreSQL, SQLite). JSON function paths are emitted as inline string literals (SQL Server and Oracle require a literal path); JSON operator keys are parameterized normally. (#152)

## [0.5.0-beta.1] - 2026-06-30
### Added
- `SqlStatement.ToString()` now returns `Text` (the parameter-marked SQL), so logging or inspecting a built statement shows the SQL instead of the type name. Parameter values are not included (they may be sensitive); read `Parameters` explicitly when you need them. (#147)
- Added `DbTable`, an ad-hoc table reference: name a table inline (`new DbTable("users", "u")`) and read its columns by name with `Column(name)`, without declaring a typed `DbTableBase` subclass — mirroring `Cte` / `DerivedTable`. Columns are qualified by the alias (unqualified when none); usable in `SELECT` / `FROM` / `JOIN` and as an `INSERT` / `UPDATE` / `DELETE` target. (#145)
- Completed Oracle `RETURNING … INTO` output-parameter binding. `Into(...)` now takes typed `OutputParameter` values — `new OutputParameter("outId", DbType.Int32)` — each a variable name, its `DbType`, and an optional size (required for variable-length types such as strings), instead of bare strings. The output type cannot be inferred from the returned column, and an untyped output parameter is rejected by the driver (Dapper cannot map a `DBNull` value to a `DbType`), so the type is supplied explicitly. Added `ExecuteReturningInto` / `ExecuteReturningIntoAsync` to the Dapper integration, which run the statement and return the populated `DynamicParameters` bag so the bound values can be read back by name (`outputs.Get<int>("outId")`) — previously the convenience `Execute` extension built and discarded its parameter bag internally, leaving the output parameters unreadable. End-to-end binding (numeric and string outputs) is verified against Oracle by the integration matrix.
- Added conditional aggregation via `FILTER (WHERE ...)`: chain `.Filter(condition)` on an aggregate (`Sum`, `Count`, `Avg`, `Max`, `Min`) to count only matching rows — `Sum(amount).Filter(age > 30)` → `SUM(amount) FILTER (WHERE age > :0)`. Combine with `.Over(...)` for a filtered window function (`SUM(x) FILTER (WHERE ...) OVER (...)`), and with `DISTINCT` (`Sum(Distinct, x).Filter(...)`). Native on PostgreSQL and SQLite; it is emitted faithfully on every dialect (never rewritten to `CASE`), leaving availability to the database and the analyzer. (#154)
- Added PostgreSQL's `DISTINCT ON (...)` as a select prefix: `Select(DistinctOn(a, b), ...)` emits `SELECT DISTINCT ON (a, b) ...`, keeping one row per distinct combination of the listed expressions (the first per the query's `ORDER BY`). It is a separate type from the `Distinct` keyword — valid only as a select prefix, never inside an aggregate — and requires at least one expression. PostgreSQL syntax, emitted faithfully on every dialect with availability left to the database. (#155)
### Changed
- **Breaking:** window/analytic functions (`Rank`, `RowNumber`, `DenseRank`, `CumeDist`, `PercentRank`, `Lag`, `Lead`, `Ntile`, `FirstValue`, `LastValue`, `NthValue`) are no longer `SqlExpression`s on their own — only `.Over(...)` turns one into a usable expression. A window function is a syntax error without `OVER` in every dialect, so a bare `Select(Rank())` previously emitted an `OVER`-less token the database would reject; it is now caught (a value position typed `SqlExpression` fails to compile, and `Select(...)`, which takes `object`, throws `ArgumentException`). Code that already attaches `.Over(...)` is unaffected. This mirrors the existing pending-clause enforcement on `Listagg`/`PercentileCont` (`.WithinGroup(...)`). (#150)
- Split the API reference out of the README into `docs/` to keep the README a lean landing page (it ships in the NuGet package and renders on nuget.org). The README now carries a capability-map index; usage examples, expressions, and the function catalog moved to `docs/query-statements.md`, `docs/expressions.md`, and `docs/functions.md`, indexed by `docs/README.md`. README→docs links are absolute GitHub URLs since nuget.org does not resolve relative links. Added `llms.txt` as an LLM-friendly index for AI coding tools. (#143)
- Documented the `GREATEST` / `LEAST` functions, which were public but absent from the function reference. (#143)
- Passing an incomplete "pending" expression to a value position now throws with an actionable hint instead of a generic type name. A window function used without `.Over(...)` (`Rank`, `RowNumber`, …) or an ordered-set aggregate used without `.WithinGroup(...)` (`Listagg`, `PercentileCont`, `PercentileDisc`) previously failed with `Invalid type for SelectItem: …AnalyticRankFunction`; it now reads `AnalyticRankFunction is not a complete SQL expression. Complete it with .Over(...) — a window function requires an OVER clause.`. The guidance is consistent across every value position (`SELECT` / `ORDER BY` / `GROUP BY` / `WHERE` / `INSERT` values); a genuinely unsupported type still gets the generic message. The line between what the library rejects (incomplete constructs) and what it emits faithfully (dialect-specific availability) is deliberate. (#190)
### Fixed
- `InsertInto(...).Values(...)` with a `null` value now emits a SQL `NULL` literal (`VALUES (:0, NULL)`) instead of throwing a `NullReferenceException`, so a nullable column can be inserted as `NULL`. (#169)
- `STRING_AGG`'s separator is now emitted as an inline string literal (`STRING_AGG(name, ', ')`) instead of a bind parameter. SQL Server requires the separator to be a literal and rejected the parameter form (`Argument data type nvarchar is invalid for argument 2`); the literal form is valid on both SQL Server and PostgreSQL. This matches how `GROUP_CONCAT`'s `SEPARATOR` and `LIKE ... ESCAPE` are already inlined. `Sql.StringAgg`'s `separator` parameter is now typed `string`. Caught by the integration matrix. (#168)
- A column aliased to a CTE / derived-table handle column via `.As(handle.Column)` now emits the alias **unquoted** (`... code cte_code`), matching how that column is later referenced (`"cte".cte_code`). Previously the definition was alias-quoted (`... code "cte_code"`) while the reference was bare, so on Oracle the bare reference case-folded to uppercase and could not resolve the lowercase quoted column — `ORA-00904`. Emitted SQL for such CTE/derived-table column aliases changes accordingly (the alias is no longer quoted); a string alias (`.As("name")`) is unaffected. Caught by the integration matrix. (#165)
### Build
- The published packages now enable Source Link (`Microsoft.SourceLink.GitHub`), a deterministic build on CI (`ContinuousIntegrationBuild`), and ship debugging symbols as a separate `.snupkg` (`DebugType=portable`) — so consumers can step straight into SqlArtisan source while debugging, and the assemblies are reproducible from this exact commit. Applies to `SqlArtisan`, `SqlArtisan.Dapper`, and `SqlArtisan.TableClassGen`. (#157)
- Declared the core `SqlArtisan` package trim- and AOT-compatible (`IsAotCompatible`, which turns on the trim/single-file/AOT analyzers): the builder uses no reflection or dynamic codegen, so it produces zero trim/AOT warnings — verified by a native-AOT smoke test — a trust signal for container, CLI, and serverless deployments. Scoped to the core; `SqlArtisan.Dapper` depends on Dapper and is out of scope. (#158)
### Tests
- Added a real-database integration test matrix (`tests/SqlArtisan.IntegrationTests`) that executes SqlArtisan-built statements against all five engines — MySQL, Oracle, PostgreSQL, SQL Server (via Testcontainers) and SQLite (in-process) — catching grammar and semantic bugs only the engine itself rejects. It exercises the query surface (joins, CTEs, window functions, `GROUP BY` and its extensions, set operators, `WHERE` predicates, `APPLY`/`LATERAL`), DML (per-dialect pagination, UPSERT, `MERGE`, `RETURNING` and Oracle `RETURNING … INTO`), the Dapper integration (sync and async, typed parameter binding), and edge cases (`NULL`, Unicode, `decimal`/`DateTime`/`boolean`). The exact-SQL unit tests remain the fast inner loop; the matrix runs nightly, on demand, and gates releases. (#151)

## [0.4.0-beta.1] - 2026-06-27
### Added
- Added support for joining a correlated derived table via `APPLY` / `LATERAL`, each grammar exposed as its own method: `CrossApply` / `OuterApply` (`CROSS APPLY` / `OUTER APPLY`, SQL Server / Oracle) and `CrossJoinLateral` / `LeftJoinLateral` / `JoinLateral(...).On(...)` (`CROSS JOIN LATERAL` / `LEFT JOIN LATERAL ... ON TRUE` / `JOIN LATERAL ... ON ...`, PostgreSQL / MySQL). The derived table is named by a `DerivedTableBase` handle — subclass it for typed `DbColumn` members, or use the inline `DerivedTable` and read columns with `Column(...)`. `Build(Dbms)` emits faithfully and never rewrites one form into the other; availability is left to the database and the analyzer. (#122)
- Added support for the GROUP BY grouping extensions `Rollup(...)`, `Cube(...)`, and `GroupingSets(...)`. `Group(...)` forms a composite grouping element — a multi-column set inside `GroupingSets(...)`, or a parenthesized composite column inside `Rollup(...)` / `Cube(...)` (e.g. `Rollup(Group(a, b), c)` → `ROLLUP((a, b), c)`) — with `Group()` being the grand total; a single-column `Group(x)` renders bare as `x`. All three always emit their standard function forms (`ROLLUP(...)` / `CUBE(...)` / `GROUPING SETS(...)`) on every dialect. `Build(Dbms)` emits faithfully and does not gate on DBMS availability — an unsupported combination (e.g. `Cube` or the function-form `Rollup(...)` on MySQL, any extension on SQLite) is emitted as written for the database/analyzer to flag, not thrown. (#121)
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
