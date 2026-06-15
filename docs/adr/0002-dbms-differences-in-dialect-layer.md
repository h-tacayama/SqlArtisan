# ADR 0002 — Handling DBMS differences: tokens in the dialect layer, constructs as per-dialect APIs

**Status:** Accepted

## Context

Under ADR 0001 (faithful output), DBMS differences must still be handled — and two
kinds need different treatment:

- **Token-level**: the *same* construct spelled differently (identifier quoting,
  parameter markers, the `ON CONFLICT` excluded-row casing, a statement terminator).
- **Construct-level**: a genuinely *different* construct for the same intent
  (UPSERT: `ON CONFLICT` / `ON DUPLICATE KEY UPDATE` / `MERGE`; string aggregation:
  `STRING_AGG` / `LISTAGG` / `GROUP_CONCAT`).

## Decision

**Resolve token differences internally in the dialect layer; expose construct
differences as per-dialect public APIs. Neither rewrites semantics.**

- **Tokens → dialect layer, transparent.** Per-DBMS spelling lives behind
  `IDbmsDialect`; nodes never branch on `Dbms`. The user writes one call, and
  `Build(Dbms)` yields the right tokens.
- **Constructs → per-dialect APIs, explicit.** Expose distinct, dialect-named
  methods (the Drizzle/Kysely approach), never one "portable" method that silently
  rewrites. Where a dialect cannot express an operation, fail at build time.

A per-dialect API still relies on the dialect layer for its own tokens.

## Consequences

- Token divergence is centralised and testable; adding a DBMS is largely one new
  `IDbmsDialect`.
- The public surface grows per dialect for divergent constructs — accepted, and
  what makes the opt-in guidance of ADR 0003 possible.
- This upholds ADR 0001 (syntax differs, semantics do not) and governs the
  divergent features on the roadmap (UPSERT, MERGE, string aggregation, date
  arithmetic).
