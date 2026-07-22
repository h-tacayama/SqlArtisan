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
- [Version-aware warnings (SQLA0003)](#version-aware-warnings-sqla0003)
- [Context rules (SQLA0004)](#context-rules-sqla0004)
- [Correlated DML target (SQLA0005)](#correlated-dml-target-sqla0005)
- [Mixed-dialect projects](#mixed-dialect-projects)
- [CI gates and stricter enforcement](#ci-gates-and-stricter-enforcement)
- [Verified-against versions](#verified-against-versions)
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
| `SQLA0003` | Warning | A construct is supported on the target dialect, but not at the declared `sqlartisan_target_version` — see [Version-aware warnings](#version-aware-warnings-sqla0003). |
| `SQLA0004` | Warning | A construct the target dialect supports, used in a syntactic position that dialect rejects it in — see [Context rules](#context-rules-sqla0004). |
| `SQLA0005` | Warning | A correlated UPDATE or DELETE has an unaliased target — the statement `Build()` rejects at run time, surfaced early; see [Correlated DML target](#correlated-dml-target-sqla0005). |
| `SQLA0006` | Warning | A compile-time identifier literal — a table or expression alias, a CTE or derived-table name, a `VALUES` column name, or the Oracle `RETURNING` output variable — is longer than the target dialect allows. |

`SQLA0001` is a compilation-end diagnostic reported once per distinct
(key, value) with no file location: it appears in **build** output (CLI and
CI, and an IDE's Error List after an explicit build — check that the list's
source filter includes Build entries), but not in the editor's live
analysis, which never runs compilation-end actions. `SQLA0002`, by
contrast, is a per-usage diagnostic and shows up live as you type.

`SQLA0002` only ever fires for a construct the matrix has confirmed — one
without a matrix entry stays silent rather than guessed at, so an incomplete
matrix can under-warn but never produce a false positive.

`SQLA0006` checks compile-time identifier literals — table and expression
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
diagnostic suppressible at a single site (`#pragma warning disable SQLA0006`, a
`[SuppressMessage]` attribute, or `dotnet_diagnostic.SQLA0006.severity`).

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

## Version-aware warnings (SQLA0003)

Some constructs are only newer than *some* engine versions on an otherwise
supported dialect — `MERGE` before PostgreSQL 15, `DATETRUNC` before SQL
Server 2022. Declare your engine's version with `sqlartisan_target_version`
and the matrix's version bounds warn on those too, as `SQLA0003` — the same
"this could break in production" fact `SQLA0002` reports for a dialect
mismatch, just for a version shortfall instead:

```ini
root = true

[*.cs]
sqlartisan_target_dbms = sqlserver
sqlartisan_target_version = 2019
```

```csharp
using static SqlArtisan.Sql;

var g = Datetrunc(DateTimePart.Day, "created_at");
// warning SQLA0003: 'Datetrunc' requires SQL Server 2022+ but the declared
// target version is 2019. Set 'sqlartisan_construct_datetrunc = supported'
// in .editorconfig if your engine supports it.
```

Or, if you prefer an MSBuild property:

```xml
<PropertyGroup>
  <SqlArtisanTargetVersion>2019</SqlArtisanTargetVersion>
</PropertyGroup>
```

- **Value format.** The engine's own version spelling, the same one this
  documentation's dialect notes use — `8.0.16` for MySQL, `23` for Oracle,
  `16` for PostgreSQL, `3.44` for SQLite, `2022` for SQL Server. Versions
  compare by numeric segment (`8.0.20` is newer than `8.0.16`, and a bare
  `8.0` reads as `8.0.0` — declare the precise patch version if an 8.0.x
  bound matters to you); trailing letters in a segment are ignored (`23ai`
  reads as `23`).
- **Unset means today's behavior.** With no declared version, version
  bounds never fire and the analyzer behaves exactly as the rest of this
  page describes. The key also has no effect without
  `sqlartisan_target_dbms` — a version alone identifies no engine.
- **Your overrides keep the last word.** A version bound refines the
  shipped matrix's verdict, not yours: resolution stays *your arity-level
  override → your member-level override → the matrix (version-refined) →
  silence*, so `supported` / `unsupported` keys silence or force the
  warning exactly as they do today — `sqlartisan_construct_datetrunc =
  supported` silences the example above even with `2019` still declared.
- **No new false positives.** A version bound only ever refines a construct
  the matrix already has an entry for; a construct without an entry stays
  silent whether or not a version is declared.
- **Same plumbing as the target key.** Resolved per source file,
  `.editorconfig` wins over the MSBuild property, and an unrecognized value
  is flagged as `SQLA0001` and otherwise treated as unset.

Suppression is per rule ID, the standard Roslyn way
(`#pragma warning disable SQLA0003`, a `[SuppressMessage]` attribute, or
`dotnet_diagnostic.SQLA0003.severity`).

### Version-bound constructs

Every construct below has a recorded minimum version on the named dialect;
below that version the matrix's plain `supported`/`not supported` verdict
holds instead — declaring no version, or a version at or above the bound,
reproduces that verdict exactly.

| Construct | Dialect | Minimum version | Why |
|---|---|---|---|
| `WithRecursive` | MySQL | 8.0 | `WITH RECURSIVE` needs MySQL's CTE support, added in 8.0. |
| `Grouping` (1-argument form) | MySQL | 8.0.1 | `GROUPING(expr)` landed in 8.0.1. |
| `Except`, `Intersect`, `ExceptAll`, `IntersectAll` | MySQL | 8.0.31 | `EXCEPT`/`INTERSECT` landed in 8.0.31. |
| `Nowait`, `SkipLocked` | MySQL | 8.0 | `FOR UPDATE NOWAIT`/`SKIP LOCKED` need 8.0. |
| `OnDuplicateKeyUpdate`, `Excluded` | MySQL | 8.0.19 | SqlArtisan always emits the row-alias UPSERT form (`... AS new ON DUPLICATE KEY UPDATE col = new.col`), which needs the row alias MySQL added in 8.0.19 — the pre-8.0.19 `VALUES()` function form is never emitted. |
| `JsonValue` | MySQL | 8.0.21 | `JSON_VALUE` landed in 8.0.21. |
| `Except`, `ExceptAll`, `IntersectAll`, `MinusAll` | Oracle | 21 | `EXCEPT`/`INTERSECT` (and their `ALL` forms) landed in Oracle 21c — live-verified forward-compatible on Oracle 23ai too. |
| `MergeInto`, `Using`, `WhenMatched`, `WhenNotMatched`, `ThenInsert`, `ThenUpdateSet`, `ThenDelete`, the 3-argument `Values` (MERGE `USING` literal rows) | PostgreSQL | 15 | `MERGE` landed in PostgreSQL 15. |
| `RegexpLike`, `RegexpCount`, `RegexpReplace`, `RegexpSubstr` | PostgreSQL | 15 | The `REGEXP_*` function family landed in PostgreSQL 15. |
| `RightJoin`, `FullJoin`, `NaturalRightJoin`, `NaturalFullJoin` | SQLite | 3.39 | `RIGHT JOIN`/`FULL JOIN` landed in SQLite 3.39. |
| `Returning` | SQLite | 3.35 | `RETURNING` landed in SQLite 3.35. |
| `StringAgg` (both overloads), `Concat` (both overloads) | SQLite | 3.44 | `string_agg`/`concat` landed in SQLite 3.44. |
| `NullsFirst`, `NullsLast` | SQLite | 3.30 | `NULLS FIRST`/`NULLS LAST` landed in SQLite 3.30. |
| `Trim` (1-argument form) | SQL Server | 2017 | `TRIM(...)` landed in SQL Server 2017. |
| `Datetrunc`, `Greatest`, `Least`, the 2-argument `Ltrim`/`Rtrim`/`Trim` forms | SQL Server | 2022 | `DATETRUNC`, `GREATEST`/`LEAST`, and the trim-characters overloads all landed in SQL Server 2022. |

### Sources for these version bounds

Every minimum version above is drawn from the vendor's own documentation,
linked below — and each is more than a citation: the integration suite runs
the construct against a live engine at that dialect's verified baseline, so
the "supported from version N" direction is reproduced, not just quoted. The
"unsupported below N" direction rests on the documentation alone, because the
suite does not pin a below-baseline image of every engine.

**MySQL** — the reference manual gives the introducing release for each:

- [`WITH RECURSIVE` / common table expressions](https://dev.mysql.com/doc/refman/8.0/en/with.html) — 8.0.1.
- [`GROUPING()` under `WITH ROLLUP`](https://dev.mysql.com/doc/refman/8.0/en/group-by-modifiers.html) — 8.0.1.
- [`INTERSECT` / `EXCEPT`](https://dev.mysql.com/doc/refman/8.0/en/set-operations.html) — 8.0.31.
- [`FOR UPDATE ... NOWAIT` / `SKIP LOCKED`](https://dev.mysql.com/doc/refman/8.0/en/innodb-locking-reads.html) — 8.0.1.
- [row-alias `INSERT ... AS new ON DUPLICATE KEY UPDATE`](https://dev.mysql.com/doc/refman/8.0/en/insert-on-duplicate.html) — 8.0.19.
- [`JSON_VALUE()`](https://dev.mysql.com/doc/refman/8.0/en/json-search-functions.html) — 8.0.21.

(`WITH RECURSIVE` and `NOWAIT` / `SKIP LOCKED` arrived in the 8.0.1 development
milestone; their bounds round to `8.0`, whose first production release — 8.0.11
— already includes them.)

**Oracle** — `EXCEPT [ALL]`, `INTERSECT ALL`, and `MINUS ALL` are new in Oracle
Database 21c, per the
[21c New Features Guide](https://docs.oracle.com/en/database/oracle/oracle-database/21/nfcon/)
and the
[21c SQL Language Reference](https://docs.oracle.com/en/database/oracle/oracle-database/21/sqlrf/sql-language-reference.pdf)
(the set-operators section).

**PostgreSQL** — the
[version 15 release notes](https://www.postgresql.org/docs/15/release-15.html)
list both `MERGE` and the `regexp_count` / `regexp_instr` / `regexp_like` /
`regexp_substr` family as new in 15.

**SQLite** — the per-release change logs:

- [`NULLS FIRST` / `NULLS LAST`](https://sqlite.org/releaselog/3_30_0.html) — 3.30.0 (2019-10-04).
- [`RETURNING`](https://sqlite.org/releaselog/3_35_0.html) — 3.35.0 (2021-03-12).
- [`RIGHT JOIN` / `FULL OUTER JOIN`](https://sqlite.org/releaselog/3_39_0.html) — 3.39.0 (2022-06-25).
- [`string_agg()` / `concat()`](https://sqlite.org/releaselog/3_44_0.html) — 3.44.0 (2023-11-01).

**SQL Server** — Microsoft Learn's "Applies to" notes:

- [`TRIM`](https://learn.microsoft.com/en-us/sql/t-sql/functions/trim-transact-sql) — 2017; its optional trim-characters argument — 2022.
- [`DATETRUNC`](https://learn.microsoft.com/en-us/sql/t-sql/functions/datetrunc-transact-sql), [`GREATEST`](https://learn.microsoft.com/en-us/sql/t-sql/functions/logical-functions-greatest-transact-sql), [`LEAST`](https://learn.microsoft.com/en-us/sql/t-sql/functions/logical-functions-least-transact-sql), and the [`LTRIM`](https://learn.microsoft.com/en-us/sql/t-sql/functions/ltrim-transact-sql) / [`RTRIM`](https://learn.microsoft.com/en-us/sql/t-sql/functions/rtrim-transact-sql) trim-characters argument — 2022 (16.x).

---

## Context rules (SQLA0004)

A construct can be valid on a dialect in one position and rejected by the same
engine in another. The construct-level warnings above cannot express that —
the construct itself *is* supported — so these facts ship as **context
rules**: `SQLA0004` fires when the offending position is visible in the
expression where the construct is used. Two rules ship today, both MySQL
facts, live-verified against the engine.

**`LIMIT` inside an `IN` / `ANY` / `ALL` / `SOME` subquery.** MySQL rejects a
row-limited query directly under these positions ("This version of MySQL
doesn't yet support 'LIMIT & IN/ALL/ANY/SOME subquery'") — route the limited
query through a derived table or CTE instead. Scalar, `EXISTS`, CTE, and
derived-table positions accept `LIMIT` and stay silent.

```csharp
// sqlartisan_target_dbms = mysql
var q = Select(u.Id).From(u)
    .Where(u.Id.In(Select(o.UserId).From(o).OrderBy(o.UserId).Limit(2)));
// warning SQLA0004: 'Limit' is not supported inside an IN/ANY/ALL/SOME subquery on MySQL
```

**`GROUPING()` outside a `WITH ROLLUP` query.** MySQL accepts `Grouping(...)`
only when the query's `GROUP BY` carries the `WITH ROLLUP` suffix — chain
`.WithRollup()` after `.GroupBy(...)`.

```csharp
// sqlartisan_target_dbms = mysql
var q = Select(u.DepartmentId, Grouping(u.DepartmentId))
    .From(u).GroupBy(u.DepartmentId).OrderBy(u.DepartmentId);
// warning SQLA0004: 'Grouping' is not supported outside a WITH ROLLUP query on MySQL
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
(`#pragma warning disable SQLA0004`, a `[SuppressMessage]` attribute, or
`dotnet_diagnostic.SQLA0004.severity`). The `sqlartisan_construct_*`
override keys do **not** apply here — they answer "does my engine support
this construct," which is not what a context rule reports.

---

## Correlated DML target (SQLA0005)

An UPDATE or DELETE whose subquery references a column of the **unaliased**
target table is a silent tautology: the bare outer column resolves to the
inner table, so the condition compares a row to itself and the statement
updates or deletes every row. `Build()` rejects exactly this statement at
run time; `SQLA0005` is the same finding surfaced at compile time, where
the fix is cheapest.

```csharp
// sqlartisan_target_dbms = postgresql
UsersTable u = new();
OrdersTable o = new("o");
var q = DeleteFrom(u)
    .Where(Exists(Select(o.Id).From(o).Where(o.UserId == u.Id)));
// warning SQLA0005: The target of a correlated UPDATE or DELETE must be aliased
```

The fix is the one the run-time guard demands: alias the target
(`new UsersTable("u")`). On MySQL, Oracle, PostgreSQL, and SQLite the
aliased target is the correlated form; on SQL Server the DML target cannot
be aliased at all — write the joined UPDATE/DELETE form
(`.From(...)` / `.Using(...)` with joins) instead.

The diagnostic is **advisory duplication** of the `Build()` guard:
suppressing it does not stop the exception — the statement still fails to
build. It fires on every configured dialect, because the wrong-scope
resolution is universal, not a dialect fact.

The warning reports only what is provable from the source. The target must
be a local variable or a `readonly` field whose initializer visibly
constructs the table class with an empty alias and which is never
reassigned, and the correlated column reference must sit in a subquery
written inline in the same fluent chain. Anything less certain — the table
built by a helper, the alias passed as a variable, the builder split across
statements, a table class compiled into a referenced assembly — stays
silent. A missing warning therefore never means the statement is safe;
`Build()` remains the enforcement.

Suppression is per rule ID, the standard Roslyn way
(`#pragma warning disable SQLA0005`, a `[SuppressMessage]` attribute, or
`dotnet_diagnostic.SQLA0005.severity`). The `sqlartisan_construct_*`
override keys do not apply — the construct's dialect support is not what
this rule reports.

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
- **`SQLA0005` needs a configured target too.** The correlated-DML mistake
  it reports is dialect-independent, but the analyzer as a whole stays
  silent until `sqlartisan_target_dbms` is set — without a target, the
  `Build()` guard is the only report.
