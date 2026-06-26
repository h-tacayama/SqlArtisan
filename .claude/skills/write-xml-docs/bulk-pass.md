# Bulk documentation passes

How to document a large public surface at once (the campaign behind issue #120).
For writing a *single* comment, see `SKILL.md` — this file is only the operational
wrapper around it.

## Shipping docs (project policy)

Enabling `<GenerateDocumentationFile>` is what makes a package ship IntelliSense —
the .NET default for a type-safety-first library. It **enforces** "document every
public/protected member" via **CS1591** (one warning per undocumented
publicly-visible member). It changes none of the rules; it only surfaces the gap.
Treat enabling it as a deliberate decision, and decide what to do with the
`public`-in-`Internal` nodes (the project suppresses CS1591 for `Internal/SqlPart`
via `.editorconfig` — they are implementation detail).

## The CS1591 detector = work-list and exit condition

A passing build proves nothing while `GenerateDocumentationFile` is off — an
undocumented member is silent, so "0 warnings" can mean "nothing documented."
Make the compiler enumerate the gap:

1. Set `<GenerateDocumentationFile>true</GenerateDocumentationFile>` in
   `src/SqlArtisan/SqlArtisan.csproj`.
2. `dotnet build src/SqlArtisan/SqlArtisan.csproj -c Release` — each undocumented
   publicly-visible member is a **CS1591**, each broken `<see cref>` a **CS1574**.
   That CS1591 list **is** the work; the pass is done at **0 warnings**.
3. `dotnet format SqlArtisan.sln` — the `.editorconfig` gate CI enforces.
4. `dotnet test tests/SqlArtisan.Tests` — docs never change emitted SQL, so it
   must stay green (a failure means an edit touched code, not just `///`).

Documenting only a subset? Revert step 1 after counting, so CI does not fail on
the not-yet-done surface. When the whole surface is done, leave it on and confirm
the full `dotnet build SqlArtisan.sln` is 0-warning.

## Per-batch cycle

Go area by area (or `Sql.<letter>` file by file); run every batch through:

0. **Enable the detector** (above) so CS1591 lists the batch's gap.
1. **Write** per `SKILL.md` — skeletons, `<inheritdoc>` overloads, enum rules.
2. **Reconcile existing docs** in the touched files against the house style; apply
   the internal-`///` hygiene rule.
3. **Verify against source** — harness any dialect-specific / non-obvious `<c>`
   form or enum token with the `run-sql-harness` skill; never assert from memory.
4. **Re-review** against the checklist below.
5. **Gate, then commit** — touched files raise no CS1591/CS1574, `dotnet format`
   is clean, and `dotnet test` is green.

## Per-member checklist

A publicly-visible member is done only when all that apply hold:

- `<summary>` first sentence is a complete, completion-list-ready phrase.
- The `<c>` form equals real `Format` output (harness-confirmed for any
  dialect-specific or non-obvious form; obvious 1:1 tokens need no harness run).
- Every parameter has a `<param>` (all-or-none); `<returns>` names the result;
  each failure mode has an `<exception cref>`.
- Overload siblings use `<inheritdoc cref>` — no hand-retyped duplicate summary.
- No CS1591 / CS1574 for the member.
