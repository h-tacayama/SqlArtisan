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
  rewrites.

A per-dialect API still relies on the dialect layer for its own tokens.

## Consequences

- Token differences stay centralised and testable in the dialect layer; adding a
  DBMS is largely one new `IDbmsDialect`.
- Divergent constructs grow the public surface per dialect — the cost of being
  explicit. In return, those distinct, dialect-named methods give the ADR 0003
  analyzer something to check: it can tell which DBMS a call targets and warn on a
  mismatch.
