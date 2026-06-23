---
name: write-xml-docs
description: Write or revise XML doc comments (///) for SqlArtisan's public API. Use when adding/editing summaries on Sql.* factories, builder-step interfaces, or other user-facing surface — to match the project's terse, point-of-use house style (what it emits + when to use it, no design rationale) and its tag conventions.
---

# Writing XML docs for SqlArtisan

The reader is a SQL-savvy SqlArtisan user seeing this in IntelliSense while
writing a query. Write for that moment: what the call **emits** and **when to
use it** — nothing about how or why it is built.

## What to document — by accessibility (the .NET rule)

The codebase is not fully documented yet; **do not treat that as the standard.**
Follow the .NET convention, which keys off externally-visible accessibility
(the compiler's "publicly visible" / CS1591 concept):

- **`public` and `protected` members of `public` types — document all of them.**
  They are the published contract that drives consumer IntelliSense and reference
  docs. `GenerateDocumentationFile` enforces this as CS1591 (see "Shipping docs"),
  and it does **not** exempt "obvious" members — even a one-line summary counts,
  and an interface implementation can use `<inheritdoc/>`.
- **`internal`, `private`, `private protected`, and every member of an `internal`
  type — optional.** The compiler never requires a doc here. Add a `///` only when
  a non-obvious contract helps a maintainer (e.g. `IDbmsDialect`'s members); a plain
  `//` note is equally fine, and trivial internals need nothing. Don't copy a public
  method's doc onto the internal type it backs.

Caveat for this repo: some node types are `public` yet live in the `Internal`
namespace (a `public` factory can't return an `internal` type), so by the .NET rule
they are publicly visible and fall under "document them." If they are truly
implementation detail, the cleaner alignment is to make them `internal` — have the
factory return a public base such as `SqlExpression` — rather than leave a public
type undocumented.

(Existing summaries predate these conventions — apply them to new and edited
docs; no mass retrofit.)

## Include (point of use)

- **The emitted SQL form**, shown in `<c>...</c>` — e.g. `<c>ROLLUP(a, b)</c>`,
  `<c>CEIL(expr)</c>`. The highest-value line; it must equal what `Format` emits.
- **`<param name="x">`** for each argument — its meaning, accepted form, units, or
  constraints (e.g. "the exact SQL data type" for `Cast`'s `type`). Write
  information, not the name restated; if you add `<param>`, cover every parameter.
- **`<returns>`** — the construct or builder state the call produces.
- **`<exception cref="...">`** — the exceptions misuse raises. This API fails
  loudly (empty `GroupBy()` / `Rollup()` → `ArgumentException`, a null value →
  `ArgumentNullException`, a wrong item type → `ArgumentException`), and those are
  part of the contract, so document them.
- **When to reach for it** — dialect availability or the sibling to use instead,
  in `<remarks>` (`SQL Server spells this <c>CEILING</c>; use
  <see cref="Ceiling(object)"/>`). Reference a param inside prose with
  `<paramref name="x"/>`.

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

## House style

- Tags to use: `<summary>`, `<param>`, `<returns>`, `<exception cref>`,
  `<paramref name="x"/>`, `<see cref>`, `<remarks>` (a genuine caveat or
  cross-reference), and `<c>` for SQL tokens / code. Use `<typeparam name="T">`
  for a generic member (e.g. `SqlParameters.Get<T>`) and `<value>` for a property.
- Write language keywords as `<see langword="null"/>` / `true` / `false`, not
  `<c>null</c>` — the modern .NET idiom. A boolean `<returns>` reads
  `<see langword="true"/> if …; otherwise, <see langword="false"/>`.
- **First sentence** shows in completion lists, so make it a complete, concise
  description. For a `Sql.*` construct-producing factory use a noun phrase naming
  the construct ("The `<c>CEIL(expr)</c>` function (smallest integer not less than
  <paramref name="expr"/>)."); for an action-like member or property use the .NET
  verb-first form ("Gets the current value of a sequence …").
- Keep the `<summary>` to the construct and its emitted form; put dialect caveats
  and "use X instead" pointers in `<remarks>`.
- **Document on the interface, not the implementation.** Put a builder step's doc
  on its `ISelectBuilder*` interface. The IDE shows it on the implementation
  automatically, but the *generated XML* and DocFX do not inherit it — so when
  docs are shipped (see below) put `<inheritdoc/>` on the `SelectBuilder` method.
  Either way, never write the doc twice.
- **Keep runnable usage examples in the README** (the canonical user doc), not in
  `<example>` blocks — two copies drift. `<seealso>` is optional sugar over an
  inline `<see cref>`; `<list>` / `<para>` are rarely needed.

Reference docs to copy from: `Sql.C.cs` — `Ceil`/`Ceiling` (`<remarks>` +
`<see cref>` cross-reference) and `Cast` (`<paramref>` inside the summary).

## Accuracy (it must match the source)

- The form in `<c>` equals real output. If a doc says `ROLLUP(a, b)`, a harness
  run must produce exactly that (use the `run-sql-harness` skill to confirm).
- **When behavior changes, update the doc in the same change.** A stale promise
  (e.g. "throws at build time" after the throw was removed) is a review finding.
- Every `<see cref="..."/>` must resolve — unresolved crefs are CS1574 warnings,
  and the core lib builds at the **0-warning** bar.

## Shipping docs (project policy)

The packages are published (each csproj sets `<Description>`) but none set
`<GenerateDocumentationFile>`, so no XML doc ships — consumers get no IntelliSense
from the package, which a type-safety / IntelliSense-first library is expected to
provide. Shipping it is the .NET-cultural default; the trade-off is that
`GenerateDocumentationFile` turns on **CS1591** (warn on every undocumented public
member), which conflicts with "skip the obvious": you would then document the
whole public surface, leaning on `<inheritdoc/>` for implementations and a brief
summary even on simple factories. Treat enabling it as a deliberate decision, not
an incidental one.

## Validate

```bash
dotnet build src/SqlArtisan/SqlArtisan.csproj -c Release   # 0 warnings (incl. cref CS1574)
```
