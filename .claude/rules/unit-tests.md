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
    `ThrowsArgumentException` / `ThrowsArgumentNullException` (e.g.
    `Where_AllConditionsExcluded_ThrowsArgumentException`), `Returns<X>`
    (Dapper), or a specific behavior (`UsesRowAlias`, `EscapesLiteral`).
  - e.g. `Abs_NumericValue_CorrectSql`, `Extract_Oracle_CorrectSql`,
    `Returning_NoArguments_ThrowsArgumentException`.
- **Guard assertions** (forward convention, in force since e51da43c (#272) —
  the #236/#245 guards; ea8783c6 (#246) is the rules' own vintage — tests
  written before it assert only the exception type or use `Contains`, don't
  copy them; `GuardAssertionRatchetTests` pins those pre-convention sites per
  file, so a new or edited bare `Assert.Throws<Argument*>` fails the suite). A `Throws...` test asserts the exception's
  **exact message** (`Assert.Equal` on `ex.Message`) — the message grammar in
  the guards rule is part of the contract, not incidental wording. For an
  `ArgumentNullException` whose message is the runtime's, asserting
  `ex.ParamName` suffices; the ratchet accepts either. Also cover the
  legal twin: a partly-excluded condition that keeps the clause non-empty must
  still build (assert the exact SQL with the excluded operand dropped), and a
  clause omitted entirely must build; for a Build()-time builder guard, a stage
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
  `:0`, `:1`, … and land in `Parameters`). `ParameterAssertionRatchetTests`
  pins the pre-convention sites per file, as `GuardAssertionRatchetTests` does
  for bare throws (an `Assert.ThrowsAsync`, or a captured throw followed by a
  `StartsWith`/`Contains` on `.Message`, counts as bare — pin the whole fixed
  string). Those two baselines are the whole grandfathered set: a
  pre-convention test is not re-raised in review, it upgrades when its file is
  next edited for substance, and an unrelated edit elsewhere in the file does
  not lapse the rest of it. `CommentCapRatchetTests` gates the
  comment caps the same way; `LineLengthSweepTests` and `FormattingSweepTests`
  hold the 100-column limit and the brace-adjacent blank lines at zero, with no
  baseline at all, over `src/` and `tests/` alike.
- `FunctionTests.{A..Y}.cs` mirror `Sql.{A..Y}.cs`; put a function's tests in the
  file for its leading letter (`public partial class FunctionTests`).
- Run `dotnet test tests/SqlArtisan.Tests` and `dotnet format SqlArtisan.sln`
  after changing tests; both gate CI.
