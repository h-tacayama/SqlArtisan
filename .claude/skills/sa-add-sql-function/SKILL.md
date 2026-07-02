---
name: sa-add-sql-function
description: Add a new SQL function to the SqlArtisan query builder. Use when the user wants to add/implement/expose a SQL function (e.g. ABS, COALESCE, TRIM, an aggregate, a date function) in the public `Sql` API. Walks through the four required touch points (node class, keyword, public factory, test) following the project's alphabetical-partial-class conventions. Also covers adding a fluent builder step / clause modifier (e.g. a GROUP BY suffix) and the type-safety practice of narrowing the return interface so invalid chains fail to compile.
---

# Add a new SQL function to SqlArtisan

Adding a SQL function touches **four** places, all kept in alphabetical order.
Skipping any one leaves the function unusable or untested. Work through them in
order, then validate with format + test.

Reference implementations to copy from:
- **Single argument** → `AbsFunction` (`NumericFunction/AbsFunction.cs`)
- **Multiple required args** → `AddMonthsFunction` (`DateTimeFunction/AddMonthsFunction.cs`)
- **Optional trailing args** → `RtrimFunction` (`CharacterFunction/RtrimFunction.cs`)
- **Conditional / keyword-in-args** → `TrimFunction` (`CharacterFunction/TrimFunction.cs`)

Before starting, decide the **category** and **name**:
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

## 4. Test

Add `[Fact]`(s) to `FunctionTests.<Letter>.cs` (a `public partial class
FunctionTests`) covering the basic case and each overload (multi-arg, `DISTINCT`,
optional arg present/absent). Mirror the existing tests and follow
`.claude/rules/unit-tests.md` for the conventions — naming, dialect-specific
`Build(Dbms.X)`, and exact-SQL `StringBuilder` + `Parameters` assertions.

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
```

Both must pass. Then, for user-visible additions:
- Add an entry to `CHANGELOG.md`.
- Document the function in `README.md` if it belongs in a usage section.
