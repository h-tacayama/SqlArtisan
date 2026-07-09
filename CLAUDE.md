# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this project is

**SqlArtisan** is a type-safe SQL query builder for C# (.NET 8). You write
SQL-like C# and it produces the SQL string plus its bind parameters.

**Core design philosophy — read this before proposing changes:**
> "The SQL you write is the SQL that runs. Cross-database portability is a
> deliberate non-goal."

The broader mission and constraints (guard-rail for AI-assisted SQL, no
portability abstractions, no opinion-holes) are in **ADR 0010** (`docs/adr/`),
building on ADRs 0001–0003/0007.

## Layout

| Path | Purpose |
|------|---------|
| `src/SqlArtisan/Sql/Sql.{A..W}.cs` | Public API. `static partial class Sql`, one file per **leading letter** of the function name. |
| `src/SqlArtisan/Internal/SqlPart/Expression/Function/**` | Internal function node classes (`*Function : SqlExpression`). |
| `src/SqlArtisan/Internal/SqlBuilder/**` | Statement builders (Select/Insert/Update/Delete/With) + `SqlBuildingBuffer`. |
| `src/SqlArtisan/Internal/SqlBuilder/DbmsDialect/**` | Per-DBMS syntax (`IDbmsDialect`: `AliasQuote`, `ParameterMarker`). |
| `src/SqlArtisan/Internal/SqlPart/Keywords.cs` | All SQL keyword string constants. |
| `src/SqlArtisan/SqlBuilder/` | Public surface: `Dbms`, `DbmsResolver`, `SqlArtisanConfig`, `SqlStatement`, `SqlParameters`, `ISqlBuilder`. |
| `src/SqlArtisan.Dapper/` | Dapper integration. |
| `src/SqlArtisan.TableClassGen/` | Console tool that generates table classes from a live DB (Oracle/PgSql). |
| `tests/SqlArtisan.Tests/` | xUnit tests. `FunctionTests.{A..W}.cs` mirror `Sql.{A..W}.cs`. |
| `tests/SqlArtisan.Benchmark/` | BenchmarkDotNet comparisons vs other builders. |

Supported DBMS (`Dbms` enum): MySql, Oracle, PostgreSql, Sqlite, SqlServer.
Default DBMS is `PostgreSql` (`SqlArtisanConfig.DefaultDbms`).

## Build & test

```bash
dotnet restore
dotnet build SqlArtisan.sln
dotnet test tests/SqlArtisan.Tests          # unit tests (xUnit)
dotnet format SqlArtisan.sln --verify-no-changes   # .editorconfig style gate (CI enforces this)
```

Always run `dotnet test` after changing `src/`. Tests assert the **exact** SQL
string, so any output change will surface here. Also run
`dotnet format SqlArtisan.sln` before pushing — CI fails on any `.editorconfig`
violation. The SDK is pinned by `global.json`; treat CI as the authoritative
format gate.

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

## Conventions

This file carries only always-true invariants and the map. File-scoped
conventions live in `.claude/rules/` (auto-loaded by path when the matching
files are edited); procedures live in `.claude/skills/`. Add new conventions
there, not here — a pointer line in this list is enough.

- Style is enforced by `.editorconfig`. Match it.
- Keep DBMS-specific syntax inside `DbmsDialect`; never branch on `Dbms` inside
  function nodes.
- Public API lives in `Sql.*.cs`, `src/SqlArtisan/SqlBuilder/`, the
  table-reference types under `src/SqlArtisan/SqlPart/TableReference/`, and
  `DbColumn` under `src/SqlArtisan/SqlPart/Expression/`. Types users must
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
