---
name: write-xml-docs
description: Write or revise XML doc comments (///) for SqlArtisan's public API. Use when adding/editing summaries on Sql.* factories, builder-step interfaces, or other user-facing surface — to match the project's terse, point-of-use house style (what it emits + when to use it, no design rationale) and the tag set the codebase actually uses.
---

# Writing XML docs for SqlArtisan

The reader is a SQL-savvy SqlArtisan user seeing this in IntelliSense while
writing a query. Write for that moment: what the call **emits** and **when to
use it** — nothing about how or why it is built.

Document the user-facing public surface where it adds value: `Sql.*` factories,
the `ISelectBuilder*` / `SqlBuilder/` fluent API, public expression/clause types.
Skip a doc when the name already says everything (e.g. `Rank()`, `RowNumber()`
carry none). Internal mechanics never need a doc.

## Include (point of use)

- **The emitted SQL form**, shown in `<c>...</c>` — e.g. `<c>ROLLUP(a, b)</c>`,
  `<c>CEIL(expr)</c>`. This is the highest-value line; it must equal what
  `Format` actually emits.
- **What a non-obvious argument means**, inline with `<paramref name="x"/>`.
- **When to reach for it** — dialect availability or the sibling to use instead
  (`SQL Server spells this <c>CEILING</c>; use <see cref="Ceiling(object)"/>`).

## Leave out

- **Design rationale / mechanism** — why it exists, how it is wired, ADR-level
  reasoning. (ADR numbers belong in `//` implementation comments, never in `///`
  public docs.)
- **What the type system and IntelliSense already show** — the return type, that
  a one-shot step "can't be called twice", parameter types. Don't restate the
  signature.
- **Internal type names** the user can't see or doesn't act on.

Example — the trim that motivated this skill:

```csharp
// Before (mechanism the compiler already enforces):
/// Appends MySQL's <c>WITH ROLLUP</c> suffix ... Returns
/// <see cref="ISelectBuilderWithRollup"/>, which does not offer <c>WithRollup()</c>,
/// so the suffix cannot be applied twice.

// After (what the caller needs):
/// Appends MySQL's <c>WITH ROLLUP</c> suffix to the <c>GROUP BY</c> clause
/// (<c>GROUP BY a, b WITH ROLLUP</c>). MySQL's grouping syntax; on other dialects
/// use the standard <c>Sql.Rollup(...)</c> function form.
```

## House style (match the codebase)

- Tags used: `<summary>`, `<c>` (SQL tokens / code), `<paramref name="x"/>`,
  `<see cref="..."/>`, and `<remarks>` **only** for a genuine caveat or
  cross-reference. Do **not** use `<param>`, `<returns>`, `<example>`, or `<list>`
  — the codebase uses none; describe args inline with `<paramref>` instead.
- The summary's **first sentence is a noun phrase naming the construct** — it
  shows in completion lists. "The `<c>CEIL(expr)</c>` function (smallest integer
  not less than <paramref name="expr"/>)."
- Put dialect caveats and "use X instead" pointers in `<remarks>`; keep the
  `<summary>` to the construct and its emitted form.

Reference docs to copy from: `Sql.C.cs` — `Ceil`/`Ceiling` (`<remarks>` +
`<see cref>` cross-reference) and `Cast` (`<paramref>` inside the summary).

## Accuracy (it must match the source)

- The form in `<c>` equals real output. If a doc says `ROLLUP(a, b)`, a harness
  run must produce exactly that (use the `run-sql-harness` skill to confirm).
- **When behavior changes, update the doc in the same change.** A stale promise
  (e.g. "throws at build time" after the throw was removed) is a review finding.
- Every `<see cref="..."/>` must resolve — unresolved crefs are CS1574 warnings,
  and the core lib builds at the **0-warning** bar.

## Validate

```bash
dotnet build src/SqlArtisan/SqlArtisan.csproj -c Release   # 0 warnings (incl. cref CS1574)
```
