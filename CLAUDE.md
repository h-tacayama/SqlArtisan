# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this project is

**SqlArtisan** is a type-safe SQL query builder for C# (.NET 8). You write
SQL-like C# and it produces the SQL string plus its bind parameters.
`Directory.Build.props` is the single source of truth for the shipped version
across all three packages.

**Core design philosophy — read this before proposing changes:**
> "The SQL you write is the SQL that runs. Cross-database portability is a
> deliberate non-goal."

The broader mission and constraints (guard-rail for AI-assisted SQL, no
portability abstractions, no opinion-holes) are in **ADR 0010** (`docs/adr/`),
building on ADRs 0001–0003/0007. There are 12 ADRs total covering faithful SQL
output, dialect layer, safety, parameterization, API shape, performance,
validity boundaries, analyzer config/distribution, the mission statement, and
value-domain guards.

## Layout

| Path | Purpose |
|------|---------|
| `src/SqlArtisan/Sql/Sql.{A..W}.cs` | Public API. `static partial class Sql`, one file per **leading letter** of the function name (20 files; gaps at K, Q, V, X–Z). |
| `src/SqlArtisan/Internal/SqlPart/Expression/Function/**` | Internal function node classes (`*Function : SqlExpression`), 13 categories (see below). |
| `src/SqlArtisan/Internal/SqlBuilder/**` | Statement builders (Select/Insert/Update/Delete/Merge/With), `SqlBuildingBuffer`, validation guards. |
| `src/SqlArtisan/Internal/SqlBuilder/DbmsDialect/**` | Per-DBMS syntax (`IDbmsDialect`: `AliasQuote`, `ParameterMarker`). |
| `src/SqlArtisan/Internal/SqlPart/Keywords.cs` | All SQL keyword string constants. |
| `src/SqlArtisan/SqlBuilder/` | Public surface: `Dbms`, `DbmsResolver`, `SqlArtisanConfig`, `SqlStatement`, `SqlParameters`, `ISqlBuilder`, `ISubquery`, `OutputParameter`. |
| `src/SqlArtisan/SqlPart/` | Public types: `Clause/`, `Condition/`, `Expression/`, `FunctionArgument/`, `TableReference/`. |
| `src/SqlArtisan.Analyzers/` | Opt-in Roslyn analyzer (SQLA0001/SQLA0002). Bundled inside the main NuGet package. Targets `netstandard2.0`. |
| `src/SqlArtisan.Dapper/` | Dapper integration (sync/async SqlMapper extensions). |
| `src/SqlArtisan.TableClassGen/` | Console tool that generates table classes from a live DB (Oracle/PgSql). |
| `tests/SqlArtisan.Tests/` | xUnit unit tests. `FunctionTests.{A..W}.cs` mirror `Sql.{A..W}.cs`. |
| `tests/SqlArtisan.Analyzers.Tests/` | Analyzer unit tests (matrix coverage/integrity, config resolution, diagnostic verification). |
| `tests/SqlArtisan.IntegrationTests/` | Per-engine integration tests via Testcontainers (MySql, Oracle, PostgreSql, SqlServer, Sqlite). |
| `tests/SqlArtisan.Benchmark/` | BenchmarkDotNet comparisons vs other builders. |
| `docs/` | User-facing docs: `query-statements`, `expressions`, `functions`, `analyzer`, `cookbook`, `versioning`, plus `guides/` (Dapper quickstart, AI assistants). |
| `docs/adr/` | 12 Architecture Decision Records. |
| `llms.txt` | LLM-friendly index with raw GitHub URLs to all documentation. |
| `Directory.Build.props` | Centralized version, Source Link, AOT compatibility, analyzer mode. |

Supported DBMS (`Dbms` enum): MySql, Oracle, PostgreSql, Sqlite, SqlServer.
Default DBMS is `PostgreSql` (`SqlArtisanConfig.DefaultDbms`).

## Build & test

```bash
dotnet restore
dotnet build SqlArtisan.sln
dotnet test tests/SqlArtisan.Tests              # unit tests (xUnit)
dotnet test tests/SqlArtisan.Analyzers.Tests    # analyzer tests
dotnet format SqlArtisan.sln --verify-no-changes   # .editorconfig style gate (CI enforces this)
```

Always run `dotnet test` after changing `src/`. Tests assert the **exact** SQL
string, so any output change will surface here. Also run
`dotnet format SqlArtisan.sln` before pushing — CI fails on any `.editorconfig`
violation. The SDK is pinned by `global.json` (currently 10.0.x with
`latestPatch` roll-forward); treat CI as the authoritative format gate.

