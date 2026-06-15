# 0005 — Public API lives only in `Sql.*.cs` and `SqlBuilder/`

**Status:** Accepted

## Context

The public surface is the library's contract: it must be discoverable and clearly
separated from internals that are free to change.

## Decision

- The public API is one **`public static partial class Sql`**, split across
  `src/SqlArtisan/Sql/Sql.{A..W}.cs` — one file per leading letter of the function
  name.
- The remaining public surface lives under `src/SqlArtisan/SqlBuilder/`.
- **Everything under `Internal/` is implementation detail**, even where a type is
  `public` for technical reasons.

## Consequences

- Discoverable: `Sql.` surfaces the whole function API, in small per-letter files.
- Internals refactor freely without breaking consumers.
- Contributors keep factories in the right letter-file; the per-function recipe is
  in `CLAUDE.md`.
