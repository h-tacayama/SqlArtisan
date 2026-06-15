# 0003 — DBMS-difference safety: permissive API + opt-in analyzer

**Status:** Accepted — product implementation deferred; **analyzer required for 1.0**.
Discussion/tracking: GitHub issue #93.

## Context

ADR 0001 makes the user responsible for DBMS correctness. The question: how far
should the library help prevent *wrong-DBMS* usage (calling a function/clause a
target DBMS lacks, e.g. `Ceiling` on Oracle, `ON DUPLICATE KEY UPDATE` on
PostgreSQL), and at what cost to the API?

Five options were prototyped and measured (spike branches
`claude/dbms-namespace-spike` and `claude/dbms-analyzer-poc`):

| Option | Wrong-verb existence | Cross-DBMS mixing | Single/neutral API | Ergonomics |
|--------|----------------------|-------------------|--------------------|------------|
| ① permissive, untyped | not caught (clauses) | runtime guard | yes | clean |
| ② hybrid (ns functions + per-dialect clause methods) | compile (fn) / hole (clauses) | runtime guard | yes | clean |
| ③ namespaces everywhere (clauses = extension methods) | compile (per-file import) | runtime guard | no | clean |
| ④ phantom types | compile | **compile** | yes | generics everywhere |
| **⑤ permissive + opt-in analyzer** | **build-time warning** | not caught | yes | clean, no API change |

Key finding: the cost of namespace/wrapper schemes is driven by **fluent depth**,
not syntactic divergence — functions (depth 0) are cheap, fluent clauses
(depth ≥ 2) are expensive. Only ④ catches *mixing* at compile time, at the price
of pervasive generics and a portability tax unsuited to SqlArtisan's audience.

## Decision

Adopt **option ⑤: a permissive single API plus an opt-in Roslyn analyzer.**

- The analyzer (PoC: `claude/dbms-analyzer-poc`, all tests passing) flags, against
  a configured target DBMS:
  - `SQLA0001` — function / clause-verb not available on the target,
  - `SQLA0003` — function needing more arguments on the target (e.g. SQL Server
    `ROUND(x)`),
  - `SQLA0002` — an unrecognised configured target value.
- Target is set per file via `.editorconfig` (`sqlartisan_target_dbms`) or
  project-wide via `<SqlArtisanTargetDbms>`; **opt-in** (silent until set);
  warnings only; ships **inside the `SqlArtisan` package** (`analyzers/`, zero
  runtime cost).

## Consequences

- **No public API change, no generics, unified docs** — lowest adoption friction;
  honours ADR 0001 and the lightweight-runtime goal.
- **Reversible/additive**: options ③/④ could be layered later if positioning ever
  shifts toward "compile-time-safe multi-DBMS".
- **Out of scope for any option** (so also for ⑤): behavioural/semantic divergence
  (rounding mode, `GROUP_CONCAT` truncation, PG numeric-only 2-arg `ROUND`),
  target-vs-actual-connection mismatch, and per-call-site mixing within one file.
  These are addressed by docs + the verified matrix, not by the analyzer.
- **Dependency**: the analyzer's value rests on a **verified per-DBMS matrix**
  (a maintained asset). It is degradable — only verified entries are active, so an
  incomplete matrix yields *no warning*, never a false one.

## Plan (sequencing)

1. Complete the SQL-coverage issues (#85–#90, benchmarks #79) on the **permissive
   API**, recording dialect divergence into the matrix incrementally.
2. Freeze the 1.0 surface, then complete + vendor-verify the matrix for exactly
   that surface.
3. Ship the analyzer + verified matrix **bundled** — the analyzer is a **1.0
   requirement** (so the matrix is the 1.0 critical path; bounded because the
   surface is frozen).
4. Revisit ③/④ only on a deliberate positioning change.
