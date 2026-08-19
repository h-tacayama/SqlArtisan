# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this project is

**SqlArtisan** is a type-safe SQL query builder for C# (.NET 8). You write
SQL-like C# and it produces the SQL string plus its bind parameters.
`Directory.Build.props` is the single source of truth for the shipped version
across all four shipped packages.

**Core design philosophy — read this before proposing changes:**
> "The SQL you write is the SQL that runs. Cross-database portability is a
> deliberate non-goal."

The broader mission and constraints (guard-rail for AI-assisted SQL, no
portability abstractions, no opinion-holes) are in **ADR 0010** (`docs/adr/`),
building on ADRs 0001–0003/0007. See `docs/adr/README.md` for the full index.

## Layout

| Path | Purpose |
|------|---------|
| `src/SqlArtisan/Sql/Sql.{A..Y}.cs` | Public API. `static partial class Sql`, one file per **leading letter** of the function name (gaps at K, Q, X, Z). |
| `src/SqlArtisan/Internal/SqlPart/Expression/Function/**` | Internal function node classes (`*Function : SqlExpression`), organized into categories (see below). |
| `src/SqlArtisan/Internal/SqlBuilder/**` | Statement builders (Select/Insert/Update/Delete/Merge/With), `SqlBuildingBuffer`, validation guards. |
| `src/SqlArtisan/Internal/SqlBuilder/DbmsDialect/**` | Per-DBMS syntax (`IDbmsDialect`: `AliasQuote`, `ParameterMarker`, `BackslashEscapesStringLiterals`, `DmlTableAliasSeparator`, `ExcludedName`, `MergeTerminator`). |
| `src/SqlArtisan/Internal/SqlPart/Keywords.cs` | All SQL keyword string constants. |
| `src/SqlArtisan/SqlBuilder/` | Public surface: `Dbms`, `DbmsResolver`, `SqlArtisanConfig`, `SqlStatement`, `SqlParameters`, `ISqlBuilder`, `ISubquery`, `OutputParameter`. |
| `src/SqlArtisan/SqlPart/` | Public types: `Clause/`, `Condition/`, `Expression/`, `FunctionArgument/`, `TableReference/`. Everything here renders SQL or is consumed while rendering it. |
| `src/SqlArtisan/Metadata/` | Schema-metadata attributes on generated table classes (`DbColumnMetadataAttribute`, `DbTypeCategory`). Compile-time data, never rendered and never read at run time. |
| `src/SqlArtisan.Analyzers/` | Opt-in Roslyn analyzer (SQLA0001–SQLA0300). Bundled inside the main NuGet package. Targets `netstandard2.0`. |
| `src/SqlArtisan.ArrayBind/` | Oracle array-bind execution (one round trip per batch, not per row). |
| `src/SqlArtisan.Dapper/` | Dapper integration (sync/async SqlMapper extensions). |
| `src/SqlArtisan.TableClassGen/` | Argument-driven tool that generates table classes from a live DB (all five DBMS), and reports drift between them and the schema (`--check` / `--fix`). |
| `tests/SqlArtisan.Tests/` | xUnit unit tests. `FunctionTests.{A..Y}.cs` mirror `Sql.{A..Y}.cs`. |
| `tests/SqlArtisan.Analyzers.Tests/` | Analyzer unit tests (matrix coverage/integrity, config resolution, diagnostic verification). |
| `tests/SqlArtisan.IntegrationTests/` | Per-engine integration tests: MySql, Oracle, Oracle23ai, PostgreSql, SqlServer via Testcontainers; Sqlite in-process. |
| `tests/SqlArtisan.TableClassGen.Tests/` | TableClassGen unit tests (catalog reading, emitted code, drift detection). |
| `tests/SqlArtisan.Benchmark/` | BenchmarkDotNet comparisons vs other builders. |
| `docs/` | User-facing docs: `query-statements`, `expressions`, `functions`, `analyzer`, `cookbook`, `comparison`, `versioning`, plus `guides/` (Dapper quickstart, AI assistants, Oracle array bind). |
| `docs/adr/` | Architecture Decision Records (see `docs/adr/README.md` for the index). |
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
dotnet test tests/SqlArtisan.TableClassGen.Tests   # TableClassGen tests
dotnet format SqlArtisan.sln --verify-no-changes   # .editorconfig style gate (CI enforces this)
```

Always run `dotnet test` after changing `src/`. Tests assert the **exact** SQL
string, so any output change will surface here. Also run
`dotnet format SqlArtisan.sln` before pushing — CI fails on any `.editorconfig`
violation. The SDK version is pinned by `global.json` (`latestPatch`
roll-forward); treat CI as the authoritative format gate.

Integration tests (`tests/SqlArtisan.IntegrationTests/`) run against live
database engines — containers via Testcontainers, SQLite in-process. They are
triggered nightly and on release — not part of the default local test workflow.

## CI

Three GitHub Actions workflows in `.github/workflows/`:

| Workflow | Trigger | What it does |
|----------|---------|-------------|
| `ci.yml` | Push to `main`, all PRs | Format check, build, unit tests (`SqlArtisan.Tests`, `Analyzers.Tests`, `TableClassGen.Tests`), and the DB-less `MatrixSweepCatalogTests` completeness slice. |
| `integration.yml` | Nightly cron, `workflow_call`, manual | Integration tests across 6 lanes in parallel (Oracle runs at both 21c and 23ai). |
| `release.yml` | Tag push (`v*`) | Full verify → integration tests → pack & push 4 NuGet packages. |

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

Most function node classes sit in a category folder under
`Internal/SqlPart/Expression/Function/`, named with the `Function` suffix:
`AggregateFunction`, `AnalyticFunction`, `ArrayFunction`, `CharacterFunction`,
`ComparisonFunction`, `ConversionFunction`, `DateTimeFunction`,
`FullTextSearchFunction`, `GroupingFunction`, `JsonFunction`, `NumericFunction`,
`OrderedSetAggregateFunction`, `SequenceFunction`, `StringAggregateFunction`.
Shared bases and one uncategorized node sit at that folder's root.

## Analyzer

The Roslyn analyzer (`src/SqlArtisan.Analyzers/`) ships fourteen diagnostics:

- **SQLA0001** — Analyzer configuration problem: an unrecognized key name or
  value, a `sqlartisan_syntax_*` family resolving to no dialect at all, or a
  legacy pair whose DBMS a coexisting family silently drops.
- **SQLA0002** — Deprecated legacy config. The `sqlartisan_target_dbms` /
  `sqlartisan_target_version` pair still governs (no family key present) —
  migrate to `sqlartisan_syntax_<dbms>`.
- **SQLA0100** — SQL construct not supported on the target dialect. Fires when a
  `Sql.*` call is unsupported for the configured DBMS.
- **SQLA0101** — Version-bound construct. Supported on the target dialect, but
  not until a newer engine version than the one declared.
- **SQLA0102** — Context-restricted construct. A construct the target supports,
  used in a position that dialect rejects.
- **SQLA0103** — Identifier too long for the target dialect's limit.
- **SQLA0104** — A literal `DateTimePart` argument (`Extract`, `Datepart`,
  `Dateadd`, `Datediff`, `DateTrunc`, `Datetrunc`, `Interval`, `Timestampadd`,
  `Timestampdiff`) is not a value the target dialect's grammar accepts for that
  function — a finer grain than SQLA0100's whole-construct verdict. Resolved by
  matching the argument's constant value against the enum's own members (never
  the underlying integer), the same technique SQLA0205 uses for
  `DbTypeCategory`.
- **SQLA0200** — Constant NULL predicate: `IS [NOT] NULL` on a column the
  generated table class declares NOT NULL. Reported only in a statement that
  visibly builds its own query and has no outer join.
- **SQLA0201** — `NOT IN` over a subquery selecting a nullable column — one
  NULL and the query matches nothing.
- **SQLA0202** — `INSERT` column list omitting a NOT NULL column with no
  default.
- **SQLA0203** — `Count(col)` on a nullable column, which counts values rather
  than rows. Advice on correct code, so it is Info and off by default.
- **SQLA0204** — a `WHERE`/`ON` predicate that wraps an indexed column in a
  function or leads its pattern with `%`, so no index on it can be used.
- **SQLA0205** — a column compared to a value of another type category. MySQL
  reconciles the two as floating point, so the mismatch changes which rows
  match. The category is the public `DbTypeCategory` enum, which TableClassGen
  emits symbolically and the analyzer resolves by member name (never by the
  underlying integer), gated by `SchemaMetadataParityTests`.
- **SQLA0300** — Correlated `UPDATE`/`DELETE` with an unaliased target — the
  same violation `Build()` rejects, surfaced early.

Each ID sits in a numbered band that **is** its category, so a family gains a
rule without renumbering and a bulk-severity setting reaches one family only
(**ADR 0018**): `SQLA0001`–`0099` `SqlArtisan.Configuration`, `SQLA0100`–`0199`
`SqlArtisan.Dialect`, `SQLA0200`–`0299` `SqlArtisan.Schema`, `SQLA0300`–`0399`
`SqlArtisan.Validity`. A new rule takes the next ID **inside its band**, never
the next free number overall; `DiagnosticOrderingTests` gates the pairing.

SQLA0200–0205 read the `DbColumnMetadata` attributes TableClassGen emits;
absence of a fact is silence. What Tier 2 may collect and conclude —
and the parity catalog every rule reading the query must stay silent on
(`SchemaRuleParityTests`) — is fixed by **ADR 0010**; add a hazard shape
there, not to one rule's suite.

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

**Skills** (`.claude/skills/`): sa-add-sql-function, sa-diff-review,
sa-diff-review-refinement, sa-docs-audit, sa-panel-audit,
sa-panel-diff-review, sa-run-benchmark, sa-run-integration-tests,
sa-run-sql-harness, sa-write-xml-docs.

**Workflows** (`.claude/workflows/`): sa-audit-sweep. The `-sweep` suffix is
reserved for workflows — a skill never carries it.

Review-surface names **state their scope in a word of its own**: `diff` reads
what the branch changed, `audit` reads a whole scope as it stands (`docs`
names the documentation corpus). `review` already implies a change in this
register, so `diff-review` is deliberate reinforcement — the scope is meant to
be readable without knowing the convention, and the redundancy costs less than
the ambiguity would. `audit` takes no scope adjective: it already means the
whole thing, and pairing one with `review` produced the misleading
"full-review" this convention replaced.

A `sa-panel-` prefix means three independent models work it and the main agent
adjudicates; a `-refinement` suffix adds non-defect improvement findings.
Prefix says *who reviews*, suffix says *what is reported*. A panel needs a
bounded scope — the whole ~700-file codebase belongs to `sa-audit-sweep`,
which chunks it across single reviewers instead of tripling it.

- Style is enforced by `.editorconfig`. Match it. Key rules: 4-space indent,
  100-column line limit, explicit types (no `var`), Allman braces.
- Keep DBMS-specific syntax inside `DbmsDialect`; never branch on `Dbms` inside
  function nodes.
- Public API lives in `Sql.*.cs`, `src/SqlArtisan/SqlBuilder/`, the
  table-reference types under `src/SqlArtisan/SqlPart/TableReference/`,
  `DbColumn`/`BindValue` under `src/SqlArtisan/SqlPart/Expression/`, the
  function-argument enums under `src/SqlArtisan/SqlPart/FunctionArgument/`, and
  the schema-metadata attributes under `src/SqlArtisan/Metadata/`. Types users must
  **name** in a declaration position (`SqlExpression`, `SqlCondition`, `TableReference`,
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
- Comment the **why** / **why-not**, never the **what**; keep comments short. See
  `.claude/rules/code-comments.md`.
- A review finding closes only by landing somewhere durable — a gate (test),
  a rule/ADR clause, or a recorded decision not to mechanize it — never by the
  one-off fix alone; the finding's *class* is what the landing must cover.
- Report only what you are asking someone to change. Anything you would not
  change — fine as is, already covered elsewhere, worth knowing but needing no
  action — stays out entirely, under any label; **finding nothing is a good
  result**, not a shallow one. Being asked to look does not oblige you to
  return something.

## Release procedure

Bumping the shipped version (e.g. `0.8.0-beta.1` → `1.0.0`) is a user decision,
never made unprompted. Once approved, do it in one commit:

1. `Directory.Build.props`: set `<VersionPrefix>`; delete `<VersionSuffix>` for
   a non-prerelease version.
2. If leaving prerelease: remove the `--prerelease` install instructions and
   any "pre-release" notes from `README.md`, `docs/guides/dapper-quickstart.md`,
   `docs/guides/oracle-array-bind.md`, and `src/SqlArtisan.TableClassGen/README.md`.
3. `CHANGELOG.md`: finalize the `## [Unreleased]` section under the new version
   and date.
4. Regenerate `llms-full.txt` (command in `LlmsFullTests.cs`'s header comment).
5. `Directory.Build.props`: set `<PackageValidationBaselineVersion>` to the
   version being *replaced*, and delete `src/SqlArtisan/CompatibilitySuppressions.xml`
   — its entries record breaks taken against the old baseline and mean nothing
   against the new one. Regenerate it only if the new baseline still reports
   breaks: `dotnet pack src/SqlArtisan/SqlArtisan.csproj -c Release
   -p:ApiCompatGenerateSuppressionFile=true`. From 1.0 a surviving entry is a
   SemVer violation, not a record — resolve it rather than suppressing it.
6. Run the full gate set (`dotnet test` ×3, `dotnet format --verify-no-changes`).
7. Merge to `main`, then tag and push: `git tag vX.Y.Z && git push origin vX.Y.Z`
   — `release.yml` reads the version from `Directory.Build.props`, not the tag,
   so they must already agree before pushing it. Tag push is user-performed.

## Git

Develop on the assigned feature branch. Do not open a PR unless explicitly asked.

Commit messages follow **Conventional Commits**: `<type>: <summary>`, where
`<type>` is one of `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`,
or `chore` (e.g. `feat: Add support for CAST(expr AS type)`).
