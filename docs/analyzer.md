# Roslyn Analyzer

[← Back to Reference Home](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/README.md)

An opt-in build-time analyzer (ADR 0003) warns when your code uses a SqlArtisan
construct that is not supported on your project's target dialect. It ships
inside the `SqlArtisan` package — no extra package reference — and is
completely silent until you configure a target.

## Contents

- [Enabling it](#enabling-it)
- [Rules](#rules)
- [Correcting a warning: the override keys](#correcting-a-warning-the-override-keys)
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
| `SQLA0001` | Warning | A SqlArtisan construct is used against the configured target dialect, and the dialect matrix has a **verified** entry saying that dialect doesn't support it. |
| `SQLA0002` | Warning | A `sqlartisan_target_dbms` or `sqlartisan_construct_*` value could not be recognized. |

`SQLA0001` only ever fires for a construct the matrix has confirmed — one
without a matrix entry stays silent rather than guessed at, so an incomplete
matrix can under-warn but never produce a false positive.

```csharp
using static SqlArtisan.Sql;

// sqlartisan_target_dbms = mysql
var g = Rollup(t.Code, t.Name);
// warning SQLA0001: 'Rollup' is not supported on MySQL (per SqlArtisan's
// dialect matrix, verified against MySQL 8.0 ...). If your engine version
// supports it, silence this by setting 'sqlartisan_construct_rollup =
// supported' in .editorconfig.
```

Severity is controlled the standard Roslyn way, per rule ID:

```ini
dotnet_diagnostic.SQLA0001.severity = error   # promote to a build error
dotnet_diagnostic.SQLA0002.severity = none    # suppress entirely
```

Because severity is per rule ID, it cannot be scoped to one construct —
promoting `SQLA0001` to `error` makes *every* dialect mismatch a build
failure, not just a chosen one. If you need to forbid a specific construct
as a team policy rather than a dialect fact, reach for
[`BannedApiAnalyzers`](https://www.nuget.org/packages/Microsoft.CodeAnalysis.BannedApiAnalyzers)
instead — `sqlartisan_construct_*` expresses "my engine actually
supports/doesn't support this," not "we've decided not to use this."

---

## Correcting a warning: the override keys

Every `SQLA0001` message names the override key that would silence it. Two
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
dotnet_diagnostic.SQLA0001.severity = error
```

Every confirmed mismatch fails the build; escape hatches (`supported`
overrides) still apply first, so only genuinely unconfirmed constructs fail.

**Whitelist mode** — fail on anything the matrix hasn't explicitly verified
one way or the other is not offered as a separate rule. `SQLA0001` only
fires on a *confirmed* mismatch by design (ADR 0003's degradable matrix), so
there is no "unverified construct" diagnostic to promote — the matrix's
completeness is the whole safety net. Track the analyzer's own package
version if you want to be notified as matrix coverage grows.

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
  key with an unrecognized *value*.
- **The matrix does not yet cover every public member.** Absence of an
  entry means "not yet verified," not "universally supported" — see
  [`DialectMatrix.cs`](https://github.com/h-tacayama/SqlArtisan/blob/main/src/SqlArtisan.Analyzers/DialectMatrix.cs)
  for what's currently entered.
