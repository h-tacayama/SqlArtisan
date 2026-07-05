---
description: Unit test conventions for SqlArtisan (naming, dialect, assertions)
paths:
  - "tests/**/*.cs"
---

# Writing unit tests

- **Name** every test `<Subject>[_<Dbms>][_<Condition>]_<Expectation>`:
  - `<Subject>` — the function / construct / method under test.
  - `<Dbms>` — *only* for dialect-specific tests; placed immediately after the
    subject and spelled exactly as the `Dbms` enum (`MySql`, `Oracle`,
    `PostgreSql`, `Sqlite`, `SqlServer`). No `For` prefix; never `MySQL`/`SQLite`.
  - `<Condition>` — the input / scenario (`NumericValue`, `WithWhere`,
    `DistinctSeparator`); omit when there is nothing to qualify.
  - `<Expectation>` — required tail: `CorrectSql` (exact SQL-string assertion),
    `ThrowsArgumentException` / `ThrowsArgumentNullException`, `Returns<X>`
    (Dapper), a clause-elision assertion (`Omits<Clause>`, e.g.
    `Where_AllConditionsExcluded_OmitsWhereClause`), or a specific behavior
    (`UsesRowAlias`, `EscapesLiteral`).
  - e.g. `Abs_NumericValue_CorrectSql`, `Extract_Oracle_CorrectSql`,
    `Returning_NoArguments_ThrowsArgumentException`.
- **Guard and elision assertions** (forward convention — applies to the
  #236/#245 guards as they land; tests written before it assert only the
  exception type or use `Contains`, don't copy them). A `Throws...` test
  asserts the exception's
  **exact message** (`Assert.Equal` on `ex.Message`) — the message grammar in
  the guards rule is part of the contract, not incidental wording. An
  `Omits<Clause>` test asserts the **exact full SQL string** without the
  clause; never a `Contains`/negation check, which passes on garbage. Where a
  Build()-time builder guard is added, also cover the legal twin: building a
  *finished* chain twice (multi-dialect) asserts equal text, while a stage
  call after `Build()` asserts the guard throw (#245).
- **Build with the dialect the SQL targets.** A test that asserts
  dialect-specific tokens (Oracle `SYSDATE`, SQL Server `DATEADD`, MySQL
  `GROUP_CONCAT`, …) must `.Build(Dbms.X)`, never the default `.Build()` —
  otherwise it asserts SQL that cannot run on the nominal (PostgreSql) dialect.
  Markers / quotes per dialect: Oracle / PostgreSql / Sqlite use `:`-params and
  `"`-quotes (the asserted string is unchanged when only the dialect is
  declared); SQL Server uses `@`; MySQL uses `?` and backticks (update the
  expected string accordingly). PG-valid tokens (`TO_CHAR`, numeric `TRUNC`,
  `NEXTVAL('seq')`, `DATE_TRUNC`, …) stay on the default `.Build()`.
- **Assert the exact SQL** built with a `StringBuilder`; also assert
  `sql.Parameters` whenever a literal becomes a bind value (literals render as
  `:0`, `:1`, … and land in `Parameters`).
- `FunctionTests.{A..W}.cs` mirror `Sql.{A..W}.cs`; put a function's tests in the
  file for its leading letter (`public partial class FunctionTests`).
- Run `dotnet test tests/SqlArtisan.Tests` and `dotnet format SqlArtisan.sln`
  after changing tests; both gate CI.
