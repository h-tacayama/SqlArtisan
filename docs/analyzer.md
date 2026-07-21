# Roslyn Analyzer

[← Back to Reference Home](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/README.md)

The analyzer is the second layer of SqlArtisan's deterministic guard-rail
stack — after compile-time type safety and before exact-SQL tests. It warns
at build time when your code uses a construct that is not supported on your
project's target dialect. It ships inside the `SqlArtisan` package — no
extra package reference — and is completely silent until you configure a
target.

## Contents

- [Enabling it](#enabling-it)
- [Rules](#rules)
- [Correcting a warning: the override keys](#correcting-a-warning-the-override-keys)
- [Context rules (SQLA0003)](#context-rules-sqla0003)
- [Mixed-dialect projects](#mixed-dialect-projects)
- [CI gates and stricter enforcement](#ci-gates-and-stricter-enforcement)
- [Verified-against versions](#verified-against-versions)
- [Reserved: version-aware warnings](#reserved-version-aware-warnings)
- [Known limitations](#known-limitations)

---

## Enabling it

Set a target dialect in `.editorconfig`:

```ini
root = true

[*.cs]
sqlartisan_target_dbms = postgresql   # mysql | oracle | postgresql | sqlite | sqlserver
```

Or, if you prefer an MSBuild property (e.g. in `Directory.Build.props`):

```xml
<PropertyGroup>
  <SqlArtisanTargetDbms>postgresql</SqlArtisanTargetDbms>
</PropertyGroup>
```

`.editorconfig` wins when both are set. With no target configured either way,
the analyzer never reports anything — enabling it is purely additive.

---

## Rules

| ID | Severity | Reports |
|---|---|---|
| `SQLA0001` | Warning | A `sqlartisan_target_dbms` or `sqlartisan_construct_*` value could not be recognized. |
| `SQLA0002` | Warning | A SqlArtisan construct is used against the configured target dialect, and the dialect matrix has a **verified** entry saying that dialect doesn't support it. |
| `SQLA0003` | Warning | A construct the target dialect supports, used in a syntactic position that dialect rejects it in — see [Context rules](#context-rules-sqla0003). |
| `SQLA0004` | Warning | A compile-time identifier literal — a table or expression alias, a CTE or derived-table name, a `VALUES` column name, or the Oracle `RETURNING` output variable — is longer than the target dialect allows. |

`SQLA0001` is a compilation-end diagnostic reported once per distinct
(key, value) with no file location: it appears in **build** output (CLI and
CI, and an IDE's Error List after an explicit build — check that the list's
source filter includes Build entries), but not in the editor's live
analysis, which never runs compilation-end actions. `SQLA0002`, by
contrast, is a per-usage diagnostic and shows up live as you type.

`SQLA0002` only ever fires for a construct the matrix has confirmed — one
without a matrix entry stays silent rather than guessed at, so an incomplete
matrix can under-warn but never produce a false positive.

`SQLA0004` checks compile-time identifier literals — table and expression
aliases (`.As(...)`), CTE and derived-table names, `VALUES` column names, and
the Oracle `RETURNING ... INTO` output variable — against each dialect's
identifier-length limit: MySQL 256 characters, Oracle 128 bytes, PostgreSQL
63 bytes, SQL Server 128 characters (SQLite is unbounded and never warns).
MySQL's figure is its **alias** limit: the checked positions are aliases, and
MySQL allows those up to 256 characters, well past its 64-character table and
column names. PostgreSQL leads the list because it does not error on an
over-long identifier — it truncates it (with only a notice, not an error), so
two distinct long names can collide after truncation, and the analyzer is the
only place this surfaces before the database does. Oracle and PostgreSQL
measure in UTF-8 bytes, so a multi-byte identifier reaches the limit sooner
than its character count suggests. Only constant identifiers are checked — a
name built at run time is left alone — and, like `SQLA0002`, it is a per-usage
diagnostic suppressible at a single site (`#pragma warning disable SQLA0004`, a
`[SuppressMessage]` attribute, or `dotnet_diagnostic.SQLA0004.severity`).

```csharp
using static SqlArtisan.Sql;

// sqlartisan_target_dbms = mysql
var g = Rollup(t.Code, t.Name);
// warning SQLA0002: 'Rollup' is not supported on MySQL. Set
// 'sqlartisan_construct_rollup = supported' in .editorconfig if your
// engine version supports it.
```

Severity is controlled the standard Roslyn way, per rule ID:

```ini
dotnet_diagnostic.SQLA0002.severity = error   # promote to a build error
dotnet_diagnostic.SQLA0001.severity = none    # suppress entirely
```

Because severity is per rule ID, it cannot be scoped to one construct —
promoting `SQLA0002` to `error` makes *every* dialect mismatch a build
failure, not just a chosen one. If you need to forbid a specific construct
as a team policy rather than a dialect fact, reach for
[`BannedApiAnalyzers`](https://www.nuget.org/packages/Microsoft.CodeAnalysis.BannedApiAnalyzers)
instead — `sqlartisan_construct_*` expresses "my engine actually
supports/doesn't support this," not "we've decided not to use this."

---

## Correcting a warning: the override keys

Every `SQLA0002` message names the override key that would silence it. Two
kinds exist:

- **Member-level** — `sqlartisan_construct_<name>` — applies to *every*
  overload of that member, including ones added in a future SqlArtisan
  version. Use this when your intent is "this function," not one specific
  shape of it.
- **Arity-level** — `sqlartisan_construct_<name>_arity<N>` — applies only to
  the overload with exactly `N` declared parameters. Use this when the
  matrix (or your own knowledge) says only one shape of an overloaded member
  differs — e.g. `StringAgg`'s 3-argument inline-`ORDER BY` form is
  PostgreSQL-only while its 2-argument form also runs on SQL Server:

  ```ini
  sqlartisan_construct_string_agg_arity3 = unsupported   # only the 3-arg form
  ```

Name conversion is mechanical: each capitalized segment of the C# member
name becomes a lowercase, underscore-separated word — `MergeInto` →
`merge_into`, `DateTrunc` → `date_trunc`. A member with no internal capital
(`Dateadd`, from the underscore-free SQL token `DATEADD`) stays one word:
`dateadd`. The arity suffix is spelled out (`_arity2`, never a bare `_2`) so
it can't collide with a member name that itself ends in a digit (`Atan2` →
`atan2` is a different key from `Atan`'s 2-argument form, `atan_arity2`).

An overloaded C# operator is keyed by its CLR method name: `%` is
`op_Modulus`, so its key is `sqlartisan_construct_op_modulus`. No need to
memorize the mapping — the warning message names the exact key, so it can
be copied from there.

Both keys accept two values:

| Value | Meaning |
|---|---|
| `supported` | Silences the warning — your engine version handles this construct even though the shipped matrix doesn't (yet) confirm it. |
| `unsupported` | Forces the warning even where the matrix says the dialect is fine — useful if your specific engine version, fork, or configuration doesn't actually support it. |

An arity-level key always wins over a member-level key for the same member,
and any override always wins over the shipped matrix:

```ini
sqlartisan_target_dbms = postgresql
sqlartisan_construct_merge_into = supported   # e.g. targeting PostgreSQL 15+, where MERGE landed
```

A typo in a key name is not detectable — Roslyn does not let an analyzer
enumerate which `.editorconfig` keys exist, only look one up by exact name —
so a misspelled override silently does nothing. If a warning doesn't clear
after adding one, check the key against the message text exactly.

---

## Context rules (SQLA0003)

A construct can be valid on a dialect in one position and rejected by the same
engine in another. The construct-level warnings above cannot express that —
the construct itself *is* supported — so these facts ship as **context
rules**: `SQLA0003` fires when the offending position is visible in the
expression where the construct is used. Two rules ship today, both MySQL
facts, live-verified against the engine like every matrix entry.

**`LIMIT` inside an `IN` / `ANY` / `ALL` / `SOME` subquery.** MySQL rejects a
row-limited query directly under these positions ("This version of MySQL
doesn't yet support 'LIMIT & IN/ALL/ANY/SOME subquery'") — route the limited
query through a derived table or CTE instead. Scalar, `EXISTS`, CTE, and
derived-table positions accept `LIMIT` and stay silent.

```csharp
// sqlartisan_target_dbms = mysql
var q = Select(u.Id).From(u)
    .Where(u.Id.In(Select(o.UserId).From(o).OrderBy(o.UserId).Limit(2)));
// warning SQLA0003: 'Limit' is not supported inside an IN/ANY/ALL/SOME subquery on MySQL
```

**`GROUPING()` outside a `WITH ROLLUP` query.** MySQL accepts `Grouping(...)`
only when the query's `GROUP BY` carries the `WITH ROLLUP` suffix — chain
`.WithRollup()` after `.GroupBy(...)`.

```csharp
// sqlartisan_target_dbms = mysql
var q = Select(u.DepartmentId, Grouping(u.DepartmentId))
    .From(u).GroupBy(u.DepartmentId).OrderBy(u.DepartmentId);
// warning SQLA0003: 'Grouping' is not supported outside a WITH ROLLUP query on MySQL
```

A context rule warns only when the position is provable from the expression
itself. A subquery held in a variable, a builder chain continued from a
helper method, or any shape the analyzer doesn't recognize stays silent —
the same under-warn-but-never-false-positive principle the matrix follows.
The absence side is equally strict: `Grouping` warns only when the chain
shows a call *after* `.GroupBy(...)` that isn't `.WithRollup()` — from that
point the builder's type can never accept the suffix — and a chain that
still ends at `.GroupBy(...)` stays silent.

Suppression is per rule ID, the standard Roslyn way
(`#pragma warning disable SQLA0003`, a `[SuppressMessage]` attribute, or
`dotnet_diagnostic.SQLA0003.severity`). The `sqlartisan_construct_*`
override keys do **not** apply here — they answer "does my engine support
this construct," which is not what a context rule reports.

---

## Mixed-dialect projects

`.editorconfig` sections scope by file path, so a project that emits SQL for
more than one engine can give each area its own target:

```ini
root = true

[src/Reporting/Postgres/**.cs]
sqlartisan_target_dbms = postgresql

[src/Reporting/MySql/**.cs]
sqlartisan_target_dbms = mysql
```

The analyzer resolves the target per source file, so this is the supported
pattern for mixed-dialect codebases — there is no per-call target inference
(e.g. reading the literal argument to `.Build(Dbms.MySql)`); a file's target
comes only from its `.editorconfig` scope or the MSBuild property.

---

## CI gates and stricter enforcement

**Promote a dialect mismatch to a hard failure:**

```ini
dotnet_diagnostic.SQLA0002.severity = error
```

Every confirmed mismatch fails the build; escape hatches (`supported`
overrides) still apply first, so only genuinely unconfirmed constructs fail.

**Whitelist mode** — fail on anything the matrix hasn't explicitly verified
one way or the other is not offered as a separate rule. `SQLA0002` only
fires on a *confirmed* mismatch by design (a construct the matrix doesn't
know stays silent rather than guess), so there is no "unverified construct"
diagnostic to promote — the matrix's
completeness is the whole safety net, and it is enforced in this repository:
a coverage test fails when a public method, property, field, or overloaded
operator ships without
a matrix entry or a documented dialect-neutral exclusion, and an
integration-test sweep executes the entries against a live engine per
dialect (the versions in the table below) asserting the accept/reject
outcome matches the matrix both ways.

---

## Verified-against versions

The matrix's `verified` entries were checked against one representative
version per dialect (the same engines the integration test matrix runs
against):

| Dialect | Verified against |
|---|---|
| MySQL | MySQL 8.0 |
| Oracle | Oracle Database XE 21c (the `Testcontainers.Oracle` module's default `gvenzl/oracle-xe:21.3.0-slim-faststart` image) |
| PostgreSQL | PostgreSQL 16 |
| SQLite | the SQLite build bundled with `Microsoft.Data.Sqlite` 9.0.5 |
| SQL Server | SQL Server 2022 |

An older or newer engine version may disagree with a `false` entry in
either direction — that's what the `supported`/`unsupported` overrides are
for, not a bug in the matrix.

---

## Reserved: version-aware warnings

`sqlartisan_target_version` (and its MSBuild counterpart,
`<SqlArtisanTargetVersion>`) is a **reserved key**. Setting it today does
nothing; a future release will use it to make the warnings version-aware —
matrix entries will optionally carry a minimum engine version per dialect,
so a construct newer than your declared engine (say, `MERGE` before
PostgreSQL 15, or `DATETRUNC` before SQL Server 2022) warns just like a
plain dialect mismatch does today.

The reservation pins down now what the future release will not change:

- **Value format.** The engine's own version spelling, the same one this
  documentation's dialect notes use — `8.0.16` for MySQL, `23` for Oracle,
  `16` for PostgreSQL, `3.44` for SQLite, `2022` for SQL Server. Versions
  compare by numeric segment (`8.0.20` is newer than `8.0.16`); trailing
  letters in a segment are ignored (`23ai` reads as `23`).
- **Unset means today's behavior.** With no declared version, version
  bounds never fire and the analyzer behaves exactly as the rest of this
  page describes. The key also has no effect without
  `sqlartisan_target_dbms` — a version alone identifies no engine.
- **Your overrides keep the last word.** A version bound refines the
  shipped matrix's verdict, not yours: resolution stays *your arity-level
  override → your member-level override → the matrix (version-refined) →
  silence*, so `supported` / `unsupported` keys silence or force the
  warning exactly as they do today.
- **No new false positives.** A version bound only ever refines a construct
  the matrix already has an entry for; a construct without an entry stays
  silent whether or not a version is declared.
- **Same plumbing as the target key.** Resolved per source file,
  `.editorconfig` wins over the MSBuild property, and an unrecognized value
  is flagged as `SQLA0002` and otherwise treated as unset.

---

## Known limitations

- **No cross-call inference.** The target comes from `.editorconfig`/MSBuild
  scope only; a literal `.Build(Dbms.MySql)` argument elsewhere in the file
  is not read as a hint.
- **Same-arity, different-type overloads share one key.** Two overloads of
  the same name with the same *parameter count* but different parameter
  *types* (e.g. `Match(object, params object[])` for MySQL vs.
  `Match(DbTableBase, object)` for SQLite) cannot be told apart by the
  `sqlartisan_construct_*` key scheme — the shipped matrix enters the
  *union* of their support rather than guess, which can under-warn but
  never false-positive.
- **A construct whose dialect support depends on the runtime value of an
  argument, not its declared type or arity, is not modeled at all.**
  `Trunc(expr[, format])` is the example: a numeric argument is
  Oracle+PostgreSQL, a date/time argument is Oracle-only, and both shapes
  compile to the exact same C# overload. It has no matrix entry and never
  warns either way.
- **Typo'd keys fail silently** (see above) — there is no diagnostic for an
  unrecognized `sqlartisan_construct_*` *key name*, only for a recognized
  key with an unrecognized *value*. Value validation covers the keys derived
  from the matrix; an override key naming a member the matrix has no entry
  for is honored when its value is valid, but a typo in its *value* is
  silently ignored too.
- **Absence of an entry still means silence, not endorsement.** The matrix
  covers every referencable public method, property, field, and overloaded
  operator except a
  documented set of dialect-neutral plumbing (`Build`, the result and
  configuration objects) and `Trunc` above — gate-enforced by the
  repository's tests — but a member in that excluded set never warns either
  way. See
  [`DialectMatrix.cs`](https://github.com/h-tacayama/SqlArtisan/blob/main/src/SqlArtisan.Analyzers/DialectMatrix.cs)
  for what's entered.
