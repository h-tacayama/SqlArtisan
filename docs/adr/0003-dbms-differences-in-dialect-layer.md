# 0003 — DBMS-specific syntax lives only in the dialect layer; nodes never branch on `Dbms`

**Status:** Accepted

## Context

Even under "faithful output, no portability" (ADR 0001), some syntax genuinely
differs per DBMS: identifier/alias quoting (`"` vs `` ` ``), parameter markers
(`:` / `@` / `?`), pagination, the `ON CONFLICT` excluded-row spelling
(`EXCLUDED` vs `excluded`), the statement terminator (SQL Server `MERGE` needs
`;`), and so on. This divergence must live *somewhere*.

## Decision

- Per-DBMS syntax primitives live behind **`IDbmsDialect`** (one implementation
  per DBMS), surfaced to emission through **`SqlBuildingBuffer`** helpers
  (e.g. `EncloseInAliasQuotes`, parameter markers, `AppendExcludedReference`).
- **Function and clause nodes never branch on `Dbms`.** A node emits via the
  buffer; any genuine syntactic difference is asked of the dialect, not decided
  with `if (dbms == ...)` inside the node.

## Consequences

- **Divergence is centralised and testable** in one place per DBMS, instead of
  scattered conditionals across hundreds of nodes.
- **Nodes stay DBMS-agnostic**; adding a DBMS is (mostly) one new `IDbmsDialect`.
- A genuinely divergent *construct* (not just a token) may add a member to
  `IDbmsDialect` that only some dialects use — an accepted, visible cost that
  signals real divergence.
- This is the mechanism by which ADR 0001 is upheld: syntax differs, semantics do
  not.
