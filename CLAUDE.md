# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this project is

**SqlArtisan** is a type-safe SQL query builder for C# (.NET 8). You write
SQL-like C# and it produces the SQL string plus its bind parameters.

**Core design philosophy — read this before proposing changes:**
> "The SQL you write is the SQL that runs. Cross-database portability is a
> deliberate non-goal."

Faithful emission is the foundation, not the whole mission: SqlArtisan aims
to be the **deterministic guard rail for SQL written alongside generative
AI** — misuse fails to compile or throws loudly, the analyzer
deterministically flags what the target DBMS rejects (dialect availability
today; version/schema checks are the #232 direction; cost-based advice is
permanently out of scope), and exact-SQL tests plus the live-engine matrix
close the loop. The full decision is **ADR 0010** (`docs/adr/`), building on
ADRs 0001–0003/0007.

Do **not** introduce abstractions whose purpose is to make one query run
unchanged across multiple DBMS. DBMS differences are handled only where the
*syntax* genuinely differs (quoting, parameter markers, pagination), via the
dialect layer — never by rewriting the user's SQL semantics. And never omit
a legitimate SQL spelling to steer users — opinions live in docs and the
analyzer, not in API holes (ADR 0010).

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
violation.

The SDK is pinned by `global.json` — `dotnet format` behavior differs across
feature bands, so an unpinned local SDK can pass `--verify-no-changes` while
CI fails it. If the pinned band isn't installable, run the commands from
**outside** the repo directory with explicit project paths, and treat CI as
the authoritative format gate.

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

- Style is enforced by `.editorconfig` (4-space indent, Allman braces,
  explicit types over `var` unless the type is apparent, `Nullable` enabled,
  implicit usings). Match it.
- Keep DBMS-specific syntax inside `DbmsDialect`; never branch on `Dbms` inside
  function nodes.
- Public API lives in `Sql.*.cs`, `src/SqlArtisan/SqlBuilder/`, the
  table-reference types under `src/SqlArtisan/SqlPart/TableReference/`
  (`DbTableBase`/`DbTable`, `CteBase`/`Cte`, `DerivedTableBase`/`DerivedTable`,
  and their base `TableReference`), `DbColumn` under
  `src/SqlArtisan/SqlPart/Expression/`, and the types users must **name** in a
  declaration position (a helper's return type, an accumulator variable, a
  `List<>` element, a shared `static readonly` field) — `SqlExpression`,
  `SqlCondition`, `ISubquery`, `SortOrder`, `ExpressionAlias`,
  `CommonTableExpression`, `DbSequence`, `SqlHints`, `LockBehaviorBase` — all
  in the root namespace under `src/SqlArtisan/SqlPart/` /
  `src/SqlArtisan/SqlBuilder/`. The boundary rule (#244): **values, items,
  and handles move out of `Internal/` when a mainstream flow must write
  their name; clause syntax stays** — clause fragments are specified through
  the builder chain, and the reusable unit is the completed expression,
  already nameable via the roots. Every concrete node stays `Internal/` and
  is held only through these roots. Everything else under `Internal/` is
  implementation detail.
- Name public members after their SQL token — **underscores are the only word
  boundaries** (`ADD_MONTHS`→`AddMonths`, `DATEADD`→`Dateadd`, never invented
  internal capitals). The full rule, the glyph/helper naming categories, and
  the other API-shape decisions live in `.claude/rules/public-api-design.md`.
- Make invalid fluent chains uncompilable through the **return type** (narrowed
  step interfaces for one-shot steps; the two-type "pending" pattern for
  mandatory trailing clauses) — the `sa-add-sql-function` skill has the full
  recipe. Builder member/interface ordering lives in
  `.claude/rules/sql-building-style.md`.
- Unit test conventions (naming grammar, dialect-specific `Build`, exact-SQL
  assertions) live in `.claude/rules/unit-tests.md` — auto-loaded when editing
  `tests/**`.
- Guard conventions (the empty-state policy, eager vs Build()-time throws,
  message grammar) live in `.claude/rules/guards-and-empty-states.md`; public
  API design decisions (naming categories, the overload split for analyzer
  arity, collection parameters) in `.claude/rules/public-api-design.md` —
  both auto-loaded when editing the relevant sources.
- Before asserting emitted-SQL behavior anywhere durable (an issue, docs, a
  review comment), reproduce it with the `sa-run-sql-harness` skill — the
  #225 follow-up corrected seven audit-record claims this way. An assertion
  not yet probed carries the `grammar-unverified` tag.
- Update `CHANGELOG.md` for user-visible changes. The **README is the
  landing/overview plus a capability-map index**; the **API reference lives in
  `docs/`** (`docs/README.md` home + `query-statements.md` / `expressions.md` /
  `functions.md`) — keep usage examples there, not in the README; `/llms.txt`
  is the AI-tool index. Link formats (absolute GitHub URLs), DBMS
  naming/order, terminology, and the reference-entry/caveat-note shapes live
  in `.claude/rules/docs-style.md` — auto-loaded when editing `README.md`,
  `docs/**`, `llms.txt`, or `CHANGELOG.md`.

## Git

Develop on the assigned feature branch. Do not open a PR unless explicitly asked.

Commit messages follow **Conventional Commits**: `<type>: <summary>`, where
`<type>` is one of `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`,
or `chore` (e.g. `feat: Add support for CAST(expr AS type)`).
