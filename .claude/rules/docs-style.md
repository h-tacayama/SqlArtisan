---
description: Documentation prose & style conventions for SqlArtisan (README, docs/, llms.txt)
paths:
  - "README.md"
  - "docs/**/*.md"
  - "llms.txt"
  - "CHANGELOG.md"
---

# Documentation style

Covers wording and formatting for the README (landing + capability-map index),
`docs/` (reference), `llms.txt`, and `CHANGELOG.md`. The README/`docs/` split,
the absolute-URL rule, and the DBMS enum order also live in CLAUDE.md.

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
- README→docs and docs↔docs links are absolute GitHub `blob/main` URLs;
  `llms.txt` uses `raw.githubusercontent.com` URLs; in-page anchors stay relative.
- Adding/renaming/moving a `## ` section in `docs/expressions.md` or
  `docs/functions.md` must update `docs/README.md`'s index **in page order**
  (and usually the README capability map and `llms.txt`). `DocsIndexTests`
  fails the unit suite when an index link is missing (#210).
