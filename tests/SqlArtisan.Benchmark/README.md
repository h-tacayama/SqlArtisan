# SqlArtisan.Benchmark

The [BenchmarkDotNet](https://benchmarkdotnet.org/) suite behind the numbers in
the root [README's Performance section](https://github.com/h-tacayama/SqlArtisan/blob/main/README.md#performance).
The numbers live there and only there; this page is how you reproduce them.

## Running it

```bash
dotnet run -c Release -- validate                          # equivalence check, not a measurement
dotnet run -c Release -- --filter *SqlBuilderBenchmarks*   # the cross-library comparison
dotnet run -c Release                                      # pick a suite interactively
```

`validate` is the one-time check that the entrants are actually comparable. It
prints each entrant's SQL and parameter count and asserts that everyone required
to parameterize the query produced exactly two bind parameters. SQL text is *not*
required to be byte-identical — dialects, alias generation, and EF Core's
pipeline all differ — only the logical query and the parameter count. Run it
after touching any entrant; a change that quietly stops parameterizing would
otherwise look like a win.

**Release configuration is required.** BenchmarkDotNet refuses to run a Debug
build, and a measurement taken on a shared or virtualized host is not comparable
to the root README's figures.

## What is measured

Every entrant builds the SQL string **and** its bind-parameter collection for the
same logical query, so the comparison is like-for-like:

```csharp
Select(u.Id.As("user_id"), u.Name.As("user_name"), Count(o.Id).As("order_count"))
    .From(u)
    .InnerJoin(o).On(u.Id == o.UserId)
    .Where(o.OrderDate >= new DateTime(2024, 1, 1)
        & o.OrderDate < new DateTime(2025, 1, 1))
    .GroupBy(u.Id, u.Name)
    .OrderBy(Count(o.Id).As("order_count").Desc)
    .Build();
```

An `INNER JOIN` plus a `GROUP BY` aggregate, filtered by two date parameters, on
the PostgreSQL dialect. Each `[Benchmark]` returns the produced
`(Sql, ParameterCount)` tuple so BenchmarkDotNet consumes both outputs and cannot
dead-code-eliminate the work.

Entrants fall into three categories, and only one of them is a comparison:

| Category | Entrants | Why |
|---|---|---|
| `Builders` | SqlArtisan (twice — specific and Dapper dynamic parameters), Dapper.SqlBuilder, InterpolatedSql, linq2db, Sqlify, SqlKata | The like-for-like set |
| `Baseline` | A hand-written `StringBuilder` + Dapper `DynamicParameters` | The floor — no type safety, no dialect handling |
| `ORM reference` | EF Core | Materially different work; shown only for scale |

linq2db and EF Core cache compiled queries and reuse a long-lived connection or
context created once in `[GlobalSetup]`, so the loop measures warm steady state
rather than first-call cost.

`SqlBuildingBufferBenchmark` covers `SqlBuildingBuffer` on its own, away from
the cross-library comparison.

## Pinned library versions

The comparison is only meaningful against the versions it was run with. These
are pinned in `SqlArtisan.Benchmark.csproj`; update this table when they move.

| Package | Version |
|---|---|
| BenchmarkDotNet | 0.15.8 |
| Dapper.SqlBuilder | 2.1.66 |
| InterpolatedSql | 2.5.1 |
| linq2db | 6.3.0 |
| Microsoft.EntityFrameworkCore | 8.0.11 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.11 |
| Sqlify | 0.3.14 |
| SqlKata | 4.0.1 |

## Reading the results

The allocation column is the firm one — the lightweight builders allocate the
same bytes every run. Treat the timing order as directional: run-to-run variance
grows for the heavier entrants, and it grows further on any host you do not
control.
