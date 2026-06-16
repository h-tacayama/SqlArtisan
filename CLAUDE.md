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

## How to add a new SQL function (the most common task)

Touch all four, keeping the alphabetical convention:

1. **Node class** — `src/SqlArtisan/Internal/SqlPart/Expression/Function/<Category>/<Name>Function.cs`:
   `public sealed class <Name>Function : SqlExpression`, `internal` constructor,
   `internal override void Format(SqlBuildingBuffer buffer)` that emits via the
   fluent buffer (`.Append`, `.OpenParenthesis()`, `.PrependComma()`, etc.).
2. **Keyword** — add the SQL token to `Keywords.cs` (keep alphabetical).
3. **Public factory** — add a `public static <Name>Function <Name>(object ...)`
   method to `Sql.<Letter>.cs`, wrapping args with
   `ExpressionResolver.Resolve(...)`.
4. **Test** — add `[Fact]`s to `FunctionTests.<Letter>.cs` asserting the exact
   `SqlStatement.Text` (and parameters where relevant).

Follow `AbsFunction` (single arg) and `AddMonthsFunction` (multi arg) as
reference implementations.

## Conventions

- Style is enforced by `.editorconfig` (4-space indent, Allman braces,
  explicit types over `var` unless the type is apparent, `Nullable` enabled,
  implicit usings). Match it.
- Keep DBMS-specific syntax inside `DbmsDialect`; never branch on `Dbms` inside
  function nodes.
- Public API lives only in `Sql.*.cs` and `src/SqlArtisan/SqlBuilder/`;
  everything under `Internal/` is implementation detail.
- Name a function (factory method, `*Function` node, keyword constant) after its
  SQL token, treating **underscores as the only word boundaries**: each
  underscore-delimited segment gets one leading capital, the rest lowercase; a
  token with no underscore stays a single word — never invent internal capitals.
  So `ADD_MONTHS`→`AddMonths`, `DATE_TRUNC`→`DateTrunc`, but `DATEPART`→`Datepart`,
  `CURRVAL`→`Currval`, and `DATEADD`/`DATEDIFF`→`Dateadd`/`Datediff`.
- Tests build expected SQL with a `StringBuilder` and assert equality — mirror
  that pattern.
- Update `CHANGELOG.md` for user-visible changes; the README is the canonical
  user-facing documentation.

## Git

Develop on the assigned feature branch. Do not open a PR unless explicitly asked.

Commit messages follow **Conventional Commits**: `<type>: <summary>`, where
`<type>` is one of `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`,
or `chore` (e.g. `feat: Add support for CAST(expr AS type)`).
