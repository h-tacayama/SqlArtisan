# Spike: opt-in dialect analyzer (the "(B) permissive + opt-in prevention" layer)

> **Status:** PoC on branch `claude/dbms-analyzer-poc` (separate from the
> `claude/dbms-namespace-spike` exploration, which is preserved untouched).

## What this is

A Roslyn analyzer that sits **on top of the permissive single `SqlArtisan.Sql`
API** and flags calls to functions the project's **target DBMS** does not support
— without splitting namespaces, adding generics, or changing the public API.

It is the missing piece for option **(B)** from the namespace decision spike: keep
one clean, permissive API ("any DBMS's SQL is writable"), and offer
*discoverability/safety as an opt-in layer* for those who want it.

## How it works

| Piece | Location |
|-------|----------|
| Analyzer (`SQLA0001`) | `src/SqlArtisan.Analyzer/TargetDbmsFunctionAnalyzer.cs` |
| MSBuild opt-in property wiring | `src/SqlArtisan.Analyzer/build/SqlArtisan.Analyzer.props` |
| In-memory test harness + tests | `tests/SqlArtisan.Analyzer.Tests/` |

A consuming project opts in:

```xml
<PropertyGroup>
  <SqlArtisanTargetDbms>Oracle</SqlArtisanTargetDbms>
</PropertyGroup>
```

Then `Sql.Ceiling(...)` (no `CEILING` on Oracle) yields a **warning**:

```
SQLA0001: 'Ceiling' is not available on Oracle; it is supported on: MySql, PostgreSql, Sqlite, SqlServer
```

## Design choices (why this fits the philosophy)

- **Opt-in.** With no `<SqlArtisanTargetDbms>`, the analyzer does nothing. The
  permissive API is unchanged for everyone else.
- **Warning, never error** (consumers can promote via `.editorconfig` if they
  want). It guides; it never blocks "the SQL you write is the SQL that runs".
- **Degradable.** Functions absent from the catalog are ignored — a missing
  matrix entry means *no warning*, not a broken build (avoids the "wrong matrix is
  worse than none" trap; the analyzer only claims **existence** checks, not
  semantics).
- **No API surface change.** One namespace, no generics, no per-dialect facades.
  Docs and examples stay unified.

## What it proves (5 passing tests)

| Claim | Test |
|-------|------|
| Flags an unsupported function for the target | `Flags_Ceiling_When_Target_Is_Oracle` |
| Allows the supported spelling | `Does_Not_Flag_Ceil_When_Target_Is_Oracle` |
| Never flags universal functions | `Does_Not_Flag_Universal_Abs` |
| Off until opted in | `Is_Silent_When_No_Target_Configured` |
| Correct per target | `Does_Not_Flag_Ceiling_When_Target_Is_SqlServer` |

```bash
dotnet test tests/SqlArtisan.Analyzer.Tests   # 5 passed
```

## Scope / what it deliberately does NOT do

- **Existence only.** Like every option in the namespace spike, it cannot catch
  *semantic* divergence (rounding mode, `GROUP_CONCAT` truncation, PG numeric-only
  2-arg `ROUND`) or a target-vs-actual-connection mismatch.
- **Catalog is a 5-row numeric stub.** The real version consumes the verified
  full matrix (the same artifact every option needs).
- **Function calls only.** Extending to clause-level verbs (UPSERT/MERGE) is
  straightforward (match the builder method names) but not in this PoC.

## How it compares to the namespace options

| | Existence check | Mixing check | API change | Ergonomics | Opt-in |
|---|---|---|---|---|---|
| Analyzer (this) | ✅ build warning | ❌ (n/a) | **none** | clean | ✅ |
| ② hybrid | ✅ compile (fn) | runtime guard | namespaces (fn) | clean | no |
| ③ extensions | ✅ compile | runtime guard | namespaces | 1 ns/file | no |
| ④ phantom | ✅ compile | ✅ compile | generics everywhere | heavy | no |

The analyzer trades the *compile-time* guarantee for **zero API cost + opt-in**,
matching the jOOQ-style "permissive core + dialect validation" model recommended
for SqlArtisan's audience.
