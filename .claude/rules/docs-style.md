---
description: Documentation prose & style conventions for SqlArtisan (README, docs/, llms.txt)
paths:
  - "README.md"
  - "docs/**/*.md"
  - "llms.txt"
  - "llms-full.txt"
  - "context7.json"
  - "CHANGELOG.md"
  - "src/*/README.md"
---

# Documentation style

Covers wording and formatting for the README (landing + capability-map index),
`docs/` (reference), `llms.txt` (the AI-tool index), `llms-full.txt` (its
full-text companion — every page `llms.txt` links via a raw-content URL,
concatenated verbatim in that order; regenerate per the header comment in
`tests/SqlArtisan.Tests/LlmsFullTests.cs`, which gates it byte-for-byte against
drift), `context7.json` (Context7 indexing config), `CHANGELOG.md`, and the
package READMEs under `src/*/README.md` (NuGet landing pages). The
README/`docs/` split also lives in CLAUDE.md; the absolute-URL rule and the
DBMS enum order live only here.

A dedicated MCP docs server was evaluated (#228) and deliberately not built:
`llms.txt`/`llms-full.txt` (resolvable via `raw.githubusercontent.com`, zero
extra infrastructure) plus Context7 registration already cover every
mainstream assistant's docs-resolution path, and a from-scratch server would
be a standing hosting/auth/registry maintenance cost this repo doesn't need.

**No ADR citations on user-facing surfaces** — README, `docs/` reference
pages, `llms.txt`, and `CHANGELOG.md` must not cite ADR numbers ("per
ADR 0003", "(ADR 0001/0003)"): readers rarely follow them. State the
principle in plain words instead ("emitted faithfully on every dialect,
with availability left to the database"). ADR cross-references belong in
`docs/adr/` itself, code comments, and PR/issue discussion.

## Terminology

- **table class** — the generated `DbTableBase` subclass. Never "table schema
  class": the project deliberately drops the "Schema" term to avoid confusion
  with a database schema (cf. `DbTableBase`, `CteBase`, the `TableClassGen` tool).
- **type-safe** (adjective, hyphenated) vs **type safety** (noun, open).
- **bind parameter** / **bind parameters** as a noun (spaced); **bind-parameter**
  (hyphenated) only as a compound modifier — "bind-parameter prefix/marker".
  The reference section is titled **Bind Parameter Types**.
- **UPSERT** in caps as the feature/concept name (the actual per-dialect methods
  are `ON CONFLICT` / `ON DUPLICATE KEY UPDATE` / `MERGE`).
- **query builder** (not "SQL builder"). Performance wording: **allocation-light**
  / **lowest-allocation** / **fast**, in that hook order ("allocation-light, fast").

## DBMS names

- In prose, use the display spelling: **MySQL, Oracle, PostgreSQL, SQLite, SQL
  Server**. In code, use the `Dbms` enum identifiers (`Dbms.PostgreSql`, etc.) —
  never the display spelling inside code.
- When listing more than one DBMS, use `Dbms` enum order:
  **MySQL, Oracle, PostgreSQL, SQLite, SQL Server**.

## Punctuation & formatting

- Em dash: use the **spaced** form — like this — everywhere (U+2014 with one
  space on each side). Do not mix in unspaced `word—word`.
- Emitted SQL shown in `// …` comments may be line-wrapped for readability, but
  the real `sql.Text` is a single line with the same tokens in the same order —
  don't claim the wrapping is literal ("verbatim").
- Reference entries follow one shape: a one-line description → the C# snippet →
  the emitted SQL → (only when it differs by dialect) a dialect note that lists
  DBMS in enum order.
- **Dialect caveat note** (a construct that is invalid or a trap on some
  DBMS): one sentence naming the affected DBMS (enum order) and, where one
  exists, the **sibling SqlArtisan API that emits that dialect's own
  construct** — "On Oracle and SQL Server, recurse with plain `With(...)` —
  `WithRecursive()` is rejected there." Where no sibling API exists, "not
  available there" is the complete answer: never a hand-written SQL rewrite,
  and never a claim that two constructs are interchangeable (ADR 0020 — that
  is a portability claim, which ADR 0001 refuses in code).
- **Result semantics are the engine's** — duplicate handling, `NULL` matching,
  multiplicity, collation, precision. Link the engine's manual; do not restate
  it. A hazard in *SqlArtisan's own emitted SQL* is a different thing and stays
  (the callouts below).
- **No version floor in a reference page.** A floor lives only where a test
  keeps it tied to `DialectMatrix`: `docs/analyzer.md`'s version-bound register
  and XML `<remarks>` (ADR 0020). Reference pages state which dialects support
  a construct and link to the register for the version.
- README→docs and docs↔docs links are absolute GitHub `blob/main` URLs;
  in-page anchors stay relative. In `llms.txt`, a page's URL form decides
  whether it joins the `llms-full.txt` deep bundle: pages meant for ingestion
  use `raw.githubusercontent.com` URLs (the `LlmsFullTxt_SourceOrder` gate
  requires each to have a matching embedded section), while `## Optional`
  pages kept *out* of the bundle use `github.com` browse URLs (`blob`/`tree`)
  — the CHANGELOG (its history names since-removed APIs) and the ADR corpus.
- Adding/renaming/moving a `## ` section in `docs/expressions.md` or
  `docs/functions.md` must update `docs/README.md`'s index **and** the root
  README capability-map row, both **in page order** — `DocsIndexTests` gates
  missing links on both, plus order and stale anchors on the root README row
  (#210, #340) — and usually `llms.txt` (descriptor prose, ungated).

## Hazard callouts

Which of `[!WARNING]`, `[!NOTE]`, or a plain dialect caveat bullet a hazard
earns is keyed on whether the specific silently-wrong case is caught, not on
how alarming the hazard feels:

- **`[!WARNING]`** — the same call's meaning silently changes with context —
  the target dialect, or a change over time such as schema drift — and for
  at least one context where it executes and returns a plausible-but-wrong
  answer, nothing diagnoses it (a *different* dialect of the same construct
  may separately be flagged unsupported elsewhere in the matrix — that
  doesn't disqualify the hazard). Bold one-line statement of what silently
  changes, naming the dialects (enum order) when the hazard is dialect-keyed,
  and the working alternative. At most one comparison table. **Hard cap: 10
  `> ` lines.** Escape hatches that only matter once the trap is already hit
  (pragma spelling, `.editorconfig` key) go in plain prose below the callout,
  not inside it.
- **`[!NOTE]`** — the hazard is *catchable* by some layer (the analyzer
  fires, the database rejects the call, or the divergence is a silent
  *absence* rather than a wrong value — truncation, `NULL`), or it is two
  separate, correctly-named factories whose argument order or spelling
  invites mixing them up, rather than one call whose own meaning drifts. One
  to a few sentences, no table. **Hard cap: 5 `> ` lines.**
- **Plain bullet / prose** — a straightforward per-dialect substitution with
  no silent-wrongness risk, and any purely informational note (cross-reference
  to another section, usage reminder). A callout that contains no hazard
  language is a cross-reference — write it as prose with a link, never as a
  `[!NOTE]`.

A callout is for the hazard only. Move anything a reader needs only *after*
already tripping the trap (pragma names, config keys, workaround mechanics)
into ordinary prose immediately following it.

Those three are the **whole** taxonomy: a hazard picks one of them, never
`[!IMPORTANT]`/`[!TIP]`/`[!CAUTION]`. Reaching for a fourth kind is how a
hazard escapes the severity question and the length cap at once — the drift
this section exists to stop. The corpus's one `[!IMPORTANT]`
(`docs/query-statements.md`, SQL Server `MERGE`) predates this section and is
grandfathered, not a precedent — a new callout that reads as a usage advisory
still picks `[!WARNING]`/`[!NOTE]`/prose like any other, per the criteria
above. `DocsCalloutTests` bounds the grandfathered one to 10 lines like every
other kind, so it cannot grow unchecked while it stands.
