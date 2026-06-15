# 0008 — Divergent features are exposed as per-dialect APIs, not a unified rewrite

**Status:** Accepted

## Context

Many high-value features are genuinely divergent across DBMS not just in a token
but in *shape*: UPSERT (`ON CONFLICT` vs `ON DUPLICATE KEY UPDATE` vs `MERGE`),
string aggregation (`STRING_AGG` vs `LISTAGG` vs `GROUP_CONCAT`, with different
ordering/separator syntax), date/interval arithmetic, etc. ADR 0001 forbids
rewriting the user's SQL to paper over this. So how should such features be
*surfaced*?

## Decision

Expose divergent features as **explicit per-dialect APIs** (the Drizzle/Kysely
style), **never** as a single "portable" method that silently rewrites to each
dialect.

- Separate, dialect-named methods/entry points (e.g. `OnConflict…` vs
  `OnDuplicateKeyUpdate…`; `StringAgg` / `Listagg` / `GroupConcat`).
- Pure-token differences are still resolved in the dialect layer (ADR 0003); this
  ADR governs the **public API shape** of a divergent *construct*, not token
  spelling.
- Where a dialect cannot express a requested operation, fail at build time with a
  clear error rather than emitting silently-wrong SQL.

## Consequences

- The author picks the construct that matches their DBMS — fidelity over false
  portability (upholds ADR 0001); the produced SQL is exactly that construct.
- The public surface grows per dialect for divergent features (more methods); this
  is accepted, and is what makes opt-in guidance (ADR 0005) tractable — the
  analyzer flags the wrong dialect's method per the verified matrix.
- No hidden cross-dialect translation to maintain or be surprised by.
- Guides every coverage item in Epic #91 (#85 UPSERT, #88 string-agg, #86 date
  arithmetic, #89 MERGE): design the per-dialect methods first, do not invent a
  unifying abstraction.
