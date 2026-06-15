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
| **Clause verb: `MergeInto`** flagged off Oracle/SqlServer | `Flags_MergeInto_When_Target_Has_No_Merge` / `Does_Not_Flag_MergeInto_On_Oracle` |
| **Clause verb: `OnDuplicateKeyUpdate`** flagged off MySQL | `Flags_OnDuplicateKeyUpdate_On_PostgreSql` |
| **Clause verb: `OnConflict`** flagged off PG/SQLite | `Flags_OnConflict_On_MySql` / `Does_Not_Flag_OnConflict_On_PostgreSql` |
| **Arity (`SQLA0003`)**: SQL Server `ROUND(x)` flagged, `ROUND(x,n)` ok | `Flags_SingleArg_Round_On_SqlServer` / `Does_Not_Flag_TwoArg_Round_On_SqlServer` |
| **Unknown target (`SQLA0002`)** for a typo'd value | `Flags_Unknown_Target_Value` |
| **Aliases / case-insensitive** (`mssql`, `ORACLE`) | `Accepts_Alias_Mssql_As_SqlServer` / `Target_Is_Case_Insensitive` |
| **Config precedence**: `.editorconfig` over MSBuild; MSBuild fallback | `EditorConfig_Overrides_MsBuild_Property` / `MsBuild_Property_Used_As_Fallback_When_No_EditorConfig` |

```bash
dotnet test tests/SqlArtisan.Analyzer.Tests   # 20 passed
```

### Clause-level verbs (UPSERT / MERGE), not just facade functions

The analyzer recognises both the `Sql` facade functions **and the fluent-builder
verbs** (`OnConflict`, `OnDuplicateKeyUpdate`, `MergeInto`) by matching any member
under the `SqlArtisan` namespace against the catalog. This directly closes the
hole that namespace option ② leaves open: in ②, `…OnDuplicateKeyUpdate().Build(Dbms.PostgreSql)`
is visible and emits invalid SQL silently at runtime — here it is a **build-time
warning**, with no API change and no generics.

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

## Delivery: how users get it

A Roslyn analyzer ships **inside a NuGet package** under `analyzers/dotnet/cs/`;
the compiler loads it automatically for any project that references the package.
Two delivery models:

1. **Bundled into the existing `SqlArtisan` package (recommended, proven here).**
   Anyone who installs `SqlArtisan` gets dialect checking with no extra step —
   and because it's *opt-in-silent*, it does nothing until a target DBMS is set.
   Crucially, an analyzer is **build-time only**: it lives in `analyzers/`, never
   `lib/`, so it adds **zero runtime weight** — the "lightweight core" value
   (about runtime/allocations) is fully preserved.
2. **Separate `SqlArtisan.Analyzer` companion package** (the "thin core, separate
   integration" pattern, like `SqlArtisan.Dapper`). Opt-in at install time.

Because the analyzer carries no runtime dependency, bundling does **not** make the
runtime library heavier — the usual reason to split a package does not apply here.

### Proven includable in the current library

`src/SqlArtisan/SqlArtisan.csproj` was wired to bundle the analyzer, and
`dotnet pack -c Release` produced a package containing:

```
lib/net8.0/SqlArtisan.dll                    (runtime — unchanged)
analyzers/dotnet/cs/SqlArtisan.Analyzer.dll  (build-time only)
build/SqlArtisan.props                        (<SqlArtisanTargetDbms> wiring)
buildTransitive/SqlArtisan.props              (flows to transitive consumers)
```

Wiring used: a `ProjectReference` with `ReferenceOutputAssembly="false"
OutputItemType="Analyzer"` (builds + applies the analyzer without adding it to the
library's dependency graph), a `TargetsForTfmSpecificBuildOutput` target placing
the DLL under `analyzers/dotnet/cs/`, and the props packed to
`build/buildTransitive` as `SqlArtisan.props` (name must match the package id to
auto-import).

**Recommendation:** bundle into `SqlArtisan` (zero runtime cost + opt-in-silent +
best discoverability). Keep the separate-package option only if the maintainer
wants analyzer versioning fully decoupled from the library.

## Completeness pass (three diagnostics, robust config)

To make the layer credible rather than a toy, the PoC now covers:

- **`SQLA0001` — existence**: function/clause-verb not available on the target
  (scalars, structural functions, and UPSERT/MERGE verbs).
- **`SQLA0003` — arity**: a function that needs more arguments on the target,
  e.g. SQL Server `ROUND(x)` is a syntax error (needs `ROUND(x, n)`). This shows
  the analyzer reaches *beyond pure existence* where the matrix encodes it.
- **`SQLA0002` — unknown target**: a typo'd `sqlartisan_target_dbms` (e.g.
  `Oracel`) is reported as its own error instead of silently flagging *everything*
  — the obvious footgun of config-driven checks, closed.
- **Target normalisation**: case-insensitive, with aliases (`postgres`/`pg` →
  PostgreSql, `mssql` → SqlServer); diagnostics print the canonical name.
- **Config precedence proven**: per-file `.editorconfig` overrides the project-wide
  MSBuild property (test `EditorConfig_Overrides_MsBuild_Property`); MSBuild is the
  fallback when no `.editorconfig` value applies.
- **Analyzer release tracking** (`AnalyzerReleases.*.md`) — the RS2008 suppression
  was removed; the analyzer is now production-shaped.

## Scope / what it deliberately does NOT do

- **No deep semantics.** It cannot catch *behavioural* divergence (rounding mode,
  `GROUP_CONCAT` truncation, PG's numeric-only 2-arg `ROUND` restriction) or a
  target-vs-actual-connection mismatch. Existence + arity are covered; meaning is
  not.
- **Catalog is a small numeric/clause stub.** The real version consumes the
  verified full matrix (the same artifact every option needs).
- **Name + namespace matching** (PoC heuristic). It flags catalog names on any
  `SqlArtisan.*` member; the production version would match the resolved symbols
  precisely. Both facade functions and clause verbs (UPSERT/MERGE) are covered.

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
