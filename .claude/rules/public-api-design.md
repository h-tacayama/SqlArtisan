---
description: Public API design decisions — naming categories, overload split for analyzer arity, collection parameters, no opinion-holes
paths:
  - "src/SqlArtisan/Sql/*.cs"
  - "src/SqlArtisan/SqlPart/**/*.cs"
  - "src/SqlArtisan.Analyzers/DialectMatrix.cs"
  - "tests/SqlArtisan.IntegrationTests/Infrastructure/MatrixSweepCatalog.cs"
---

# Public API design

Decision principles distilled from the #225 expressibility triage
(#231–#245). The mechanical six-touch-point procedure stays in the
`sa-add-sql-function` skill; this rule covers the *decisions* made before it.

## Naming: three categories, in priority order

1. **SQL-token names** — underscores are the only word boundaries: each
   underscore-delimited segment gets one leading capital and the rest
   lowercase; a token with no underscore stays a single word
   (`ADD_MONTHS`→`AddMonths`, `DATETRUNC`→`Datetrunc`, never invented
   internal capitals). CLAUDE.md carries the one-line summary; this is the
   full rule.
2. **Symbol-only operators → glyph names**, precedent `JsonArrow` (`->`) /
   `JsonArrowText` (`->>`); the `||` factory (#234) follows this category.
3. **Non-token helpers** (`ConditionIf`, `Hints`, `Group`) — invented names
   are allowed *only* here, for API affordances that correspond to no SQL
   token.

Never invent a token-like name for a real construct: `CountAll()` was rejected
(#233) because no `COUNT_ALL` token exists — `COUNT(*)` lands as a
parameterless `Count()` overload (#233, not yet implemented), and real tokens
like SQL Server's `COUNT_BIG` keep their conventional names available.

## Overload split for analyzer arity

The analyzer keys the dialect matrix by the **declared** parameter count
(`method.Parameters.Length`), so a single `params` overload can never carry an
arity-restricted matrix entry — every call site reports the same declared
arity. When dialect support differs by argument count, **split the overloads
so the declared arities differ**:

> Decided shape (#234 — **not yet landed**; today `Concat` is still one
> `params` method and its matrix entry is a support union, see the caveat
> comment on it in `DialectMatrix.cs`): `Concat(object, object)` (arity 2,
> all dialects) + `Concat(object, object, object, params object[])` (arity 4,
> `oracle: false`) lets `("Concat", 2)` / `("Concat", 4)` matrix entries warn
> on Oracle's 2-argument limit with zero analyzer-engine changes. `Grouping`
> (#235) is decided as the first from-scratch use.

Before adding a matrix entry, check the `MatrixKey` collision caveat in
`DialectMatrix.cs`: keys are (name, arity) with **no parameter types**, so
same-name same-arity overloads collide into a support union.

## Collection parameters: `IReadOnlyCollection<T>`, not `IEnumerable<T>`

> Worked example — do not repeat (#243 ERG-07): an `In<T>(IEnumerable<T>)`
> overload would silently re-bind `x.In("abc")` — today one bind via
> `params object[]` — into three `char` binds, because `string` implements
> `IEnumerable<char>` and a generic method in normal form beats `params`
> expansion. `string` does **not** implement `IReadOnlyCollection<char>`,
> while `List<T>`, arrays, and sets all do — so `IReadOnlyCollection<T>`
> keeps strings on the `params` path and still covers every runtime
> collection.

## Opinions live in docs and the analyzer, not in API holes

Never omit a legitimate SQL spelling to steer users toward a "better" one —
the `COUNT(*)` lesson (#233, #232): knowledge encoded as API absence is
invisible, unexplained, and unsuppressible, and the adoption test is binary.
Emit faithfully; put the guidance in `docs/`; let the matrix warn where a
dialect genuinely rejects the construct.
