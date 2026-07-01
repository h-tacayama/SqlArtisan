---
name: sa-write-xml-docs
description: Write or revise XML doc comments (///) for SqlArtisan's public API. Use when adding/editing summaries on Sql.* factories, builder-step interfaces, or other user-facing surface — to match the project's terse, point-of-use house style (what it emits + when to use it, no design rationale) and its tag conventions.
---

# Writing XML docs for SqlArtisan

The reader is a SQL-savvy user seeing this in IntelliSense while writing a query.
Document what the call **emits** and **when to use it** — never how or why it is
built.

## Core rules (the ones that get missed)

1. Document every `public`/`protected` member of a `public` type; internals are
   optional (and use `//`, not `///`).
2. `<summary>` = the emitted SQL in `<c>...</c>` + when to reach for it. Leave
   **out** rationale, mechanism, the return type, and "can't be called twice" —
   the type system already shows those.
3. Overload siblings share one canonical doc via `<inheritdoc cref="Name(ArgTypes)"/>`
   plus only their new `<param>`; never re-type a summary.
4. Every `<c>` form must equal real `Format` output — harness-verify anything
   dialect-specific or non-obvious (don't trust memory). Every `<see cref>` must
   resolve (CS1574 = 0).
5. Layout: `<summary>` spans three `///` lines (open / text / close), even
   one-liners; `<param>` / `<returns>` / `<exception>` stay inline. Keywords →
   `<see langword="null"/>`; SQL tokens → `<c>`.
6. Put a builder step's doc on its `ISelectBuilder*` interface, not the
   implementation.

> Running a whole-surface campaign — enabling the CS1591 detector, the per-batch
> cycle, the exit condition? See **[bulk-pass.md](bulk-pass.md)**.

## What to document (.NET accessibility rule)

- **`public`/`protected` members of `public` types** — all of them; even a
  one-line summary counts, and an interface implementation can use `<inheritdoc/>`.
- **`internal`/`private`, and members of `internal` types** — optional. Add `///`
  only for a non-obvious maintainer contract; a plain `//` is usually better.
  **Hygiene:** when editing a file, downgrade stray `///` on internal members to
  `//` (or delete if it only restates the signature).
- **`public`-in-`Internal` node types** (a `public` factory must return them): the
  project treats these as implementation detail and suppresses CS1591 for
  `Internal/SqlPart` via `.editorconfig` — **don't document them**.

Apply these to new and edited docs; a repo-wide normalization of older docs is a
separate, deliberate decision.

## Include (point of use)

- **Emitted SQL form** in `<c>...</c>` (`<c>CEIL(expr)</c>`) — the highest-value
  line; it must equal what `Format` emits.
- **`<param name="x">`** per argument — meaning / accepted form / constraints, not
  the name restated; all-or-none.
- **`<returns>`** — the construct or builder state produced.
- **`<exception cref>`** — misuse throws are part of the contract (empty
  `Rollup()` → `ArgumentException`, null → `ArgumentNullException`).
- **When to reach for it** — dialect availability or the sibling to use instead,
  in `<remarks>`; reference a param in prose with `<paramref name="x"/>`.

## Leave out

- Design rationale / mechanism / ADR reasoning (ADR numbers go in `//`, never
  `///`).
- What the signature already shows — return type, parameter types, "can't be
  called twice".
- Internal type names the user can't see.

```csharp
// Before (mechanism the compiler already enforces):
/// ... Returns <see cref="ISelectBuilderWithRollup"/>, which does not offer
/// <c>WithRollup()</c>, so the suffix cannot be applied twice.
// After (what the caller needs):
/// Appends MySQL's <c>WITH ROLLUP</c> suffix to <c>GROUP BY</c>
/// (<c>GROUP BY a, b WITH ROLLUP</c>). On other dialects use <c>Sql.Rollup(...)</c>.
```

## House style

- Tags: `<summary>`, `<param>`, `<returns>`, `<exception cref>`, `<paramref>`,
  `<see cref>`, `<remarks>`, `<c>` for SQL/code, `<typeparam>` for generics,
  `<value>` for a property.
- Keywords as `<see langword="null"/>` / `true` / `false`, not `<c>null</c>`.
- **First sentence** (shows in completion lists): a noun phrase for a
  construct-producing factory ("The `<c>CEIL(expr)</c>` function (smallest integer
  not less than <paramref name="expr"/>)."); the .NET verb-first form for an
  action or property ("Gets …").
- **Line breaks:** `<summary>` always three `///` lines;
  `<param>`/`<returns>`/`<typeparam>`/`<exception>` inline (wrap content only when
  long). This is the Visual Studio / Roslyn layout.
- **Document on the interface, not the implementation** — put `<inheritdoc/>` on
  the implementing method so shipped XML / DocFX inherit it. Never write it twice.
- Runnable examples live in the **README**, not `<example>` blocks.

Copy from `Sql.C.cs`: `Ceil`/`Ceiling` (`<remarks>` cross-ref) and `Cast`
(`<paramref>` inside the summary).

## Overload families

- Give the **canonical** overload (simplest, or the `params` one) the full doc.
- **Siblings**: `/// <inheritdoc cref="Canonical(ArgTypes)"/>` (the cref's type
  list disambiguates), then add `<param>` only for new args. If adding any
  `<param>` trips **CS1573**, re-list the inherited params too — required, not
  redundant.
- Overloads that genuinely differ in behavior (not just arity) are **not** a
  family — document each in full (e.g. `Trim(object)` → `TRIM(x)` vs
  `Trim(object, object)` → `TRIM(BOTH c FROM x)`).

## Enums

- **Type**: noun-phrase summary; `<see cref>` the factory that consumes it.
- **Every value**: one-line `<summary>`, with the dialect nuance when the token is
  non-obvious (`Dow` = day of week, Sunday = 0; `Isodow` = Monday = 1;
  `Epoch` = seconds since 1970). `<inheritdoc>` doesn't apply to enum fields.
- **`[Flags]`**: state what each flag turns on, the mutually-exclusive pairs, and
  the `None = 0` default in the type summary.

## Skeletons (copy, then fill)

```csharp
// Construct-producing factory (canonical overload)
/// <summary>
/// The <c>CEIL(<paramref name="expr"/>)</c> function (smallest integer not less than the argument).
/// </summary>
/// <param name="expr">The numeric expression to round up.</param>
/// <returns>A <c>CEIL</c> function expression.</returns>
/// <remarks>SQL Server spells this <c>CEILING</c>; use <see cref="Ceiling(object)"/>.</remarks>

// Sibling overload — share the text, add only the new parameter
/// <inheritdoc cref="Trim(object)"/>
/// <param name="characters">The set of characters to strip instead of spaces.</param>

// Property / enum value (still three lines)
/// <summary>
/// Gets the bound parameter values, keyed by marker name.
/// </summary>
```

## Accuracy

- The `<c>` form equals real output — confirm dialect-specific / non-obvious forms
  with the **sa-run-sql-harness** skill.
- When behavior changes, update the doc in the same change.
- Every `<see cref>` resolves; the core lib builds at the **0-warning** bar
  (CS1574 included).
