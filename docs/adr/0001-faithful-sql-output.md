# ADR 0001 — The SQL you write is the SQL that runs; portability is a non-goal

**Status:** Accepted

## Context

Most query builders abstract over SQL to gain cross-database portability — one
query that runs unchanged on many DBMS. That abstraction is also their main source
of surprise: the generated SQL drifts from the author's intent and vendor-specific
power is lost. SqlArtisan instead targets developers who know their DBMS and want
deliberate, predictable SQL in C#.

## Decision

**SqlArtisan emits the SQL you wrote, faithfully; cross-database portability is a
deliberate non-goal.** The library never rewrites the meaning of a query. DBMS
differences are honoured only where the *syntax* genuinely differs (see ADR 0002).

## Consequences

- Output is predictable and inspectable; tests assert exact SQL strings.
- The author owns DBMS correctness — the deliberate trade for fidelity. Optional
  guidance is a separate concern (ADR 0003).
- No false portability promise; features whose only purpose is to run one query
  unchanged across DBMS are out of scope.
