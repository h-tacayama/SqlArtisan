# ADR 0005 — Public API location

**Status:** Accepted

## Context

The public surface is the library's contract: it must be discoverable and clearly
separated from internals that are free to change.

## Decision

- The public API is one **`public static partial class Sql`**, split across
  `src/SqlArtisan/Sql/Sql.{A..W}.cs` — one file per leading letter of the function
  name.
- The remaining public surface lives under `src/SqlArtisan/SqlBuilder/`.
- A narrow set of **public table-reference types** lives under
  `src/SqlArtisan/SqlPart/TableReference/` — the types consumers subclass or
  instantiate to name relations and reference columns: `DbTableBase` / `DbTable`,
  `CteBase` / `Cte`, `DerivedTableBase` / `DerivedTable`, and the `DbColumn` they
  expose. These are public by necessity and part of the contract.
- **Everything under `Internal/` is implementation detail**, even where a type is
  `public` for technical reasons.

## Consequences

- Discoverable: `Sql.` surfaces the whole function API, in small per-letter files.
- Internals refactor freely without breaking consumers.
