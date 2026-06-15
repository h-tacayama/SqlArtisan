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

A consuming project opts in, **per file/folder via `.editorconfig`** (preferred —
analyzer config lives there, and it scopes by path):

```ini
[*.cs]
sqlartisan_target_dbms = postgresql            # default for the project

[src/Integrations/Oracle/**.cs]
sqlartisan_target_dbms = oracle                # this folder is checked against Oracle

dotnet_diagnostic.SQLA0001.severity = warning  # severity lives here too
```

…or project-wide via an MSBuild property (fallback):

```xml
<PropertyGroup>
  <SqlArtisanTargetDbms>Oracle</SqlArtisanTargetDbms>
</PropertyGroup>
```

The analyzer resolves the target **per file** (`.editorconfig` glob) first, then
the project-wide MSBuild property. Then `Sql.Ceiling(...)` (no `CEILING` on Oracle)
yields a **warning**:

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
| **Per-file `.editorconfig` scoping** (one project, many DBMS) | `PerFile_EditorConfig_Scopes_Each_File_To_Its_Own_Dbms` |
| …and the supported spelling per file is allowed | `PerFile_Supported_Spelling_Is_Not_Flagged` |

```bash
dotnet test tests/SqlArtisan.Analyzer.Tests   # 7 passed
```

### Mixing multiple DBMS in one project

`.editorconfig` scopes by **file path**, so a single project that targets several
DBMS can restrict each **folder/file individually** (verified: in one compilation,
an Oracle-scoped file flags `Ceiling` while a SqlServer-scoped file flags `Ceil`,
at the same time). The granularity is **per file** — the analyzer reads the target
from each file's effective `.editorconfig`.

What it **cannot** do: distinguish two DBMS **within the same file** (one path → one
target). True per-*call-site* discrimination in a mixed file is only achievable by
encoding the DBMS in the code itself — i.e. **phantom types** (`Ceil<Oracle>(...)`,
see the `claude/dbms-namespace-spike` Step 5). In practice multi-DBMS code is
usually separated by folder/module, which `.editorconfig` handles cleanly.

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
