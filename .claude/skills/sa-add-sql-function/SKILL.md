---
name: sa-add-sql-function
description: Add a new SQL function to the SqlArtisan query builder. Use when the user wants to add/implement/expose a SQL function (e.g. ABS, COALESCE, TRIM, an aggregate, a date function) in the public `Sql` API. Walks through the required touch points (node class, keyword, public factory, test, the analyzer's dialect matrix, and its integration-test sweep case) following the project's alphabetical-partial-class conventions. Also covers adding a fluent builder step / clause modifier (e.g. a GROUP BY suffix) and the type-safety practice of narrowing the return interface so invalid chains fail to compile.
---

# Add a new SQL function to SqlArtisan

Adding a SQL function touches **six** places: four core ones kept in
alphabetical order (node class, keyword, factory, tests), plus the analyzer's
dialect matrix and its integration-test sweep catalog — both gate-enforced, so
skipping either fails the test suite. Work through them in order, then
validate with format + test.

Reference implementations to copy from:
- **Single argument** → `AbsFunction` (`NumericFunction/AbsFunction.cs`)
- **Multiple required args** → `AddMonthsFunction` (`DateTimeFunction/AddMonthsFunction.cs`)
- **Optional trailing args** → `RtrimFunction` (`CharacterFunction/RtrimFunction.cs`)
- **Conditional / keyword-in-args** → `TrimFunction` (`CharacterFunction/TrimFunction.cs`)

Before starting, decide the **category** and **name**. Naming has three
categories — SQL-token names (the rule below), glyph names for symbol-only
operators (`JsonArrow` for `->`), and invented names for non-token helpers
(`ConditionIf`, `Hints`) — see `.claude/rules/public-api-design.md` for when
each applies and why token-like inventions (`CountAll`) are rejected. For a
token name:
- Pick the function `<Category>` folder under
  `src/SqlArtisan/Internal/SqlPart/Expression/Function/`. Existing categories:
  `AggregateFunction`, `AnalyticFunction`, `CharacterFunction`,
  `ComparisonFunction`, `ConversionFunction`, `DateTimeFunction`,
  `NumericFunction`, `OrderedSetAggregateFunction`, `SequenceFunction`.
- `<Name>` is derived from the SQL token by treating **underscores as the only
  word boundaries**: capitalize the first letter of each underscore-delimited
  segment and lowercase the rest. A token with **no** underscore becomes a
  single capitalized word — do **not** invent internal capitals.
  - `ADD_MONTHS` → `AddMonths`, `ROW_NUMBER` → `RowNumber`, `DATE_TRUNC` → `DateTrunc`
  - `DATEPART` → `Datepart`, `CURRVAL` → `Currval`, `SYSTIMESTAMP` → `Systimestamp`
  - `DATEADD` → `Dateadd`, `DATEDIFF` → `Datediff` (no underscore ⇒ no internal capital)
- `<Letter>` is the leading letter of `<Name>`.

## 1. Node class

Create `src/SqlArtisan/Internal/SqlPart/Expression/Function/<Category>/<Name>Function.cs`.

```csharp
namespace SqlArtisan.Internal;

public sealed class <Name>Function : SqlExpression
{
    private readonly SqlExpression _expr;

    internal <Name>Function(SqlExpression expr)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.<Name>)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
```

Rules:
- `public sealed class`, derives from `SqlExpression`, constructor is `internal`.
- Take `SqlExpression` (or `SqlExpression?` for optional) args — never raw
  `object`; resolution happens in the factory (step 3).
- Emit through the fluent `SqlBuildingBuffer` only. **Never** branch on `Dbms`
  here — DBMS syntax differences belong in `DbmsDialect`.

`SqlBuildingBuffer` cheat sheet (most-used):
- `.Append(SqlPart)` / `.Append(string?)` — append a node or literal token
- `.OpenParenthesis()` / `.CloseParenthesis()` — `(` and `)`
- `.PrependComma(part)` — `, ` then the part (for the 2nd+ required arg)
- `.PrependCommaIfNotNull(part)` — same, only if the arg is non-null (optional args)
- `.AppendSpace()` / `.AppendSpace(part)` / `.AppendSpaceIfNotNull(part)`
- `.EncloseInSpaces(op)` — ` op ` (a keyword/operator between two operands)
- `.PrependSpaceIfNotNull(part-or-string)` — space + token, only when present
  (for a flag: `flag ? Keywords.X : null`)

Const keyword interpolations may carry their own edge spaces
(`$"{Keywords.Where} "`); the helpers cover spacing a const string cannot — a
part between operands, an optional token, a runtime value. Full rules (incl.
one-token keyword constants and one-method-per-line wrapped chains) live in
`.claude/rules/sql-building-style.md` (auto-loaded when editing `Internal/**`).

For 3+ args with several optional ones, see `RegexpCountFunction` which delegates
to `VariadicFunctionCore`.

## 2. Keyword

Add the SQL token to `src/SqlArtisan/Internal/SqlPart/Keywords.cs`, keeping
alphabetical order:

```csharp
internal const string <Name> = "<SQL_TOKEN>";   // e.g. AddMonths = "ADD_MONTHS";
```

Reuse an existing keyword constant if the token already exists.

**One SQL token per constant — never a spaced phrase.** A multi-word construct
(`IN BOOLEAN MODE`, `GROUP BY`) is composed at the use site from single-token
atoms via const interpolation: `$"{Keywords.In} {Keywords.Boolean} {Keywords.Mode}"`
(compile-time folded, so it costs nothing). See `.claude/rules/sql-building-style.md`.

## 3. Public factory

Add a `public static` factory to `src/SqlArtisan/Sql/Sql.<Letter>.cs` (create the
file if no function starts with that letter yet — mirror the header of an
existing `Sql.*.cs`). Insert in alphabetical order within the file.

```csharp
public static <Name>Function <Name>(object expr) => new(Resolve(expr));

// multiple args:
public static <Name>Function <Name>(object a, object b) =>
    new(Resolve(a), Resolve(b));

// DISTINCT overload (aggregates), see Avg in Sql.A.cs:
public static <Name>Function <Name>(DistinctKeyword distinct, object expr) =>
    new(distinct, Resolve(expr));
```

Rules:
- Public factory params are `object` (accepts columns, literals, nested
  expressions); wrap **every** value arg with `Resolve(...)`
  (`using static SqlArtisan.Internal.ExpressionResolver;` is at the top of each
  `Sql.*.cs`).
- Return the concrete `<Name>Function` type, not `SqlExpression`.
- Keep the signature (and expression body) on as few lines as fit within
  **100 columns** — `Cast` / `Currval` / the JSON factories are the shape to
  copy; wrap one parameter per line only when a line would exceed 100 (#209).
- Adding a guard (empty `params`, empty collection, mandatory argument)?
  Follow `.claude/rules/guards-and-empty-states.md` — the eager vs
  Build()-time timing decision and the message grammar live there.

## 4. Test

**Probe first, then pin.** Before writing the tests, run the construct through
the `sa-run-sql-harness` skill: print `Build(Dbms.X)` for every target dialect
and read the real output — the #225 follow-up corrected seven from-memory
claims this way. Paste the probe output into the issue/PR, then pin those
exact strings as the tests.

Add `[Fact]`(s) to `FunctionTests.<Letter>.cs` (a `public partial class
FunctionTests`) covering the basic case and each overload (multi-arg, `DISTINCT`,
optional arg present/absent). Mirror the existing tests and follow
`.claude/rules/unit-tests.md` for the conventions — naming, dialect-specific
`Build(Dbms.X)`, and exact-SQL `StringBuilder` + `Parameters` assertions.

## 5. Dialect matrix entry (gate-enforced)

Every new public member needs an entry in
`src/SqlArtisan.Analyzers/DialectMatrix.cs` — an all-`true` row for a
universal function, a restricted row when it is only *meaningful* on some
dialects (an "Oracle syntax" / "MySQL, SQLite" style XML remark — the
ADR 0001/0003 case, not a `DbmsDialect` token swap) so `SQLA0001` (#93) can
warn about it. `DialectMatrixCoverageTests` fails the suite when a public
member has neither an entry nor a documented exclusion there. Key the entry
by the C# member name (add an `_arity<N>`-suffixed key alongside the
member-wide one only if support genuinely differs by overload, e.g.
`StringAgg`'s 3-arg inline-`ORDER BY` form vs. its 2-arg form — see the
file's own doc comment for the full key scheme and its collision caveat).

**When support differs by argument count on a `params` method, split the
overloads first.** The analyzer computes arity from the *declared* parameter
count, so a single `params` overload reports one arity for every call site
and can never carry an arity-restricted entry. Declare e.g. `(object, object)`
and `(object, object, object, params object[])` so the declared arities
differ, then key the entries per arity — the `Concat` split (#234) is the
worked example, `Grouping` (#235) the first from-scratch use. Full recipe in
`.claude/rules/public-api-design.md`.
Cite the primary source (the XML remark,
`docs/functions.md`/`docs/expressions.md`, a `CHANGELOG.md` entry, or a test)
in a comment next to the entry — do not guess a `false` without one, since a
wrong `false` is exactly the false positive the matrix exists to avoid.

If the function's dialect support depends on the *runtime value* of an
argument rather than its arity or declared type (`Trunc`'s numeric-vs-date/time
argument is the existing example — same overload, disjoint dialect sets), the
current matrix key shape cannot express it safely; leave it unentered rather
than assert a partial truth, and add it to `DialectMatrixCoverageTests`'
exclusion list with the reason, the way `Trunc` is.

## 6. Sweep case (gate-enforced)

Every matrix entry needs a statement in
`tests/SqlArtisan.IntegrationTests/Infrastructure/MatrixSweepCatalog.cs`
exercising exactly that construct — the integration-test dialect sweep runs
it on all five engines and asserts the accept/reject outcome matches the
matrix in both directions, so a wrong entry is caught by a live engine, not
a user. `MatrixSweepCatalogTests` fails the suite when an entry has neither
a sweep case nor a documented `ExcludedEntries` reason. Copy a nearby shape
(`Scalar(...)` for a plain function) and keep the statement minimal — one
construct per case.

## DBMS-specific syntax

If the function's *syntax* (not semantics) differs per DBMS — quoting,
parameter marker, pagination-style tokens — extend `IDbmsDialect` and the
per-DBMS dialect classes under
`src/SqlArtisan/Internal/SqlBuilder/DbmsDialect/`, then read the value from the
buffer/dialect inside `Format`. Do **not** rewrite the user's SQL to make it
portable — that is a deliberate non-goal. If a function simply doesn't exist on
a DBMS, that's fine; SqlArtisan does not emulate it.

A dialect flag is only for a **token** swap that doesn't change the statement's
shape. If the per-dialect form composes differently or sits in a different
position (a *construct*, not a token — e.g. `WITH ROLLUP` vs `ROLLUP(...)`),
expose it as a separate per-dialect method instead of one rewritten call. The
`dbms-differences` rule (auto-loaded when editing the dialect layer) has the test
and the worked example.

## Adding a fluent builder step (not a function)

Some additions are not `Sql.*` functions but **builder steps** — a clause or
modifier chained on the statement builder (e.g. `.GroupBy(...).WithRollup()`).
These touch the `ISelectBuilder*` interfaces and `SelectBuilder` under
`src/SqlArtisan/Internal/SqlBuilder/Select/`, not the four function touch points;
a new internal `*Clause : SqlPart` renders the tokens. Keep the
implemented-interface list and the members alphabetical (see CLAUDE.md).

**Make invalid chains uncompilable through the return type.** Don't return the
same builder and trust the caller not to misuse it — return a *narrowed*
interface that omits the now-invalid methods:

- A **one-shot** step (must not repeat — e.g. MySQL `WITH ROLLUP`): have it return
  a narrower step interface that omits the method. Follow the builder's flat
  step-interface style (each interface inherits only capability bases like
  `ISqlBuilder` / `IPagination` and declares its own forward methods — no
  step-to-step inheritance): declare `ISelectBuilderWithRollup` with the valid
  continuations (Having / OrderBy / pagination / Build) but no `WithRollup()`, and
  have `WithRollup()` return it. Then `.WithRollup().WithRollup()` is a compile
  error while every valid continuation stays.
- A **mandatory** trailing clause: use the two-type "pending" pattern — the
  pending type is **not** a `SqlExpression`, so omitting the clause fails at
  `Select(...)` (e.g. `ListaggFunction` → `ListaggWithinGroupFunction`).

Verify the guard the way you verify SQL: a throwaway with the bad chain behind
`#if BAD` must fail to compile (CS1061), per the `sa-run-sql-harness` skill.

## Validate

```bash
dotnet format SqlArtisan.sln --verify-no-changes   # style (.editorconfig)
dotnet test tests/SqlArtisan.Tests                 # exact-SQL assertions
dotnet test tests/SqlArtisan.Analyzers.Tests       # coverage gate: every public member has a matrix entry or documented exclusion; integrity gate: every entry resolves to a real member
dotnet test tests/SqlArtisan.IntegrationTests --filter "Engine=Sqlite|FullyQualifiedName~MatrixSweepCatalogTests"  # sweep-catalog completeness + the one engine that runs without Docker
```

All must pass. Then, for user-visible additions:
- Add an entry to `CHANGELOG.md`.
- Add the reference entry to `docs/functions.md` (or `docs/expressions.md` /
  `docs/query-statements.md` for non-function surface) and keep
  `docs/README.md`'s index in page order — `DocsIndexTests` fails the suite
  on a missing index link. Touch `README.md` only if the capability map
  itself changes; usage examples live in `docs/`, not the README.
