# 0006 — Performance is a feature: allocation-light, fast building

**Status:** Accepted

## Context

SqlArtisan competes with hand-written SQL strings and other builders. Its
positioning ("allocation-light, fast") is only credible if the build path is
engineered for it and measured. Query building runs on hot request paths, so
allocations and copies matter.

## Decision

Treat build-time performance — **especially low allocation** — as a first-class
design constraint, verified by benchmarks.

Concretely, in the internal build path:

- `SqlBuildingBuffer` rents/returns its `char[]` from **`ArrayPool<char>.Shared`**
  rather than allocating per build, and grows via pooled buffers.
- The parameter list is **allocated lazily** (parameterless statements share a
  static empty list); a small insertion-ordered `List` is used over a dictionary
  because parameter counts are small.
- Clause storage is pre-sized and iterated as a span via
  **`CollectionsMarshal.AsSpan`** to avoid enumerator/copy overhead.
- Hot append paths are **`AggressiveInlining`**; the rare buffer-growth path is
  isolated and `NoInlining`.
- A **`tests/SqlArtisan.Benchmark`** (BenchmarkDotNet) project compares against
  other builders; **allocation leadership is the firm target**, speed is
  best-effort-top-tier.

## Consequences

- Internal code accepts extra complexity (pooling, manual buffer growth, spans,
  inlining attributes) in exchange for measured low allocation — an explicit
  trade, justified only in the build hot path.
- Pooled, mutable buffers require disciplined lifetime handling (build → finalize
  via `ToSqlStatement` → dispose); `SqlStatement` takes ownership of parameters.
- Performance claims in docs must be backed by the benchmark suite on a stated
  machine/version (no unbacked numbers).
- This constraint applies to the **build path**; correctness and faithful output
  (ADR 0001) always win over micro-optimisation elsewhere.
