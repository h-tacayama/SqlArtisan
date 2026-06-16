---
name: add-sql-function
description: Add a new SQL function to the SqlArtisan query builder. Use when the user wants to add/implement/expose a SQL function (e.g. ABS, COALESCE, TRIM, an aggregate, a date function) in the public `Sql` API. Walks through the four required touch points (node class, keyword, public factory, test) following the project's alphabetical-partial-class conventions.
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
- `.AppendIf(bool, string)` — conditional token

For 3+ args with several optional ones, see `RegexpCountFunction` which delegates
to `VariadicFunctionCore`.

## 2. Keyword

Add the SQL token to `src/SqlArtisan/Internal/SqlPart/Keywords.cs`, keeping
alphabetical order:

```csharp
internal const string <Name> = "<SQL_TOKEN>";   // e.g. AddMonths = "ADD_MONTHS";
```

Reuse an existing keyword constant if the token already exists.

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

## 4. Test

Add `[Fact]`(s) to `tests/SqlArtisan.Tests/FunctionTests/FunctionTests.<Letter>.cs`
(create it as `public partial class FunctionTests` if missing). Build the expected
SQL with a `StringBuilder` and assert the **exact** string — mirror the existing
tests:

```csharp
[Fact]
public void <Name>_<Scenario>_CorrectSql()
{
    SqlStatement sql =
        Select(<Name>(_t.Code))
        .Build();

    StringBuilder expected = new();
    expected.Append("SELECT ");
    expected.Append("<SQL_TOKEN>(\"t\".code)");

    Assert.Equal(expected.ToString(), sql.Text);
}
```

Cover: the basic case, each overload (e.g. multi-arg, `DISTINCT`, optional arg
present/absent), and assert `sql.Parameters` when a literal becomes a bind value
(literals render as `:0`, `:1`, … and land in `Parameters`).

## DBMS-specific syntax

If the function's *syntax* (not semantics) differs per DBMS — quoting,
parameter marker, pagination-style tokens — extend `IDbmsDialect` and the
per-DBMS dialect classes under
`src/SqlArtisan/Internal/SqlBuilder/DbmsDialect/`, then read the value from the
buffer/dialect inside `Format`. Do **not** rewrite the user's SQL to make it
portable — that is a deliberate non-goal. If a function simply doesn't exist on
a DBMS, that's fine; SqlArtisan does not emulate it.

## Validate

```bash
dotnet format SqlArtisan.sln --verify-no-changes   # style (.editorconfig)
dotnet test tests/SqlArtisan.Tests                 # exact-SQL assertions
```

Both must pass. Then, for user-visible additions:
- Add an entry to `CHANGELOG.md`.
- Document the function in `README.md` if it belongs in a usage section.
