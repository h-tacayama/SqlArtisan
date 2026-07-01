---
name: sa-run-integration-tests
description: Run the real-database integration test matrix (tests/SqlArtisan.IntegrationTests) that executes SqlArtisan-built statements against MySQL, Oracle, PostgreSQL, SQL Server (Testcontainers) and SQLite (in-process), to confirm the emitted SQL actually runs on a real engine. Use when asked to run/try the integration tests, verify a change against a live database, check one engine, or trigger the matrix in CI. NOT a substitute for the exact-SQL unit tests in tests/SqlArtisan.Tests (the fast inner loop) — this is the slower, releases-gating outer loop.
---

# Run the real-database integration matrix

`tests/SqlArtisan.IntegrationTests` (xUnit) builds queries with SqlArtisan and
**executes** them against a live database through the `SqlArtisan.Dapper`
connection extensions, so it exercises the whole path — builder → `DbmsResolver`
(dialect inferred from the connection type) → dialect → real DB. It catches
grammar/semantic bugs only the engine itself rejects (window functions, `MERGE`,
per-dialect pagination, Oracle identifier folding), which the exact-SQL unit
tests cannot. The four container engines run via Testcontainers; SQLite is
in-process.

Each engine is one xUnit class tagged with an `Engine=` trait
(`Sqlite` / `PostgreSql` / `MySql` / `SqlServer` / `Oracle`), so you select a
lane with `--filter`.

## Prerequisites

- **A running Docker daemon** for the four container engines (Testcontainers
  pulls and starts a container per lane). SQLite needs nothing.
- First run pulls images; **Oracle (`gvenzl/oracle-free`) is large and slow to
  start** (a minute or two), so it dominates wall-clock.

## Run it

```bash
# Whole matrix (needs Docker; pulls 4 images on first run)
dotnet test tests/SqlArtisan.IntegrationTests -c Release

# One engine — Engine ∈ { Sqlite, PostgreSql, MySql, SqlServer, Oracle }
dotnet test tests/SqlArtisan.IntegrationTests -c Release --filter "Engine=Sqlite"
dotnet test tests/SqlArtisan.IntegrationTests -c Release --filter "Engine=PostgreSql"
```

SQLite is the one lane that runs **anywhere** (no Docker), so it's the quickest
smoke test that the harness, schema, and Dapper wiring are intact:

```bash
dotnet test tests/SqlArtisan.IntegrationTests --filter "Engine=Sqlite"
```

## Caveat: the cloud dev container can't run the container engines

This managed environment has **no Docker daemon** and **Docker Hub is blocked**
(only `mcr.microsoft.com` is reachable), so locally here you can run **only
`Engine=Sqlite`**. The four container lanes (PostgreSQL, MySQL, SQL Server,
Oracle) must run on a real Docker host — a local dev machine or CI. Don't
interpret a container-lane failure *here* as a product bug; it's the missing
daemon. Verify those four in CI (below).

## Run it in CI

The matrix lives in `.github/workflows/integration.yml` as a per-engine matrix
(`fail-fast: false`, so one red lane doesn't cancel the rest). It is **off the
per-PR fast loop** (`ci.yml`) by design — unit tests are the inner loop. It runs:

- **nightly** (schedule),
- **on demand** via `workflow_dispatch` — GitHub UI *Actions → Integration
  Tests → Run workflow*, or the GitHub MCP `actions_run_trigger`
  (workflow `integration.yml`, the branch you want),
- as a **release gate** — `release.yml` calls it via `workflow_call`, so the
  NuGet `publish` job only runs after a green matrix.

To validate the container engines on a feature branch *before* merge (since
`workflow_dispatch` only shows for workflows already on the default branch),
temporarily add a `pull_request:` trigger to `integration.yml`, push, confirm
green, then remove it before merging. (This is exactly how #151 was landed.)

## Adding coverage

- Put **dialect-agnostic** assertions (valid on all five engines) in
  `Tests/IntegrationTestBase.cs` — they run for every engine via inheritance.
- Put **dialect-specific** statements (pagination, UPSERT, `MERGE`, `RETURNING`)
  in the per-engine class (`Tests/<Engine>Tests.cs`).
- **Mutating tests run inside a rolled-back transaction** so the shared baseline
  seed (5 users, 5 orders) stays intact and the suite is order-independent —
  follow that pattern, don't leave committed writes behind.
- DDL is per-engine in `Infrastructure/TestSchema.cs`; the seed rows are inserted
  through SqlArtisan, so seeding doubles as INSERT-execution coverage.

## Notes

- A known Oracle bug (#165, CTE column alias → `ORA-00904`) is parked as a
  **skipped** test, `OracleTests.Cte_AliasedColumn_KnownOracleBug` — un-skip it
  when #165 is fixed.
- This complements `sa-run-sql-harness` (which only *observes* the emitted string)
  and `sa-run-benchmark`: use this when "does it actually run on the engine?" is the
  question.