Integration tests (`tests/SqlArtisan.IntegrationTests/`) run against live
database engines via Testcontainers. They are triggered nightly and on release
— not part of the default local test workflow.

## CI

Three GitHub Actions workflows in `.github/workflows/`:

| Workflow | Trigger | What it does |
|----------|---------|-------------|
| `ci.yml` | Push to `main`, all PRs | Format check, build, unit tests (`SqlArtisan.Tests` + `Analyzers.Tests`). |
| `integration.yml` | Nightly cron, `workflow_call`, manual | Integration tests against 5 engines in parallel via Testcontainers. |
| `release.yml` | Tag push (`v*`) | Full verify → integration tests → pack & push 3 NuGet packages. |

## How to add a new SQL function (the most common task)

Adding a function touches **six** places: four kept alphabetical — the node class
(`Internal/SqlPart/Expression/Function/<Category>/<Name>Function.cs`), the keyword
in `Keywords.cs`, the public factory in `Sql.<Letter>.cs`, and the test in
`FunctionTests.<Letter>.cs` — plus two gate-enforced analyzer touch points: the
dialect-matrix entry (`src/SqlArtisan.Analyzers/DialectMatrix.cs`) and its sweep
case (`tests/SqlArtisan.IntegrationTests/Infrastructure/MatrixSweepCatalog.cs`).
The **`sa-add-sql-function` skill** walks through all six with templates and
reference implementations (`AbsFunction`, `AddMonthsFunction`, …) — follow it
for the full procedure.

Function node classes are organized into 13 categories under
`Internal/SqlPart/Expression/Function/`: Aggregate, Analytic, Character,
Comparison, Conversion, DateTime, FullTextSearch, Grouping, Json, Numeric,
OrderedSetAggregate, Sequence, StringAggregate.

## Analyzer

The Roslyn analyzer (`src/SqlArtisan.Analyzers/`) ships two diagnostics:

- **SQLA0001** — SQL construct not supported on the target dialect. Fires when a
  `Sql.*` call is unsupported for the configured DBMS.
- **SQLA0002** — Invalid analyzer configuration (unrecognized `.editorconfig`
  value).

The analyzer is bundled inside the main `SqlArtisan` NuGet package (not
shipped separately). Its dialect support matrix (`DialectMatrix.cs`) is verified
against live engines by `MatrixSweepTests` in the integration test suite.

## Conventions

This file carries only always-true invariants and the map. File-scoped
conventions live in `.claude/rules/` (auto-loaded by path when the matching
files are edited); procedures live in `.claude/skills/`. Add new conventions
there, not here — a pointer line in this list is enough.

**Rules** (`.claude/rules/`): code-comments, dbms-differences, docs-style,
guards-and-empty-states, public-api-design, sql-building-style, unit-tests.

**Skills** (`.claude/skills/`): sa-add-sql-function, sa-review-changes,
sa-review-docs, sa-run-benchmark, sa-run-integration-tests, sa-run-sql-harness,
sa-write-xml-docs.

- Style is enforced by `.editorconfig`. Match it. Key rules: 4-space indent,
  100-column line limit, explicit types (no `var`), Allman braces.
- Keep DBMS-specific syntax inside `DbmsDialect`; never branch on `Dbms` inside
  function nodes.
- Public API lives in `Sql.*.cs`, `src/SqlArtisan/SqlBuilder/`, the
  table-reference types under `src/SqlArtisan/SqlPart/TableReference/`, and
  `DbColumn`/`BindValue` under `src/SqlArtisan/SqlPart/Expression/`. Types users must
  **name** in a declaration position (`SqlExpression`, `SqlCondition`,
  `ISubquery`, `SortOrder`, `ExpressionAlias`, `CommonTableExpression`,
  `DbSequence`) live in the root namespace. Everything under `Internal/` is
  implementation detail.
- Name public members after their SQL token — **underscores are the only word
  boundaries** (`ADD_MONTHS`→`AddMonths`, `DATEADD`→`Dateadd`).
- Make invalid fluent chains uncompilable through the **return type** — the
  `sa-add-sql-function` skill has the full recipe.
- Before asserting emitted-SQL behavior in durable output, reproduce it with
  the `sa-run-sql-harness` skill.
- Update `CHANGELOG.md` for user-visible changes. Usage examples live in
  `docs/`, not in the README.
- Comment the **why**, never the **what**; keep comments short. See
  `.claude/rules/code-comments.md`.

## Git

Develop on the assigned feature branch. Do not open a PR unless explicitly asked.

Commit messages follow **Conventional Commits**: `<type>: <summary>`, where
`<type>` is one of `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`,
or `chore` (e.g. `feat: Add support for CAST(expr AS type)`).
