# 0005 — Public API lives only in `Sql.*.cs` and `SqlBuilder/`; `Internal/` is implementation detail

**Status:** Accepted

## Context

A library's public surface is its contract: it must be discoverable, stable, and
clearly separated from internals that are free to change. SqlArtisan exposes a
large and growing set of SQL functions and builders, so the surface needs a
predictable organising principle.

## Decision

- The public API is a single **`public static partial class Sql`**, split across
  **`src/SqlArtisan/Sql/Sql.{A..W}.cs`**, **one file per leading letter** of the
  function name (e.g. `Abs` lives in `Sql.A.cs`).
- The remaining public surface lives under **`src/SqlArtisan/SqlBuilder/`**
  (`Dbms`, `DbmsResolver`, `SqlArtisanConfig`, `SqlStatement`, `SqlParameters`,
  `ISqlBuilder`, …).
- **Everything under `Internal/` is implementation detail** and not part of the
  public contract, even where a type must be `public` for technical reasons.

## Consequences

- **Discoverability**: a user types `Sql.` and finds everything; the
  letter-partitioned files keep each source file small and navigable.
- **Clear contract boundary**: internals (nodes, builders, `SqlBuildingBuffer`,
  dialects) can be refactored freely without breaking consumers.
- **Convention cost**: contributors must place factories in the correct
  letter-file and keep entries alphabetical (enforced by review/conventions).
- New SQL functions follow a fixed four-touch recipe (node, keyword, `Sql.<L>.cs`
  factory, test) — see `CLAUDE.md`.
