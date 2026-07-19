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
  DBMS): one sentence naming the affected DBMS (enum order) and the working
  alternative in the same breath — "On Oracle (< 23ai) and SQL Server, recurse
  with plain `With(...)` — `WithRecursive()` is rejected there." Never a bare
  "not supported on X" with no way out.
- **Version boundary note**: parenthesized after the DBMS name — "(SQL Server
  2022+)", "(MySQL 8.0.20+)", "(SQLite 3.44+)" — inside the dialect note, not
  in the entry's one-line description.
- README→docs and docs↔docs links are absolute GitHub `blob/main` URLs;
  `llms.txt` uses `raw.githubusercontent.com` URLs; in-page anchors stay relative.
- Adding/renaming/moving a `## ` section in `docs/expressions.md` or
  `docs/functions.md` must update `docs/README.md`'s index **and** the root
  README capability-map row, both **in page order** — `DocsIndexTests` gates
  both (#210, #340) — and usually `llms.txt` (descriptor prose, ungated).
