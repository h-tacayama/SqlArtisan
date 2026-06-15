# 0003 — DBMS-difference safety: permissive API + opt-in analyzer

**Status:** Accepted — implementation deferred, **required for 1.0**. Tracking: #93.

## Context

ADR 0001 makes the author responsible for DBMS correctness. How far should the
library help prevent *wrong-DBMS* usage (e.g. `Ceiling` on Oracle,
`ON DUPLICATE KEY UPDATE` on PostgreSQL), and at what cost to the API? Five
approaches were explored (the `claude/dbms-*` spike branches):

| Approach | Wrong usage caught | Cross-DBMS mixing | API cost |
|----------|--------------------|-------------------|----------|
| ① permissive, untyped | no (clauses) | runtime | none |
| ② namespaces for functions, methods for clauses | compile (functions) | runtime | namespaces |
| ③ per-DBMS namespaces everywhere | compile (per file) | runtime | namespaces; no neutral API |
| ④ phantom types | compile | **compile** | generics everywhere |
| **⑤ permissive + opt-in analyzer** | **build-time warning** | not caught | **none** |

Only ④ catches *mixing* at compile time, but its pervasive generics suit neither
the philosophy nor the audience. Namespace cost is driven by fluent depth, not by
how divergent the syntax is.

## Decision

Adopt **⑤: keep the permissive single API and add an opt-in Roslyn analyzer.**

Given a configured target DBMS, the analyzer warns on unsupported functions and
clauses, wrong arity, and an unrecognised target. The target is set via
`.editorconfig` or an MSBuild property; it is opt-in (silent until set), warnings
only, and ships in the package as a build-time analyzer (no runtime cost).
Mechanics live with the analyzer source.

## Consequences

- No public API change, no generics, unified docs — lowest adoption friction;
  honours ADR 0001.
- Additive and reversible: ③/④ could be layered later if the product ever pivots
  to "compile-time-safe multi-DBMS".
- Out of scope for *every* approach: behavioural divergence (rounding mode,
  truncation, type restrictions), connection-vs-target mismatch, and per-call
  mixing within one file — covered by docs, not the analyzer.
- The analyzer's value rests on a verified per-DBMS matrix. It is degradable —
  only verified entries warn — so an incomplete matrix never produces a false
  positive.

## Plan

1. Build out SQL coverage on the permissive API, recording dialect divergence into
   the matrix as each feature lands.
2. Freeze the 1.0 surface, then verify the matrix for exactly that surface.
3. Ship the analyzer with the verified matrix — a **1.0 requirement**, so the
   matrix is the 1.0 critical path (bounded by the frozen surface).
