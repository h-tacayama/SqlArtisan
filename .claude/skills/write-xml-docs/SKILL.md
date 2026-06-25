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
  - **Hygiene while editing:** when a batch touches a file, clean up stray `///`
    on its internal members — downgrade to `//` if it captures real design intent,
    delete it if it only restates the signature. A `///` on an internal compiles to
    orphan XML and reads as a public contract the member does not have; `//` is the
    right vehicle for internal design notes.

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

## Overload families — share text, never paraphrase

Most factories ship as overload families (`Case` ×N, `Trim` ×3, `Avg` ×2). Drift
between near-identical overloads is the main consistency risk; eliminate it
mechanically rather than by careful re-typing:

- Pick the **canonical overload** (usually the simplest, or the `params` one) and
  give it the full doc — summary with the emitted form, every `<param>`,
  `<returns>`, `<exception>`, `<remarks>`.
- **Sibling overloads** carry `/// <inheritdoc cref="Canonical(ArgTypes...)"/>`
  so the summary/returns text is *literally the same string*, then add their own
  `<param>` only for arguments the canonical one lacks. Do **not** re-write the
  summary per overload — identical wording is the contract, and `<inheritdoc>`
  guarantees it. The `cref` uses the overload's parameter type list to
  disambiguate (`<inheritdoc cref="Case(SearchedCaseWhenClause, SearchedCaseWhenClause[])"/>`).
- When two overloads genuinely differ in behavior (not just arity), they are not
  a family — document each in full.

## Enum types and values

- Document the **enum type** with a noun-phrase summary naming what it selects and
  `<see cref>`-ing the factory that consumes it (e.g. `DateTimePart` →
  <see cref="Sql.Extract"/>).
- Document **every value** with a one-line `<summary>` — what the field selects,
  plus the dialect nuance when the token is non-obvious (`Dow` = day of week,
  Sunday = 0; `Isodow` = ISO day, Monday = 1; `Epoch` = seconds since 1970-01-01;
  `Julian` = Julian day number). Plain English; no `<c>` SQL form unless the value
  maps to a specific literal token. `<inheritdoc>` does not apply to enum fields —
  write each.
- For a `[Flags]` enum (`RegexpOptions`) state what each flag turns on and call
  out mutually-exclusive pairs (`CaseSensitive` vs `CaseInsensitive`) and the
  `None = 0` default in the type summary.

## Skeletons (copy, then fill)

```csharp
// Sql.* construct-producing factory (canonical overload)
/// <summary>The <c>CEIL(<paramref name="expr"/>)</c> function (smallest integer not less than the argument).</summary>
/// <param name="expr">The numeric expression to round up.</param>
/// <returns>A <c>CEIL</c> function expression.</returns>
/// <remarks>SQL Server spells this <c>CEILING</c>; use <see cref="Ceiling(object)"/>.</remarks>

// Sibling overload — share the text, add only the new parameter
/// <inheritdoc cref="Trim(object)"/>
/// <param name="characters">The set of characters to strip instead of spaces.</param>

// Builder-step method — doc on the ISelectBuilder* interface, not SelectBuilder
/// <summary>Appends MySQL's <c>WITH ROLLUP</c> suffix to the <c>GROUP BY</c> clause (<c>GROUP BY a, b WITH ROLLUP</c>).</summary>
/// <returns>The builder, narrowed so the suffix cannot be applied twice.</returns>

// Property
/// <summary>Gets the bound parameter values, keyed by marker name.</summary>

// Enum value
/// <summary>The year component.</summary>
```

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
provide. Shipping it is the .NET-cultural default. Enabling it **enforces** the
"document all public/protected" rule above via **CS1591** (warn on every
undocumented publicly-visible member) — it doesn't change the rule, only surfaces
its cost, since the existing public surface isn't fully documented yet. Budget for
documenting the whole surface (brief summaries, `<inheritdoc/>` on implementations)
and decide what to do with the public-in-`Internal` nodes. Treat enabling it as a
deliberate decision.

## Per-batch cycle (bulk passes)

Documenting a large surface goes area by area (or `Sql.<letter>` file by file).
Run every batch through the same loop so precision and consistency hold:

0. **Enable the detector.** Turn `GenerateDocumentationFile` on (see Done) so
   CS1591 enumerates the batch's undocumented members — that list *is* the work.
1. **Write** the batch's docs per this skill — skeletons, overload text-sharing
   (`<inheritdoc cref>`), enum rules.
2. **Reconcile existing docs.** Review any `///` already in the touched files
   against this house style and fix drift; apply the internal-`///` hygiene rule
   above (downgrade to `//` or delete).
3. **Verify against source.** For every dialect-specific or non-obvious `<c>` form
   or enum token, confirm the real output with the run-sql-harness skill — never
   assert it from memory.
4. **Re-review** the batch against the per-member checklist below.
5. **Gate, then commit.** The batch is done only when its touched files raise no
   CS1591/CS1574, `dotnet format` is clean, and `dotnet test` is green.

Keep the cycle here in this skill rather than as a separate one — it is just the
procedure for applying this house style at scale, and the exit condition and
checklist below are its final step.

## Done — the exit condition

A passing build does **not** prove completeness while `GenerateDocumentationFile`
is off: an undocumented member is silent, so "0 warnings" can mean "nothing
documented." Make the compiler enumerate the gap, then drive it to zero:

1. Temporarily set `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
   in `src/SqlArtisan/SqlArtisan.csproj` (the project under documentation).
2. `dotnet build src/SqlArtisan/SqlArtisan.csproj -c Release` — now every
   undocumented publicly-visible member is a **CS1591** and every broken
   `<see cref>` a **CS1574**. The list of CS1591s **is** the remaining work; the
   batch is done when this build reports **0 warnings**.
3. `dotnet format SqlArtisan.sln` — the `.editorconfig` gate CI enforces.
4. `dotnet test tests/SqlArtisan.Tests` — docs never change emitted SQL, so the
   suite must stay green; a failure means an edit touched code, not just `///`.

Keeping `GenerateDocumentationFile` on is the issue-#120 shipping decision. If you
are documenting only a subset, **revert it** after using it to count/verify, so CI
does not fail on the not-yet-documented surface. When the whole surface is done,
leave it on (that is the point of #120) and confirm the full
`dotnet build SqlArtisan.sln` is still 0-warning.

### Per-member checklist (precision bar)

A publicly-visible member is "done" only when all that apply hold:
- `<summary>` first sentence is a complete, completion-list-ready phrase.
- The emitted SQL in `<c>` equals real `Format` output (harness-confirmed for any
  dialect-specific or non-obvious form; obvious 1:1 tokens need no harness run).
- Every parameter has a `<param>` (all-or-none), `<returns>` names the result,
  and each failure mode has an `<exception cref>`.
- Overload siblings use `<inheritdoc cref>`; no hand-retyped duplicate summary.
- No CS1591 / CS1574 for the member in step 2.
