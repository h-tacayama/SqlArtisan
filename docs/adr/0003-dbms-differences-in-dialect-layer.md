# 0003 — Handling DBMS differences: token syntax in the dialect layer, divergent constructs as per-dialect APIs

**Status:** Accepted

## Context

Under "faithful output, no portability" (ADR 0001), DBMS differences still have to
be handled somewhere — but not all differences are alike:

- **Token-level**: the *same* construct spelled differently — alias/identifier
  quoting (`"` vs `` ` ``), parameter markers (`:` / `@` / `?`), the `ON CONFLICT`
  excluded-row spelling (`EXCLUDED` vs `excluded`), the statement terminator
  (SQL Server `MERGE` needs `;`).
- **Construct-level**: a genuinely *different* construct for the same intent —
  UPSERT (`ON CONFLICT` vs `ON DUPLICATE KEY UPDATE` vs `MERGE`), string
  aggregation (`STRING_AGG` vs `LISTAGG` vs `GROUP_CONCAT`, with different
  ordering/separator syntax), date/interval arithmetic.

The two need different treatment, and the boundary between them is the key rule.

## Decision

**Token differences are resolved internally in the dialect layer; construct
differences are exposed as per-dialect public APIs. Neither rewrites semantics.**

1. **Token-level → dialect layer (transparent to the user).**
   - Per-DBMS primitives live behind **`IDbmsDialect`** (one impl per DBMS),
     surfaced through **`SqlBuildingBuffer`** helpers (alias quoting, parameter
     markers, `AppendExcludedReference`, statement terminator, …).
   - **Function/clause nodes never branch on `Dbms`** (`if (dbms == ...)`); they
     emit via the buffer and ask the dialect for any differing token.
   - The user calls one method; `Build(Dbms)` yields the right tokens.

2. **Construct-level → per-dialect public APIs (the user chooses).**
   - Expose explicit, dialect-named methods/entry points (Drizzle/Kysely style):
     `OnConflict…` vs `OnDuplicateKeyUpdate…`; `StringAgg` / `Listagg` /
     `GroupConcat`. **Never** a single "portable" method that silently rewrites.
   - Where a dialect cannot express a requested operation, fail at **build time**
     with a clear error rather than emitting silently-wrong SQL.

A per-dialect API from (2) still uses the dialect layer (1) for its tokens — e.g.
the single `OnConflict` method emits `EXCLUDED`/`excluded` per dialect.

## Consequences

- Token divergence is **centralised and testable** in one place per DBMS; nodes
  stay DBMS-agnostic, and adding a DBMS is largely one new `IDbmsDialect`.
- Construct divergence makes the **public surface grow per dialect** for those
  features — accepted, and what makes opt-in guidance (ADR 0005) tractable (the
  analyzer flags the wrong dialect's method per the verified matrix).
- A genuinely divergent construct may add an `IDbmsDialect` member only some
  dialects use — a visible, accepted signal of real divergence.
- This is the mechanism that upholds ADR 0001: **syntax differs, semantics do
  not.** It governs every coverage item in Epic #91 (#85 UPSERT, #86 date
  arithmetic, #88 string-agg, #89 MERGE): resolve tokens in the dialect, design
  per-dialect methods for constructs, invent no unifying rewrite.
