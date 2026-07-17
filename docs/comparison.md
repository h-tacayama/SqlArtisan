# Comparison Guide

[← Back to README](https://github.com/h-tacayama/SqlArtisan/blob/main/README.md) ·
[Reference Home](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/README.md)

How SqlArtisan compares to other ways of building SQL in .NET — on the axes
where the differences are real, not on a feature-checklist scorecard. Every
alternative mentioned here solves a real problem well; this page is about
which problem *yours* is.

## Contents

- [Philosophy](#philosophy)
- [Type Safety](#type-safety)
- [Machine-Verifiability](#machine-verifiability)
- [Dialect Handling](#dialect-handling)
- [Execution Story](#execution-story)
- [Performance](#performance)
- [When NOT to Choose SqlArtisan](#when-not-to-choose-sqlartisan)

---

## Philosophy

Four families of SQL tooling, each with a clear design center:

**Full ORMs** (EF Core) map objects to SQL. The SQL is an implementation
detail the ORM controls — you write LINQ expressions against a domain
model, and the provider translates them into engine-specific SQL. The
abstraction is the value: schema changes, migrations, change tracking,
and lazy loading live inside the ORM.

**LINQ-to-SQL data access** (linq2db) also translates LINQ expressions
into engine-specific SQL, but without the ORM scaffolding — no change
tracking, no migrations, no lazy loading. It is a lightweight data-access
layer that sits between a full ORM and a query builder.

**Portability-focused query builders** (SqlKata) offer a SQL-like API,
but a dialect compiler rewrites the output per engine. You write something
resembling SQL; the library decides the exact grammar that reaches the
database.

**Faithful query builders** (SqlArtisan) emit the SQL you write. The C#
maps directly to SQL tokens — bind-parameter markers and identifier quoting
are normalized, but SQL grammar is never rewritten. You target one engine
and get its full SQL surface, never flattened to a lowest common
denominator. Raw Dapper / ADO.NET shares this "you own the SQL" property,
but without type safety or builder structure.

---

## Type Safety

How much the compiler catches before the query reaches a database:

| Level | What the compiler checks | Examples |
|-------|-------------------------|----------|
| **Raw SQL strings** | Nothing — typos and schema drift surface at runtime | EF Core `FromSqlRaw`, raw Dapper, InterpolatedSql |
| **String-keyed builders** | Statement structure is typed, but table/column names are strings | Dapper.SqlBuilder, SqlKata |
| **Generated table classes** | Column references are compile-checked C# properties; an `ALTER TABLE` that drops a column breaks the build | SqlArtisan |
| **Expression-tree mapping** | Full compile-time model, tightest coupling between domain types and schema | EF Core, linq2db |

Expression-tree mapping (EF Core, linq2db) offers the tightest
compile-time safety. SqlArtisan's table classes sit one level below: column
names and statement structure are compile-checked, while the query itself
stays in the SQL domain — you see the SQL you are writing, and the compiler
still catches a renamed or deleted column.

---

## Machine-Verifiability

How much of a query is checked *before* it runs — and why this axis counts
double when queries are written by AI coding assistants, whose failure mode
is plausible-but-wrong SQL that compiles and looks right but does not do
what it should.

SqlArtisan puts three deterministic checks between generation and
production:

1. **Compile-time** — table classes turn column-name errors into build
   failures.
2. **Build-time** — the opt-in
   [Roslyn analyzer](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/analyzer.md)
   flags wrong-dialect constructs at build time, catching the most common
   AI failure mode (mixing dialects from training data).
3. **Test-time** — `Build()` is deterministic, so an exact-SQL unit test
   freezes the reviewed SQL as a regression contract.

Raw SQL strings verify nothing until the database rejects them.
String-keyed builders catch structural mistakes but not column typos.
Expression-tree ORMs catch the most at compile time, but the emitted SQL
is opaque — you cannot pin it with exact-SQL tests without coupling to the
ORM's internal translation.

The full setup for AI-assisted workflows is in the
[AI coding assistants guide](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/guides/ai-assistants.md).

---

## Dialect Handling

**Dialect compiler** (SqlKata, linq2db) — one API, output varies by
engine. The library maintains a grammar mapping; you call `Limit(10)` and
it emits `LIMIT 10`, `FETCH FIRST 10 ROWS ONLY`, or `TOP (10)` depending
on the target. This is convenient when the same query must run on multiple
engines.

**Per-dialect API** (SqlArtisan) — where dialects diverge, distinct methods
surface each engine's own syntax:

```csharp
Sequence("users_id_seq").Nextval   // Oracle:      users_id_seq.NEXTVAL
Nextval("users_id_seq")            // PostgreSQL:  NEXTVAL('users_id_seq')
NextValueFor("users_id_seq")       // SQL Server:  NEXT VALUE FOR users_id_seq
```

The opt-in Roslyn analyzer warns if you reach for a construct that is not
available on your target dialect, so a wrong-dialect call fails the build
rather than emitting unexpected SQL.

The trade-off: a dialect compiler is friendlier for teams targeting
multiple engines from one codebase; per-dialect APIs are for teams that
target one engine and want its full SQL surface without worrying about what
the compiler has mapped.

---

## Execution Story

| Model | How it works | Examples |
|-------|-------------|----------|
| **Bundled execution** | The library owns the connection; you call LINQ or a query method and it executes | EF Core, linq2db |
| **Bring-your-own execution** | The builder produces SQL + parameters; you execute with Dapper, ADO.NET, or any micro-ORM | SqlArtisan, Dapper.SqlBuilder, SqlKata |

Both SqlArtisan and SqlKata ship optional Dapper-based execution packages
(`SqlArtisan.Dapper` and `SqlKata.Execution` respectively) that add
one-liner query methods on top of the builder — so the experience is close
to "bundled" when you want it, without locking you to a specific executor.
SqlArtisan.Dapper additionally auto-detects the DBMS from the connection.
See the
[Dapper quickstart](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/guides/dapper-quickstart.md).

---

## Performance

SqlArtisan minimizes heap allocations — string buffers are recycled from a
pooled `ArrayPool<T>` — and on a
[like-for-like BenchmarkDotNet workload](https://github.com/h-tacayama/SqlArtisan/tree/main/tests/SqlArtisan.Benchmark)
it is the lowest-allocation and fastest query builder tested; only a
hand-written `StringBuilder` (with no type safety or dialect handling) is
lighter. See the
[benchmark table in the README](https://github.com/h-tacayama/SqlArtisan/blob/main/README.md#performance)
for the numbers.

**What the benchmark measures:** building the SQL string and its bind
parameters — the builder path only. It does not measure end-to-end query
execution against a database. EF Core is included as a full-ORM reference
doing fundamentally different work, not as a direct comparison target.

---

## When NOT to Choose SqlArtisan

An honest section makes the rest credible.

- **You need cross-database portability from one codebase.** If the same
  query must run unchanged on PostgreSQL today and SQL Server tomorrow,
  SqlKata's dialect compiler or linq2db's provider model handles that;
  SqlArtisan does not — portability is a deliberate non-goal.

- **You want full-ORM features.** Change tracking, migrations, lazy
  loading, and a domain-model-first workflow are EF Core's strengths.
  SqlArtisan is a query builder, not an ORM — it produces SQL strings, not
  object graphs.

- **Your team does not want to write SQL.** If the goal is to stay in C#
  and let the tooling figure out the SQL, EF Core or linq2db is a better
  fit — both translate LINQ expressions into engine-specific SQL.
  SqlArtisan is for developers who *want* to write SQL — type-safely and
  composably, but still recognizably SQL.

- **You need expression-tree-level schema safety.** EF Core and linq2db
  tie C# types to database columns through expression trees — the
  tightest compile-time coupling available. SqlArtisan's table classes
  check names but not column types.
