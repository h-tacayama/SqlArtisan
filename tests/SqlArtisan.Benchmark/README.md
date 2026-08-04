# SqlArtisan.Benchmark

The [BenchmarkDotNet](https://benchmarkdotnet.org/) suite behind the numbers in
the root [README's Performance section](https://github.com/h-tacayama/SqlArtisan/blob/main/README.md#performance).
The numbers live there and only there; this page is how you reproduce them.

## Running it

From the repo root. No database is needed — every entrant generates SQL offline,
so nothing here opens a connection.

```bash
P=tests/SqlArtisan.Benchmark
dotnet run --project $P -c Release -- validate                        # not a measurement
dotnet run --project $P -c Release -- --filter '*SqlBuilderBenchmarks*'
dotnet run --project $P -c Release                                    # pick a suite
```

Keep the filter quoted. Run from the project directory instead and the unquoted
pattern matches `SqlBuilderBenchmarks.cs`, so the shell hands BenchmarkDotNet a
filename that matches no benchmark.

`validate` asserts that each entrant built the shared query: exactly two bind
parameters, `SELECT`/`FROM`/`JOIN`/`WHERE`/`GROUP BY`/`ORDER BY` present, in
order, and none of them fused onto the text before it, an aggregate outside the
sort, and two `GROUP BY` keys. It checks that
shape rather than the text, because dialects and alias generation spell the
same query differently — and it checks nothing else, so a wrong join or filter
would still pass. The EF Core reference is printed but not checked. Run it after
touching any entrant; a change that quietly stops parameterizing, stops
aggregating, or glues a template clause onto its neighbor, would otherwise look
like a win.

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
the PostgreSQL dialect. Every entrant in `SqlBuilderBenchmarks` returns the
produced `(Sql, ParameterCount)` tuple so BenchmarkDotNet consumes both outputs
and cannot dead-code-eliminate the work.

Entrants fall into three categories, and only one of them is a comparison:

| Category | Entrants | Why |
|---|---|---|
| `Builders` | SqlArtisan (twice — specific and Dapper dynamic parameters), Dapper.SqlBuilder, InterpolatedSql, linq2db, Sqlify, SqlKata | The like-for-like set |
| `Baseline` | A hand-written `StringBuilder` + Dapper `DynamicParameters` | The floor — no type safety, no dialect handling |
| `ORM reference` | EF Core | Materially different work; shown only for scale |

linq2db and EF Core reuse a connection object and a `DbContext` created once in
`[GlobalSetup]` — neither is ever opened — and EF Core caches the compiled query
plan, so the loop measures warm steady state rather than first-call cost.

## Pinned library versions

The comparison is only meaningful against the versions it was run with. The
direct ones are pinned in `SqlArtisan.Benchmark.csproj`; update this table when
they move. The last two rows are transitive — listed because their versions
float free of any direct pin, so a restore can move them without touching the
csproj. Other transitive packages in the measured path (EF Core's `Relational`
and caching assemblies) move in lockstep with the direct pin above them.

| Package | Version | |
|---|---|---|
| BenchmarkDotNet | 0.15.8 | |
| Dapper.SqlBuilder | 2.1.66 | |
| InterpolatedSql | 2.5.1 | |
| linq2db | 6.3.0 | |
| Microsoft.EntityFrameworkCore | 8.0.11 | |
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.11 | |
| Sqlify | 0.3.14 | |
| SqlKata | 4.0.1 | |
| Dapper | 2.1.66 | transitive; `DynamicParameters` is measured by the baseline, Dapper.SqlBuilder and `SqlArtisan+Dapper` |
| Npgsql | 8.0.6 | transitive; under the EF Core reference |

The runtime is part of this list too, and it has moved: the project targets
`net10.0`, while the root README's figures were taken on .NET 8. A fresh run
reproduces the *allocation* ordering, but **not** the timing ordering — the mid-
and heavy-weight entrants change places (a full run put Dapper.SqlBuilder ahead
of InterpolatedSql and SqlKata ahead of linq2db). Most rows land within a few
percent of the published bytes; SqlKata does not, because its entrant was
rebuilt after that table was measured (#382) and now allocates about half again
as much. Compare a fresh run against another fresh run, not against the published
table.

## Reading the results

The allocation column is the firm one — the lightweight builders allocate the
same bytes every run. Treat the timing order as directional: run-to-run variance
grows for the heavier entrants, and it grows further on any host you do not
control.
