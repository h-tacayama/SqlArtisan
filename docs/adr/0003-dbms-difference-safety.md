# ADR 0003 — DBMS-difference safety: permissive API + opt-in analyzer

**Status:** Accepted — not yet implemented

## Context

ADR 0001 makes the author responsible for DBMS correctness. How far should the
library help prevent *wrong-DBMS* usage (e.g. `Ceiling` on Oracle,
`ON DUPLICATE KEY UPDATE` on PostgreSQL), and at what cost to the API? Five
approaches were explored (the `claude/dbms-*` spike branches):

| Approach | Wrong usage caught | Cross-DBMS mixing | API cost |
|----------|--------------------|-------------------|----------|
| ① Permissive, untyped (no guidance) | none | not caught | none |
| ② Namespaces for functions, plain methods for clauses | functions: compile | runtime guard | per-DBMS namespaces |
| ③ Per-DBMS namespaces throughout | compile (per file) | runtime guard | namespaces; no neutral API |
| ④ DBMS as a type parameter (phantom types) | compile | **compile** | generics throughout |
| **⑤ Permissive + opt-in analyzer** | **build warning** | not caught | **none** |

Only ④ catches *mixing* at compile time, but its pervasive generics suit neither
the philosophy nor the audience. ⑤ adds wrong-usage warnings to the otherwise
untouched permissive API.

## Decision

Adopt **⑤: keep the permissive single API and add an opt-in Roslyn analyzer.**

Given a configured target DBMS, the analyzer warns on unsupported functions and
clauses, wrong arity, and an unrecognised target. The target is set via
`.editorconfig` or an MSBuild property; it is opt-in (silent until set), warnings
only, and ships in the package as a build-time analyzer (no runtime cost).

DBMS-difference checking is the analyzer's job; `Build(Dbms)` does none of it and
emits faithfully (ADR 0001).

## Consequences

- No public API change, no generics, unified docs — lowest adoption friction;
  honours ADR 0001.
- Additive and reversible: ③/④ could be layered later if the product ever pivots
  to "compile-time-safe multi-DBMS".
- Out of scope for *every* approach: behavioural divergence (rounding mode,
  truncation, type restrictions), connection-vs-target mismatch, and per-call
  mixing within one file — covered by docs, not the analyzer.
- The analyzer's value rests on a verified per-DBMS matrix; it is degradable (only
  verified entries warn), so an incomplete matrix never yields a false positive.
