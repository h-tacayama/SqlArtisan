# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this project is

**SqlArtisan** is a type-safe SQL query builder for C# (.NET 8). You write
SQL-like C# and it produces the SQL string plus its bind parameters.

**Core design philosophy — read this before proposing changes:**
> "The SQL you write is the SQL that runs. Cross-database portability is a
> deliberate non-goal."

Do **not** introduce abstractions whose purpose is to make one query run
unchanged across multiple DBMS. DBMS differences are handled only where the
*syntax* genuinely differs (quoting, parameter markers, pagination), via the
dialect layer — never by rewriting the user's SQL semantics.

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

The SDK is pinned by `global.json` (CI installs exactly that via
`setup-dotnet`'s `global-json-file`), because analyzer/`dotnet format`
behavior differs across SDK feature bands — an unpinned local SDK can pass
`--verify-no-changes` while CI fails it. If your environment cannot install
the pinned band, run the commands from **outside** the repo directory with
explicit project paths (SDK resolution reads `global.json` upward from the
current directory), and treat CI as the authoritative format gate.

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

- Style is enforced by `.editorconfig` (4-space indent, Allman braces,
  explicit types over `var` unless the type is apparent, `Nullable` enabled,
  implicit usings). Match it.
- Keep DBMS-specific syntax inside `DbmsDialect`; never branch on `Dbms` inside
  function nodes.
- Public API lives in `Sql.*.cs`, `src/SqlArtisan/SqlBuilder/`, and the
  table-reference types under `src/SqlArtisan/SqlPart/TableReference/`
  (`DbTableBase`/`DbTable`, `CteBase`/`Cte`, `DerivedTableBase`/`DerivedTable`, `DbColumn`);
  everything under `Internal/` is implementation detail.
- Name a function (factory method, `*Function` node, keyword constant) after its
  SQL token, treating **underscores as the only word boundaries**: each
  underscore-delimited segment gets one leading capital, the rest lowercase; a
  token with no underscore stays a single word — never invent internal capitals.
  So `ADD_MONTHS`→`AddMonths`, `DATE_TRUNC`→`DateTrunc`, but `DATEPART`→`Datepart`,
  `CURRVAL`→`Currval`, and `DATEADD`/`DATEDIFF`→`Dateadd`/`Datediff`.
- Order a statement builder's **implemented-interface list** and its **member
  methods** alphabetically by name (e.g. `DeleteBuilder`, `UpdateBuilder`).
  Properties precede methods and the `protected` build hook trails; overloads
  stay adjacent and explicit interface implementations sort by their simple
  name. Within an interface definition, declare members alphabetically too. This
  is mechanical and keeps builders consistent as they grow.
- Make invalid fluent chains uncompilable through the **return type**, rather than
  returning the same builder and trusting the caller. A one-shot step returns a
  *narrowed* interface that omits it (e.g. `WithRollup()` returns
  `ISelectBuilderWithRollup`, so `.WithRollup().WithRollup()` is a compile error);
  a mandatory trailing clause uses the two-type "pending" pattern (the pending
  type is not a `SqlExpression`, so omitting it fails at `Select(...)`). The
  `sa-add-sql-function` skill has the full recipe.
- Unit test conventions (naming grammar, dialect-specific `Build`, exact-SQL
  assertions) live in `.claude/rules/unit-tests.md` — auto-loaded when editing
  `tests/**`.
- Update `CHANGELOG.md` for user-visible changes. The **README is the
  landing/overview plus a capability-map index**; the **API reference lives in
  `docs/`** — `docs/README.md` (reference home), `docs/query-statements.md`,
  `docs/expressions.md`, `docs/functions.md`. Keep usage examples, expressions,
  and functions in `docs/`, not the README. `/llms.txt` is the AI-tool entry
  point (an index of the docs); keep its links in sync when docs move
  (`llms-full.txt` and an MCP/Context7 feed are future work).
- README→docs links must be **absolute GitHub URLs** (nuget.org, which renders
  the bundled README, does not resolve relative links); `llms.txt` uses
  `raw.githubusercontent.com` URLs. In-page anchors stay relative.
- List DBMS in documentation in `Dbms` enum order:
  **MySQL, Oracle, PostgreSQL, SQLite, SQL Server**.
- Documentation prose/style conventions (terminology, DBMS naming, spaced em
  dash, reference-entry shape) live in `.claude/rules/docs-style.md` —
  auto-loaded when editing `README.md`, `docs/**`, `llms.txt`, or `CHANGELOG.md`.

## Git

Develop on the assigned feature branch. Do not open a PR unless explicitly asked.

Commit messages follow **Conventional Commits**: `<type>: <summary>`, where
`<type>` is one of `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`,
or `chore` (e.g. `feat: Add support for CAST(expr AS type)`).
