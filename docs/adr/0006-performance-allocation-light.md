# 0006 — Performance is a feature: allocation-light building

**Status:** Accepted

## Context

SqlArtisan competes with hand-written SQL and other builders, often on hot request
paths. Its "allocation-light, fast" positioning is only credible if the build path
is engineered for it and measured.

## Decision

Treat build-path performance — **especially low allocation** — as a first-class
constraint, verified by the `tests/SqlArtisan.Benchmark` suite. Allocation
leadership is the firm target; speed is best-effort top-tier. (The techniques —
pooled buffers, lazy parameters, spans, selective inlining — live in the code.)

## Consequences

- Internal code accepts extra complexity in the build hot path for measured low
  allocation; this justification does not extend elsewhere.
- Performance claims in docs must cite the benchmark suite (machine and versions
  stated), never unbacked numbers.
- Correctness and faithful output (ADR 0001) win over micro-optimisation.
