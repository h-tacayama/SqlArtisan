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
tests cannot. The seven container lanes run via Testcontainers; SQLite is
in-process.

Each lane selects the xUnit classes tagged with its `Engine=` trait
(`Sqlite` / `PostgreSql` / `MySql` / `SqlServer` / `Oracle` / `Oracle23ai` /
`PostgreSql17` / `SqlServer2025`), so you select one with `--filter`. `Oracle23ai`,
`PostgreSql17` and `SqlServer2025` are second, narrower lanes (#263, #582, #614):
they prove the analyzer's version-bound
entries above the pinned baseline rather than repeating the full suite, so they
do not derive from `IntegrationTestBase`.

## Prerequisites

- **A running Docker daemon** for the seven container lanes (Testcontainers
  pulls and starts a container per lane). SQLite needs nothing.
- First run pulls images; **Oracle (`gvenzl/oracle-xe:21.3.0-slim-faststart`,
  XE 21c — pinned in `OracleFixture`, same version the docs cite) is large and
  slow to start** (a minute or two), so it dominates wall-clock.

## Run it

```bash
# Whole matrix (needs Docker; pulls 7 images on first run)
dotnet test tests/SqlArtisan.IntegrationTests -c Release

# One lane — Engine ∈ { Sqlite, PostgreSql, MySql, SqlServer, Oracle, Oracle23ai, PostgreSql17, SqlServer2025 }
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
`Engine=Sqlite`**. The seven container lanes (PostgreSQL, PostgreSql17, MySQL, SQL
Server, SqlServer2025, Oracle, Oracle23ai) must run on a real Docker host — a local
dev machine or CI. Don't interpret a container-lane failure *here* as a product bug;
it's the missing daemon. Verify those seven in CI (below).

Worse in a cloud session with only the .NET 8 SDK: `global.json` pins .NET 10,
so `dotnet build`/`test` fails outright and you can't even **compile** the
integration project locally. The per-PR `ci.yml` does *build* it — its build step
is `dotnet build SqlArtisan.sln`, and the solution includes this project — so a
compile error does fail the PR. `ci.yml` also *runs* one DB-less slice on every
PR: the `MatrixSweepCatalogTests` completeness pair, the ADR 0011
`DialectGuardTwinTests` twin gate, and the ADR 0020
`DateTimePartNumberingTests` numbering gate, the `PictModelTests`
combinatorial-model gate and the `CombinatorialBuildTests` Build gate, via an FQN
filter, and then the SQLite lane of the combinatorial tests (every
`SqliteCombinatorial*` class). The other
engine lanes execute only through the dispatched integration run (below), the
nightly schedule, or the release gate.

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

**Validate a feature branch before merge — just dispatch it against the branch
ref.** The GitHub UI only lists `workflow_dispatch` for workflows already on the
default branch, but the *API* dispatches against any ref: because
`integration.yml` is already on `main`, `actions_run_trigger` with
`ref: <your-branch>` runs the workflow **against your branch's code**, new tests
included. No `pull_request:`-trigger hack, no push-to-default:

```
actions_run_trigger(run_workflow, workflow_id="integration.yml", ref="<your-branch>")
```

Then read per-lane results — the matrix is `fail-fast: false`, so lanes stand
alone — with `actions_list(list_workflow_jobs, <run-id>)`: each job is named
`integration (<Engine>)` with its own `conclusion`. (Rebasing the branch after a
green run doesn't invalidate it as long as the touched test files are unchanged
and the run covered the lanes you care about.)

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
- **Sweep entry vs verification test.** A `MatrixSweepCatalog` entry asserts
  that each engine's accept/reject outcome **matches the dialect matrix** in
  both directions — it presumes the answer is already known and entered. When
  the point is to *discover* per-engine behavior (does Oracle reject the
  repeated-parameter GROUP BY? — the GAP-19/#241 pattern), write a dedicated
  test that records accept/reject per engine instead; promote the fact into
  the matrix + a sweep case only after the results are in.
- **A discovery probe passes, and reports through `Console.Out`.** A probe
  that records what each engine does has no expected outcome, so it does not
  end in `Assert.Fail(report)` to get its report printed: the dispatched run
  then goes red on every lane the probe runs on, and each red lane has to be
  read to tell a probe report from a real failure. Write the report a line at
  a time with `Console.Out.WriteLine(...)` and let the test pass: under the
  workflow's `console;verbosity=normal` logger, a passing test's `Console.Out`
  reaches the job log and its `ITestOutputHelper` does not (run 41957426078).
  The output lands where it is written, not under the test's name, so prefix
  every line (`PROBE <lane> | …`) to pick it out of `get_job_logs` output.
  `Write` ends no line, so a row written with it runs into the next output and
  loses its prefix's place at the start of a line. Fail only on what breaks
  the probe itself (a fixture that never starts), so red still means the
  report is missing.
- **The sweep can't hold a spelling variation of an existing construct.** The
  catalog is keyed by `MatrixKey` (member name + optional arity), so there is
  one entry per construct. A *grammar/spelling variation* of a construct that
  already has a matrix key — e.g. aliasing a DML target (`UPDATE users "cu"` on
  Oracle, or MySQL's backtick-quoted `AS` form), where `Update` / `DeleteFrom`
  are already swept — has no key of its own and can't be a second sweep entry.
  Verify it in the per-engine class alongside the other engine-specific DML
  proofs (this is where the #255 correlated-DML alias checks live).

## Combinatorial tests

`Randomized/Models/` holds one PICT model per statement kind, each `<name>.pict`
beside its committed `<name>.cases.csv`, the pairwise expansion (3-way on the
model's sub-models): `select`, `insert`, `update` and `delete`. A model's binding
in `CaseModelCatalog` names its typed row, the keys each level's verdict reads (a
matrix member, at an arity where same-named entries differ; a
`ContextRules.DmlShape` read from `DialectUsageAnalyzer`; or a `GuardKey`, a
dialect-scoped `Build()` guard the analyzer does not report, listed in
`BuildGuards` with probes `PictModelTests` builds on every engine), and its
generator. Each lane's `<Engine>CombinatorialSelectTests` runs that
engine's `select.cases.csv` rows as one theory case per row: CsCheck generates
three queries per row from fixed seeds, `CasePlanner` derives every rewrite and
relation part from each, and `CaseChecker` runs them (rewrites that must return
the same rows, set-operator and slice checks, the C# reference evaluator).
`NegativeControl_NegatedWhere_IsDetected` proves the comparison can fail.
`StatementRunner` runs a statement inside a transaction when given one, and
`RunInRolledBackTransaction` gives a statement a transaction of its own and rolls
it back (`SqliteCombinatorialTransactionTests` proves the rollback).

Each lane's `<Engine>CombinatorialDmlTests`, a class of its own so its container
starts beside the SELECT one, runs the three DML models' rows the same way
(`DmlGenerators`, `DmlPlanner`, `DmlChecker`). Every statement, and the read-back
of `rt_users` and `rt_depts` after it, runs inside one rolled-back transaction.
The relations: the state and output equal `DmlReference`'s; a DELETE's deleted
ids, and an UPDATE's or DELETE's output ids, equal the ids a SELECT of the same
join and WHERE matches, an UPDATE's changed ids are among them (a matched row
can keep its values), and an INSERT's rows equal its feeding SELECT's; each
output row reads back from the state; and every rewrite leaves the same state
and output.
Three facts per lane guard the comparisons: a negated WHERE must change the
state, a reference that skips one matched row must be caught, and at least 75%
of the generated statements must change a row (the generators retry one that
changes none). The generators leave out each shape an engine rejects or reads
differently from the reference, with the reason at the site; the engine
readings `dbms-differences.md` records have twins in the SQLite and MySQL lane
classes.

- **After editing or adding a model**, run `bash tools/regen-pict.sh` (it builds
  PICT v3.7.4 on first use and regenerates every model's CSV; an unchanged
  model's CSV comes out byte for byte the same) and commit the CSV with the
  model. A new model also needs its binding, or `PictModelTests` fails.
  `PictModelTests` runs every check per model: it fails the PR on a value
  outside the model, a row its constraints or the analyzer's verdicts exclude,
  and a valid pair or sub-model tuple no row holds. It does not compare the CSV
  with PICT's output, so a relaxed constraint whose new combinations span three
  or more factors passes it.
- **`CombinatorialBuildTests`** builds, with no database, every statement a lane
  would run for every row of every model with a generator (each seed's statement,
  its rewrites and relation parts, and the negated WHERE) for the row's engine.
  Any exception fails it, naming the row, seed, C# chain and exception, so a
  `Build()` guard the model or generator has drifted from fails the PR rather
  than the nightly run. It takes about a second.
- **A failure report** names the CSV row, the CsCheck seed, the C# chain, the
  SQL, the bound values and the expected and actual rows. The first failing
  row of a lane then adds a shrunk case CsCheck found in 20 s, with its own
  seed; later rows skip the search, so a lane with many failures stays quick.
  `SQLARTISAN_RANDOMIZED_SHRINK_SECONDS` gives every failure that budget
  instead (`0` skips the search).
- **Reproduce one case** by pinning the seed and filtering to its row (either
  seed from the report works, the original or the shrunk one):

  ```bash
  SQLARTISAN_RANDOMIZED_SEED=<seed> SQLARTISAN_RANDOMIZED_SHRINK_SECONDS=0 \
    dotnet test tests/SqlArtisan.IntegrationTests \
    --filter "FullyQualifiedName~SqliteCombinatorialSelectTests&DisplayName~row: 5,"
  ```

  With the seed set, every selected row runs that one query instead of its
  three, so keep the filter to the reported row. A DML row's display name leads
  with its model (`model: "update", row: 5,`), so filter on both.
- **Triage** a failure as unsupported per the matrix (tighten the model),
  decided in `.claude/rules/` or `docs/` (encode it in the model or the
  generator), a gap in the model, generator or reference (fix it), or a
  candidate library defect (minimise it and file an issue; fixing it lands a
  fixed test in the existing per-engine test file).

## Notes

- The Oracle CTE-column-alias bug (#165, re-aliased CTE column → `ORA-00904`)
  is **fixed**; `OracleTests.Cte_AliasedColumn_Executes` stands as its active
  regression guard (a re-aliased CTE column now emits a bare alias that resolves
  on Oracle). The still-skipped Oracle tests are the ones XE 21c genuinely can't
  run — no native boolean (`is_active` is `NUMBER(1)`), no multi-row `VALUES`.
- This complements `sa-run-sql-harness` (which only *observes* the emitted string)
  and `sa-run-benchmark`: use this when "does it actually run on the engine?" is the
  question.
