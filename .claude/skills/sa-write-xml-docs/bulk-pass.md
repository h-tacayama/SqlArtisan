# Bulk documentation passes

How to document a large public surface at once (the campaign behind issue #120).
For writing a *single* comment, see `SKILL.md` — this file is only the operational
wrapper around it.

## Shipping docs (project policy)

`<GenerateDocumentationFile>` is **permanently on** in the shipped csprojs
(`SqlArtisan`, `ArrayBind`, `Dapper`) — it ships IntelliSense XML and
**enforces** "document every public/protected member" via **CS1591** (one
warning per undocumented publicly-visible member). Never turn it off, even
for a partial batch: switching it off drops the shipped XML and silences the
CS1591/CS1574 gate. The `public`-in-`Internal` nodes are settled — CS1591 is
suppressed for `Internal/SqlPart/**` via `.editorconfig`.

## The CS1591 detector = work-list and exit condition

The compiler already enumerates the gap:

1. `dotnet build src/SqlArtisan/SqlArtisan.csproj -c Release` — each undocumented
   publicly-visible member is a **CS1591**, each broken `<see cref>` a **CS1574**.
   That CS1591 list **is** the work; the pass is done at **0 warnings**.
2. `dotnet format SqlArtisan.sln` — the `.editorconfig` gate CI enforces.
3. `dotnet test tests/SqlArtisan.Tests` — docs never change emitted SQL, so it
   must stay green (a failure means an edit touched code, not just `///`).

## Per-batch cycle

Go area by area (or `Sql.<letter>` file by file); run every batch through:

1. **Write** per `SKILL.md` — skeletons, `<inheritdoc>` overloads, enum rules.
2. **Reconcile existing docs** in the touched files against the house style; apply
   the internal-`///` hygiene rule.
3. **Verify against source** — harness any dialect-specific / non-obvious `<c>`
   form or enum token with the `sa-run-sql-harness` skill; never assert from memory.
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
