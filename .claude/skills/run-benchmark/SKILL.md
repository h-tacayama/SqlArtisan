---
name: run-benchmark
description: Build and run the SqlArtisan.Benchmark (BenchmarkDotNet) project to compare SqlArtisan's build path against other query builders or to measure the isolated Build() path. Use when asked to run/try the benchmarks, compare SqlArtisan's throughput or allocations vs other builders, or regenerate numbers for the README comparison. NOT for absolute timing in this cloud container (see the noise caveat) — use it for relative comparison and allocation (B/op) figures.
---

# Run the SqlArtisan benchmark suite

`tests/SqlArtisan.Benchmark` is a BenchmarkDotNet console that pits SqlArtisan's
build path against other C# query builders (Dapper.SqlBuilder, SqlKata, Sqlify,
InterpolatedSql, linq2db, a hand-written `StringBuilder` floor, …), shows EF Core
as a labeled ORM **reference**, and isolates the `.Build()` format path. This is
the *official* benchmark cited in docs — prefer it over the ad-hoc allocation
probe in the `run-sql-harness` skill for anything user-visible.

Every entrant builds the SQL string **and** its bind-parameter collection for the
same logical query (an INNER JOIN + GROUP BY aggregate with two date parameters),
so the comparison is apples-to-apples. To confirm that equivalence cheaply before
trusting a run, use the validation mode (no measured loop, exits non-zero on
mismatch):

```bash
dotnet run -c Release --project tests/SqlArtisan.Benchmark -- validate
```

## Read this before trusting the numbers

- **Release only.** BenchmarkDotNet refuses to run a Debug build. Always pass
  `-c Release`; without it the run aborts.
- **Absolute times (ns) are unreliable here.** This is an ephemeral, shared
  cloud container — the CPU is virtualized and unpinned, so mean/median timings
  carry large noise. Use the run for **relative ordering** and the
  **`Allocated` (B/op)** column, which is deterministic. Capture confirmed
  absolute numbers on a quiet local machine before putting them in `README.md`.
- **It takes minutes.** The cross-builder class is nine benchmarks. Filter to
  what you need (below) rather than running everything.

## Run it

`Program.cs` uses `BenchmarkSwitcher.FromAssembly(...).Run(args)`, which prompts
interactively when given no arguments — useless in a non-interactive session.
**Always pass `--filter`** so it runs unattended.

```bash
# Everything (both benchmark classes) — slow:
dotnet run -c Release --project tests/SqlArtisan.Benchmark -- --filter '*'

# Just the cross-builder comparison (class SqlBuilderBenchmarks):
dotnet run -c Release --project tests/SqlArtisan.Benchmark -- --filter '*SqlBuilderBenchmarks*'

# Just SqlArtisan's own methods in that comparison:
dotnet run -c Release --project tests/SqlArtisan.Benchmark -- --filter '*SqlArtisan*'

# Just the isolated Build() format path (class SqlBuildingBufferBenchmark):
dotnet run -c Release --project tests/SqlArtisan.Benchmark -- --filter '*SqlBuildingBufferBenchmark*'
```

If `dotnet run` triggers a permission prompt, the run path needs
`Bash(dotnet run:*)` in `.claude/settings.json` `permissions.allow`.

## What the two classes measure

| Class | What it isolates |
|-------|------------------|
| `SqlBuilderBenchmarks` | Default Job, `[MemoryDiagnoser]`. One representative query built end-to-end by each builder, so you can compare SqlArtisan against the alternatives. Methods are named `<Builder>_<ParamStyle>` (e.g. `SqlArtisan_SpecificParams`, `SqlKata_SpecificParams`, `Linq2db_TypedParams`). Benchmarks are grouped by `[BenchmarkCategory]` into `Builders` and a separate `ORM reference` (the single `EfCore_Reference` method); linq2db and EF Core reuse a connection/context created once in `[GlobalSetup]` so the loop measures warm steady-state. |
| `SqlBuildingBufferBenchmark` | The SqlPart tree is built once in `[GlobalSetup]`; only `.Build()` (dialect creation + buffer formatting + string + parameter dictionary) is timed. Use this to attribute a regression to the format path vs. tree construction. |

## Reading the output

BenchmarkDotNet prints a summary table and writes artifacts under
`tests/SqlArtisan.Benchmark/BenchmarkDotNet.Artifacts/` (git-ignored; don't
commit them). The columns that matter most here:

- **`Allocated`** — bytes per op. Deterministic; this is the headline number for
  ADR 0006 (allocation budget) claims. A SqlArtisan method allocating more than
  before is a real regression regardless of timing noise.
- **`Mean` / `Error` / `StdDev`** — treat as directional only in this container.
  A large `StdDev` relative to `Mean` confirms the environment is noisy.

## Notes

- Benchmark artifacts are throwaway output, not source — never commit
  `BenchmarkDotNet.Artifacts/`.
- For a quick allocation sanity check during development (not docs), the
  thread-allocation probe in `run-sql-harness` is faster than a full run.
- Durable correctness still lives in `tests/SqlArtisan.Tests`; the benchmark
  measures cost, not correctness.
