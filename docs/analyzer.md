# Roslyn Analyzer

[← Back to Reference Home](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/README.md)

The analyzer is the second layer of SqlArtisan's deterministic guard-rail
stack — after compile-time type safety and before exact-SQL tests. It warns
at build time when your code uses a construct that is not supported on your
project's target dialect — or, for a project that ships against more than
one engine, any dialect in the set you configure. It ships inside the
`SqlArtisan` package — no extra package reference — and is completely silent
until you configure a target.

## Contents

- [Enabling it](#enabling-it)
- [Rules](#rules)
- [Checking a set of dialects at once](#checking-a-set-of-dialects-at-once)
- [Migrating from the legacy target key](#migrating-from-the-legacy-target-key)
- [Correcting a warning: the override keys](#correcting-a-warning-the-override-keys)
- [Version-aware warnings (SQLA0101)](#version-aware-warnings-sqla0101)
- [Context rules (SQLA0102)](#context-rules-sqla0102)
- [Datepart validity (SQLA0104)](#datepart-validity-sqla0104)
- [Correlated DML target (SQLA0300)](#correlated-dml-target-sqla0300)
- [Schema-aware warnings (SQLA0200)](#schema-aware-warnings-sqla0200)
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
sqlartisan_syntax_postgresql = 16   # engine version, or `any` for no version bound
```

The key names the dialect (`sqlartisan_syntax_mysql` / `_oracle` /
`_postgresql` / `_sqlite` / `_sqlserver`); its value is the engine version to
check against, or `any` to check the dialect with no version floor. Or, if
you prefer an MSBuild property (e.g. in `Directory.Build.props`):

```xml
<PropertyGroup>
  <SqlArtisanSyntaxPostgreSql>16</SqlArtisanSyntaxPostgreSql>
</PropertyGroup>
```

`.editorconfig` wins when both are set, per dialect. With no target
configured either way, the analyzer never reports anything — enabling it is
purely additive. To check more than one dialect at once — an ISV shipping
against several engines, a migration in progress, "standard SQL only" — set
more than one key; see
[Checking a set of dialects at once](#checking-a-set-of-dialects-at-once).

An older `sqlartisan_target_dbms` / `sqlartisan_target_version` pair still
works exactly as before, but is deprecated (`SQLA0002`) — see
[Migrating from the legacy target key](#migrating-from-the-legacy-target-key)
below.

---

## Rules

### Bands and categories

Every ID sits in a numbered band, and the band is the category — so a
bulk-severity setting reaches one family without the others, and a rule added
later keeps its family's numbering instead of taking the next free number:

| Band | Category | Answers |
|---|---|---|
| `SQLA0001`–`SQLA0099` | `SqlArtisan.Configuration` | is the analyzer itself configured correctly? |
| `SQLA0100`–`SQLA0199` | `SqlArtisan.Dialect` | will this run on the engine you configured? |
| `SQLA0200`–`SQLA0299` | `SqlArtisan.Schema` | does it agree with what your table classes say the columns are? |
| `SQLA0300`–`SQLA0399` | `SqlArtisan.Validity` | is this a statement `Build()` would reject? |

```ini
# every schema rule as an error, the other families untouched
dotnet_analyzer_diagnostic.category-SqlArtisan.Schema.severity = error
```

A bulk setting reaches only rules that are enabled by default, so `SQLA0203`
still needs naming by ID.

### The rules

| ID | Severity | Reports |
|---|---|---|
| `SQLA0001` | Warning | A SqlArtisan analyzer configuration problem: an unrecognized `sqlartisan_syntax_*` key name or value, a `sqlartisan_target_dbms` / `sqlartisan_target_version` / `sqlartisan_construct_*` value that could not be recognized, a `sqlartisan_syntax_*` family that resolves to no dialect at all, or the legacy pair coexisting with a family that doesn't name its DBMS — see [Checking a set of dialects at once](#checking-a-set-of-dialects-at-once). |
| `SQLA0002` | Warning | `sqlartisan_target_dbms` / `sqlartisan_target_version` are deprecated in favor of `sqlartisan_syntax_*` — see [Migrating from the legacy target key](#migrating-from-the-legacy-target-key). |
| `SQLA0100` | Warning | A SqlArtisan construct is used against a configured dialect, and the dialect matrix has a **verified** entry saying that dialect doesn't support it. Checking more than one dialect joins every failing one into a single diagnostic. |
| `SQLA0101` | Warning | A construct is supported on a configured dialect, but not at its declared version — see [Version-aware warnings](#version-aware-warnings-sqla0101). Checking more than one dialect reports one diagnostic per failing dialect. |
| `SQLA0102` | Warning | A construct a configured dialect supports, used in a syntactic position that dialect rejects it in — see [Context rules](#context-rules-sqla0102). |
| `SQLA0103` | Warning | A compile-time identifier literal — a table or expression alias, a CTE or derived-table name, a `VALUES` column name, or the Oracle `RETURNING` output variable — is longer than a configured dialect allows. Checking more than one dialect reports one diagnostic per dialect it's too long for. |
| `SQLA0104` | Warning | A literal `DateTimePart` argument to `Extract`/`Datepart`/`Dateadd`/`Datediff`/`DateTrunc`/`Datetrunc`/`Interval`/`Timestampadd`/`Timestampdiff` is not a value the configured dialect's grammar accepts for that function — see [Datepart validity](#datepart-validity-sqla0104). Checking more than one dialect joins every failing one into a single diagnostic. |
| `SQLA0200` | Warning | `IS NULL` / `IS NOT NULL` on a column the generated table class declares `NOT NULL`, so the predicate's answer is fixed before the query runs. Reported only in a statement that visibly builds its own query and has no outer join — past one, the anti-join makes exactly this predicate meaningful; see [Schema-aware warnings](#schema-aware-warnings-sqla0200). |
| `SQLA0201` | Warning | `NOT IN` over a subquery whose selected column is nullable — one NULL makes the whole predicate NULL, so the query matches nothing. See [Schema-aware warnings](#schema-aware-warnings-sqla0200). |
| `SQLA0202` | Warning | An `INSERT` column list omits a column that is `NOT NULL` with no default, so the engine cannot construct the row. See [Schema-aware warnings](#schema-aware-warnings-sqla0200). |
| `SQLA0203` | Info, **off by default** | `Count(column)` on a column the generated table class declares nullable, which counts values rather than rows. Advice on correct code, so it reports nothing until you turn it on — see [Schema-aware warnings](#schema-aware-warnings-sqla0200). |
| `SQLA0204` | Warning | A `WHERE` or `ON` predicate wraps an indexed column in a function, or matches it with a leading-wildcard pattern, so no index on it can be used. See [Schema-aware warnings](#schema-aware-warnings-sqla0200). |
| `SQLA0205` | Warning | A column is compared to a value of another type category — a text column against a number, say. The engine reconciles the two for you, and on MySQL that changes which rows match. See [Schema-aware warnings](#schema-aware-warnings-sqla0200). |
| `SQLA0300` | Warning | A correlated UPDATE or DELETE has an unaliased target — the statement `Build()` rejects at run time, surfaced early; see [Correlated DML target](#correlated-dml-target-sqla0300). |

`SQLA0001` and `SQLA0002` are both compilation-end diagnostics with no file
location: they appear in **build** output (CLI and CI, and an IDE's Error
List after an explicit build — check that the list's source filter includes
Build entries), but not in the editor's live analysis, which never runs
compilation-end actions. `SQLA0100`, by contrast, is a per-usage diagnostic
and shows up live as you type.

`SQLA0100` only ever fires for a construct the matrix has confirmed — one
without a matrix entry stays silent rather than guessed at, so an incomplete
matrix can under-warn but never produce a false positive.

`SQLA0103` checks compile-time identifier literals — table and expression
aliases (`.As(...)`), CTE and derived-table names, `VALUES` column names, and
the Oracle `RETURNING ... INTO` output variable — against each dialect's limit:

| Dialect | Limit | Measured in |
|---|---|---|
| MySQL | 256 | characters |
| Oracle | 128 | UTF-8 bytes |
| PostgreSQL | 63 | UTF-8 bytes |
| SQLite | unbounded (never warns) | — |
| SQL Server | 128 | characters |

MySQL's 256 is its **alias** limit — the checked positions are aliases, which
MySQL allows well past its 64-character table and column names. PostgreSQL is
the sharpest edge: it does not error on an over-long identifier but silently
truncates it (only a notice), so two long names can collide after truncation,
and the analyzer is the only place this surfaces before the database does.
Only constant identifiers are checked; a name built at run time is left alone.
Like `SQLA0100`, it is a per-usage diagnostic suppressible at one site
(`#pragma warning disable SQLA0103`, a `[SuppressMessage]` attribute, or
`dotnet_diagnostic.SQLA0103.severity`).

```csharp
using static SqlArtisan.Sql;

// sqlartisan_syntax_mysql = any
var g = Rollup(t.Code, t.Name);
// warning SQLA0100: 'Rollup' is not supported on MySQL. Set
// 'sqlartisan_construct_rollup = supported' in .editorconfig if your
// engine version supports it.
```

Severity is controlled the standard Roslyn way, per rule ID:

```ini
dotnet_diagnostic.SQLA0100.severity = error   # promote to a build error
dotnet_diagnostic.SQLA0204.severity = none    # suppress entirely
```

The exceptions are `SQLA0001` and `SQLA0002`: neither carries a file
location, so a file-scoped `.editorconfig` severity line never reaches
either. Suppress from a global analyzer config (a `.globalconfig` file with
`is_global = true`) or with `<NoWarn>SQLA0001</NoWarn>` /
`<NoWarn>SQLA0002</NoWarn>` in the project file.

Because severity is per rule ID, it cannot be scoped to one construct —
promoting `SQLA0100` to `error` makes *every* dialect mismatch a build
failure, not just a chosen one. If you need to forbid a specific construct
as a team policy rather than a dialect fact, reach for
[`BannedApiAnalyzers`](https://www.nuget.org/packages/Microsoft.CodeAnalysis.BannedApiAnalyzers)
instead — `sqlartisan_construct_*` expresses "my engine actually
supports/doesn't support this," not "we've decided not to use this."

---

## Checking a set of dialects at once

Most projects write for one engine, and `sqlartisan_syntax_<dbms> = <version>`
is all they need. A second population exists: an ISV shipping one codebase
against whichever database the customer picks, an Oracle → PostgreSQL
migration keeping the old dialect checked while the new one comes online, or
a procurement requirement to run on several named engines. Set more than one
`sqlartisan_syntax_*` key to check all of them from the same build:

```ini
root = true

[*.cs]
sqlartisan_syntax_postgresql = 16
sqlartisan_syntax_oracle     = 19
sqlartisan_syntax_sqlite     = any
```

Every rule now evaluates against the whole set. No dialect in the set is
privileged — a construct failing on your migration target carries the same
weight as one failing on today's dialect. A construct can fail two different
ways across the set at once (unsupported on one dialect, merely version-bound
on another), and both are reported — dropping either would hide a real,
differently-actionable fact.

**"Standard SQL only"** is the degenerate case: name all five DBMS.

### `any` and `none`

A key's value is either an engine version (same format as
[Version-aware warnings](#version-aware-warnings-sqla0101) — `8.0.16`, `23`,
`16`, `3.44`, `2022`) or the literal `any`, meaning "check this dialect, no
version floor." `none` explicitly excludes a dialect in a narrower
`.editorconfig` section even when a broader scope (or the MSBuild property)
named it — the only way to carve an exception out of a family that has no
single "primary" key to override:

```ini
# repo-wide: this product ships against all five
[*.cs]
sqlartisan_syntax_mysql      = any
sqlartisan_syntax_oracle     = any
sqlartisan_syntax_postgresql = any
sqlartisan_syntax_sqlite     = any
sqlartisan_syntax_sqlserver  = any

# except this area, which never runs on the embedded engine
[src/Reporting/**.cs]
sqlartisan_syntax_sqlite = none
```

Omitting a key from the narrower section would not do this: `.editorconfig`
sections layer, so the broader `sqlartisan_syntax_sqlite = any` still applies
to those files. Only `none` overrides it back off.

No aliases beyond these two — no `true`/`yes`/`off`. A smaller value domain
is easier to document and validate. A key set to nothing at all
(`sqlartisan_syntax_oracle =`) reads as unset, not as `none`.

### SQLA0100 joins; SQLA0101 and SQLA0103 report one per dialect

`SQLA0100`'s message has one slot for the dialect name, so every failing
dialect in the set joins into a single diagnostic:

```csharp
// sqlartisan_syntax_mysql = any
// sqlartisan_syntax_oracle = any
// sqlartisan_syntax_sqlite = any
var g = Rollup(t.Code, t.Name);
// warning SQLA0100: 'Rollup' is not supported on MySQL and SQLite. Set
// 'sqlartisan_construct_rollup = supported' in .editorconfig if your
// engine version supports it.
```

`SQLA0101` and `SQLA0103` each carry a *per-dialect* value in their message
(a required version, a length limit and its unit) that can't join without
losing information, so they report once per failing dialect instead — a
construct version-bound on two configured dialects reports twice, one
diagnostic for each.

### `sqlartisan_construct_*` overrides are resolved once, not per dialect

A `sqlartisan_construct_*` override is your own claim about your
configuration, which under a set reads as "fine on every DBMS I've named" —
dialect-independent, so it is resolved once per usage, before the per-DBMS
matrix loop runs. Forcing a construct unsupported reports exactly one
`SQLA0100`, naming every dialect in the set, never one diagnostic per DBMS.

### An empty set is reported, not silent

`sqlartisan_syntax_oracle = none` with no other `sqlartisan_syntax_*` key
resolves to an empty set for that scope — every rule's "is a dialect
configured" gate then reads as "unconfigured," and the analyzer goes quiet
there. `SQLA0001` reports this once per compilation the first time any file's
family is present but every key resolves to `none`, so a project doesn't lose
analyzer coverage from one typo-adjacent `none` without a visible reason.

### A typo in the key name is now detectable

Unlike a `sqlartisan_construct_*` key (see
[Correcting a warning](#correcting-a-warning-the-override-keys) below),
`sqlartisan_syntax_*` puts the DBMS name in the key itself, which the
analyzer can enumerate and check — `sqlartisan_syntax_postgres` (missing the
`ql`) reports `SQLA0001` naming the valid suffixes, rather than silently
checking nothing.

### Staged adoption

Naming a new DBMS can light up many warnings at once. The same levers a
single-dialect project already has apply here too: promote or demote one
rule ID at a time (`dotnet_diagnostic.SQLA0100.severity = suggestion` while
triaging), scope the new key to one path first
(`[src/NewEngineArea/**.cs]`), and reach for `sqlartisan_construct_*`
overrides on constructs your specific engine version actually supports
ahead of the shipped matrix's baseline.

---

## Migrating from the legacy target key

`sqlartisan_target_dbms` / `sqlartisan_target_version` still work — they
desugar to a single-DBMS `sqlartisan_syntax_*` set — but are deprecated in
favor of the family:

```diff
-sqlartisan_target_dbms = postgresql
-sqlartisan_target_version = 16
+sqlartisan_syntax_postgresql = 16
```

Using either legacy key with no `sqlartisan_syntax_*` key present reports
`SQLA0002` once per compilation, including when the pair resolves perfectly
correctly — the warning is what makes the pair's eventual removal in a
future major version expected rather than sudden. (Once a family key is
present, the family governs and `SQLA0002` yields to the rules below.) If your project has `TreatWarningsAsErrors` and cannot migrate
immediately, suppress `SQLA0002` specifically — not `SQLA0001`, and not the
whole `SqlArtisan.Configuration` category — so silencing the nag never
silences real config-error detection. Like `SQLA0001`, it carries no file
location, so a file-scoped `.editorconfig` severity line never reaches it —
use a global analyzer config instead (a `.globalconfig` file with
`is_global = true`):

```ini
is_global = true
dotnet_diagnostic.SQLA0002.severity = none
```

or `<NoWarn>SQLA0002</NoWarn>` in the project file.

**The family governs outright — it is never merged with the legacy pair.**
If any `sqlartisan_syntax_*` key is present anywhere in a file's effective
options, the legacy pair is not consulted at all for that file, even to fill
in a DBMS the family didn't name:

```ini
sqlartisan_target_dbms = postgresql
sqlartisan_syntax_oracle = any
```

Here only Oracle is checked — PostgreSQL is silently dropped, which is
exactly the coverage loss `SQLA0001` exists to flag: adding
`sqlartisan_syntax_oracle` to a scope that already carries
`sqlartisan_target_dbms = postgresql` reads as *adding* Oracle, when it in
fact *replaces* PostgreSQL. The message names the dropped DBMS and the full
`key = value` line that would keep it checked, carrying the declared version
over — this is a distinct `SQLA0001` report, not `SQLA0002`; the two never
fire for the same configuration.

The report fires only when the family doesn't itself name the legacy pair's
DBMS. Mid-migration — `sqlartisan_syntax_postgresql = 16` written, the old
`sqlartisan_target_dbms = postgresql` line not yet deleted — nothing is
dropped: the family already covers PostgreSQL, so no configuration
diagnostic fires and the leftover legacy line is simply inert. Delete it at
leisure.

---

## Correcting a warning: the override keys

Every `SQLA0100` message names the override key that would silence it. Two
kinds exist:

- **Member-level** — `sqlartisan_construct_<name>` — applies to *every*
  overload of that member, including ones added in a future SqlArtisan
  version. Use this when your intent is "this function," not one specific
  shape of it.
- **Arity-level** — `sqlartisan_construct_<name>_arity<N>` — applies only to
  the overload with exactly `N` declared parameters. Use this when the
  matrix (or your own knowledge) says only one shape of an overloaded member
  differs — e.g. `StringAgg`'s 2-argument form runs on PostgreSQL, SQLite
  (3.44+), and SQL Server, but its 3-argument inline-`ORDER BY` form has no
  SQL Server spelling (there it's `WITHIN GROUP (ORDER BY ...)` instead):

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
sqlartisan_syntax_postgresql = 15
sqlartisan_construct_merge_into = supported   # e.g. targeting PostgreSQL 15+, where MERGE landed
```

A typo in a `sqlartisan_construct_*` key name is not detectable — its member
name lives entirely in an arbitrary key suffix Roslyn has no reference list
for, so a misspelled override silently does nothing. If a warning doesn't
clear after adding one, check the key against the message text exactly. (A
typo in a `sqlartisan_syntax_*` key name *is* detectable, since the DBMS
suffix is one of only five — see
[Checking a set of dialects at once](#checking-a-set-of-dialects-at-once).)

---

## Version-aware warnings (SQLA0101)

Some constructs are only newer than *some* engine versions on an otherwise
supported dialect — `MERGE` before PostgreSQL 15, `DATETRUNC` before SQL
Server 2022. Declare your engine's version as `sqlartisan_syntax_<dbms>`'s
value and the matrix's version bounds warn on those too, as `SQLA0101` — the
same "this could break in production" fact `SQLA0100` reports for a dialect
mismatch, just for a version shortfall instead:

```ini
root = true

[*.cs]
sqlartisan_syntax_sqlserver = 2019
```

```csharp
using static SqlArtisan.Sql;

var g = Datetrunc(DateTimePart.Day, "created_at");
// warning SQLA0101: 'Datetrunc' requires SQL Server 2022+ but the declared
// target version is 2019. Set 'sqlartisan_construct_datetrunc = supported'
// in .editorconfig if your engine supports it.
```

Or, if you prefer an MSBuild property:

```xml
<PropertyGroup>
  <SqlArtisanSyntaxSqlServer>2019</SqlArtisanSyntaxSqlServer>
</PropertyGroup>
```

- **Value format.** The engine's own version spelling, the same one this
  documentation's dialect notes use — `8.0.16` for MySQL, `23` for Oracle,
  `16` for PostgreSQL, `3.44` for SQLite, `2022` for SQL Server. Versions
  compare by numeric segment (`8.0.20` is newer than `8.0.16`, and a bare
  `8.0` reads as `8.0.0` — declare the precise patch version if an 8.0.x
  bound matters to you); trailing letters in a segment are ignored (`23ai`
  reads as `23`). `any` checks the dialect with no version floor — version
  bounds never fire for it, and the analyzer falls back to the matrix's plain
  supported/not-supported verdict.
- **Your overrides keep the last word.** A version bound refines the
  shipped matrix's verdict, not yours: resolution stays *your arity-level
  override → your member-level override → the matrix (version-refined) →
  silence*, so `supported` / `unsupported` keys silence or force the
  warning exactly as they do today — `sqlartisan_construct_datetrunc =
  supported` silences the example above even with `2019` still declared.
- **No new false positives.** A version bound only ever refines a construct
  the matrix already has an entry for; a construct without an entry stays
  silent whether or not a version is declared.
- **Same plumbing as every `sqlartisan_syntax_*` key.** Resolved per source
  file and per DBMS, `.editorconfig` wins over the MSBuild property, and an
  unrecognized value is flagged as `SQLA0001` and otherwise treated as
  unset for that DBMS.

Suppression is per rule ID, the standard Roslyn way
(`#pragma warning disable SQLA0101`, a `[SuppressMessage]` attribute, or
`dotnet_diagnostic.SQLA0101.severity`).

### Version-bound constructs

Every construct below has a recorded minimum version on the named dialect.
Declaring a version below the bound reports the construct as version-bound
(`SQLA0101`); declaring no version keeps the matrix's plain
`supported`/`not supported` verdict, and a version at or above the bound
resolves the construct as supported. For most rows that reproduces the plain
verdict exactly; where the bound sits above the dialect's verified baseline
(the Oracle 23 row below), the plain verdict is `not supported`, and declaring
the version is what lifts it.

| Construct | Dialect | Minimum version | Why |
|---|---|---|---|
| `WithRecursive` | MySQL | 8.0 | `WITH RECURSIVE` needs MySQL's CTE support, added in 8.0. |
| `Grouping` (1-argument form) | MySQL | 8.0.1 | `GROUPING(expr)` landed in 8.0.1. |
| `Except`, `Intersect`, `ExceptAll`, `IntersectAll` | MySQL | 8.0.31 | `EXCEPT`/`INTERSECT` landed in 8.0.31. |
| `Nowait`, `SkipLocked` | MySQL | 8.0 | `FOR UPDATE NOWAIT`/`SKIP LOCKED` need 8.0. |
| `OnDuplicateKeyUpdate`, `Excluded` | MySQL | 8.0.19 | SqlArtisan always emits the row-alias UPSERT form (`... AS new ON DUPLICATE KEY UPDATE col = new.col`), which needs the row alias MySQL added in 8.0.19 — the pre-8.0.19 `VALUES()` function form is never emitted. |
| `JsonValue` | MySQL | 8.0.21 | `JSON_VALUE` landed in 8.0.21. |
| `Except`, `ExceptAll`, `IntersectAll`, `MinusAll` | Oracle | 21 | `EXCEPT`, `EXCEPT ALL`, `INTERSECT ALL`, and `MINUS ALL` landed in Oracle 21c (plain `INTERSECT`/`MINUS` predate it) — live-verified forward-compatible on Oracle 23ai too. |
| `L2Distance`, `CosineDistance`, `NegativeInnerProduct` | Oracle | 23 | The `<->`, `<=>`, and `<#>` vector distance shorthands landed with Oracle 23ai's AI Vector Search (the other three pgvector operators have no Oracle spelling at any version). |
| `MergeInto`, `WhenMatched`, `WhenNotMatched`, `ThenInsert`, `ThenUpdateSet`, `ThenDelete`, the 3-argument `Values` (MERGE `USING` literal rows) | PostgreSQL | 15 | `MERGE` landed in PostgreSQL 15. `Using` itself carries no bound — the key is shared with `DeleteBuilder`'s plain `DELETE ... USING`, which predates and does not require PostgreSQL 15; the `MergeInto` bound still flags a MERGE statement below 15. |
| `RegexpLike`, `RegexpCount`, `RegexpReplace`, `RegexpSubstr`, `RegexpInstr` | PostgreSQL | 15 | `regexp_like`, `regexp_count`, `regexp_substr`, and `regexp_instr` landed in PostgreSQL 15. `regexp_replace` predates it — 15 is where it gained the position and occurrence arguments — but the bound covers every `RegexpReplace` overload, so a 3-argument call is reported below 15 too. MySQL's `REGEXP_SUBSTR`/`REGEXP_INSTR` top out at 5/6 arguments respectively (neither has `subexpr`), so `RegexpSubstr`'s 6-argument and `RegexpInstr`'s 7-argument overloads are MySQL-unsupported at any version, independent of this PostgreSQL bound. |
| `Log10` | PostgreSQL | 12 | `log10()` landed in PostgreSQL 12; before it, base-10 was spelled `log(x)`. |
| `RightJoin`, `FullJoin`, `NaturalRightJoin`, `NaturalFullJoin` | SQLite | 3.39 | `RIGHT JOIN`/`FULL JOIN` landed in SQLite 3.39. |
| `Returning` | SQLite | 3.35 | `RETURNING` landed in SQLite 3.35. |
| `Ceil`, `Ceiling`, `Exp`, `Floor`, `Ln`, `Log` (both forms), `Log10`, `Mod`, `Power`, `Sign`, `Sqrt` | SQLite | 3.35 | The `SQLITE_ENABLE_MATH_FUNCTIONS` extension landed in 3.35 (enabled in the project's pinned `bundle_e_sqlite3`); none of these functions exist below it. |
| `Substring` | SQLite | 3.34 | SQLite registered `SUBSTRING` as a second name for `substr()` in 3.34. |
| `StringAgg` (both overloads), `Concat` (both overloads), `ConcatWs` | SQLite | 3.44 | `string_agg`/`concat`/`concat_ws` landed in SQLite 3.44. |
| `NullsFirst`, `NullsLast` | SQLite | 3.30 | `NULLS FIRST`/`NULLS LAST` landed in SQLite 3.30. |
| `Iif` | SQLite | 3.32 | `IIF(...)` landed in SQLite 3.32. |
| `If` | SQLite | 3.48 | SQLite registered `IF` as a second name for `IIF` in 3.48; earlier versions have `IIF` only. MySQL's own `IF` predates its 8.0 baseline, so that dialect carries no bound. |
| `Iif` | SQL Server | 2012 | `IIF(...)` has been available since SQL Server 2012. |
| `Trim` (1-argument form), `ConcatWs` | SQL Server | 2017 | `TRIM(...)` and `CONCAT_WS(...)` both landed in SQL Server 2017. |
| `Datetrunc`, `Greatest`, `Least`, the 2-argument `Ltrim`/`Rtrim`/`Trim` forms | SQL Server | 2022 | `DATETRUNC`, `GREATEST`/`LEAST`, and the trim-characters overloads all landed in SQL Server 2022. |

<details>
<summary>Sources for these version bounds — the vendor documentation behind each version</summary>

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
(the set-operators section). The `<->` / `<=>` / `<#>` vector distance
shorthands are documented in the
[Oracle AI Vector Search User's Guide](https://docs.oracle.com/en/database/oracle/oracle-database/23/vecse/vector-distance-functions-and-operators.html)
as new in Oracle Database 23ai.

**PostgreSQL** — the
[version 15 release notes](https://www.postgresql.org/docs/15/release-15.html)
list both `MERGE` and the `regexp_count` / `regexp_instr` / `regexp_like` /
`regexp_substr` family as new in 15. The
[version 12 release notes](https://www.postgresql.org/docs/12/release-12.html)
add `log10()` as a named alias for the base-10 `log()`.

**SQLite** — the per-release change logs:

- [`NULLS FIRST` / `NULLS LAST`](https://sqlite.org/releaselog/3_30_0.html) — 3.30.0 (2019-10-04).
- [`RETURNING`](https://sqlite.org/releaselog/3_35_0.html) — 3.35.0 (2021-03-12).
- [The math-functions extension](https://sqlite.org/releaselog/3_35_0.html) (`ceil`, `ceiling`, `exp`, `floor`, `ln`, `log`, `log10`, `mod`, `power`, `sign`, `sqrt`) — 3.35.0, same release as `RETURNING`.
- [`RIGHT JOIN` / `FULL OUTER JOIN`](https://sqlite.org/releaselog/3_39_0.html) — 3.39.0 (2022-06-25).
- `substring()` as a second name for `substr()` — 3.34.0 (2020-12-01), confirmed against the `aBuiltinFunc[]` table in `src/func.c` (absent at tag `version-3.33.0`, present at `version-3.34.0`).
- [`string_agg()` / `concat()` / `concat_ws()`](https://sqlite.org/releaselog/3_44_0.html) — 3.44.0 (2023-11-01).
- [`iif()`](https://sqlite.org/releaselog/3_32_0.html) — 3.32.0 (2020-05-22); `if()` was added as a second name for it in [3.48.0](https://sqlite.org/releaselog/3_48_0.html) (2025-01-14), confirmed against the `aBuiltinFunc[]` table in `src/func.c` (absent at tag `version-3.47.0`, present at `version-3.48.0`).

**SQL Server** — Microsoft Learn's "Applies to" notes:

- [`TRIM`](https://learn.microsoft.com/en-us/sql/t-sql/functions/trim-transact-sql) — 2017; its optional trim-characters argument — 2022.
- [`CONCAT_WS`](https://learn.microsoft.com/en-us/sql/t-sql/functions/concat-ws-transact-sql) — 2017.
- [`DATETRUNC`](https://learn.microsoft.com/en-us/sql/t-sql/functions/datetrunc-transact-sql), [`GREATEST`](https://learn.microsoft.com/en-us/sql/t-sql/functions/logical-functions-greatest-transact-sql), [`LEAST`](https://learn.microsoft.com/en-us/sql/t-sql/functions/logical-functions-least-transact-sql), and the [`LTRIM`](https://learn.microsoft.com/en-us/sql/t-sql/functions/ltrim-transact-sql) / [`RTRIM`](https://learn.microsoft.com/en-us/sql/t-sql/functions/rtrim-transact-sql) trim-characters argument — 2022 (16.x).
- [`IIF`](https://learn.microsoft.com/en-us/sql/t-sql/functions/logical-functions-iif-transact-sql) — 2012 (11.x).

</details>

---

## Context rules (SQLA0102)

A construct can be valid on a dialect in one position and rejected by the same
engine in another. The construct-level warnings above cannot express that —
the construct itself *is* supported — so these facts ship as **context
rules**: `SQLA0102` fires when the offending position is visible in the
expression where the construct is used. Five rules ship today — three MySQL
facts and two SQL Server facts — each live-verified against the engine.

**`LIMIT` inside an `IN` / `NOT IN` / `ANY` / `ALL` / `SOME` subquery.** MySQL
rejects a row-limited query directly under these positions ("This version of
MySQL doesn't yet support 'LIMIT & IN/ALL/ANY/SOME subquery'" — the same
restriction covers `NOT IN`) — route the limited query through a derived table
or CTE instead. Scalar, `EXISTS`, CTE, and derived-table positions accept
`LIMIT` and stay silent.

```csharp
// sqlartisan_syntax_mysql = any
var q = Select(u.Id).From(u)
    .Where(u.Id.In(Select(o.UserId).From(o).OrderBy(o.UserId).Limit(2)));
// warning SQLA0102: 'Limit' is not supported inside an IN/ANY/ALL/SOME subquery on MySQL
```

**`GROUPING()` outside a `WITH ROLLUP` query.** MySQL accepts `Grouping(...)`
only when the query's `GROUP BY` carries the `WITH ROLLUP` suffix — chain
`.WithRollup()` after `.GroupBy(...)`.

```csharp
// sqlartisan_syntax_mysql = any
var q = Select(u.DepartmentId, Grouping(u.DepartmentId))
    .From(u).GroupBy(u.DepartmentId).OrderBy(u.DepartmentId);
// warning SQLA0102: 'Grouping' is not supported outside a WITH ROLLUP query on MySQL
```

**`INTERVAL` outside a `+`/`-` date-arithmetic expression.** MySQL's
`INTERVAL` keyword has no standalone value — it parses only as an immediate
operand of `+`/`-`, or as the second argument of `DateAdd(...)`/`DateSub(...)`,
whether the spelling came from `Interval(...)` or the 2-argument
`IntervalLiteral(...)` MySQL's grammar also happens to accept.

```csharp
// sqlartisan_syntax_mysql = any
var q = Select(Interval(30, DateTimePart.Day)).From(u);
// warning SQLA0102: 'Interval' is not supported outside a +/- date-arithmetic expression or a DATE_ADD/DATE_SUB call on MySQL
```

**`PERCENTILE_CONT` / `PERCENTILE_DISC` outside an `OVER` clause.** SQL Server
exposes both percentiles only as window functions, so the plain
`WithinGroup(...)` form Oracle and PostgreSQL accept has no spelling there —
chain `.Over()` (optionally with `PartitionBy(...)`) after `.WithinGroup(...)`.

```csharp
// sqlartisan_syntax_sqlserver = any
var q = Select(PercentileCont(0.5).WithinGroup(OrderBy(u.Age))).From(u);
// warning SQLA0102: 'PercentileCont' is not supported outside an OVER clause on SQL Server
```

**`INSERTED` / `DELETED` outside an `OUTPUT` clause.** The pseudo-tables are
bound by the `OUTPUT` clause itself, so `Inserted(...)` / `Deleted(...)`
resolve against no table anywhere else — read the row images inside
`Output(...)`, and filter on the target table's own columns instead.

```csharp
// sqlartisan_syntax_sqlserver = any
var q = Select(u.Id).From(u).Where(Inserted(u.Id) == 1);
// warning SQLA0102: 'Inserted' is not supported outside an OUTPUT clause on SQL Server
```

A context rule warns only when the position is provable from the expression
itself. A subquery held in a variable, a builder chain continued from a
helper method, or any shape the analyzer doesn't recognize stays silent —
the same under-warn-but-never-false-positive principle the matrix follows.
The absence side is equally strict: `Grouping` warns only when the chain
shows a call *after* `.GroupBy(...)` that isn't `.WithRollup()` — from that
point the builder's type can never accept the suffix — and a chain that
still ends at `.GroupBy(...)` stays silent. The percentile rule reads the
same way: it warns only where the expression is passed straight into the
clause that consumes it, since a percentile parked in a variable can still
acquire `.Over()` on a later line.

Suppression is per rule ID, the standard Roslyn way
(`#pragma warning disable SQLA0102`, a `[SuppressMessage]` attribute, or
`dotnet_diagnostic.SQLA0102.severity`). The `sqlartisan_construct_*`
override keys do **not** apply here — they answer "does my engine support
this construct," which is not what a context rule reports.

---

## Datepart validity (SQLA0104)

`DateTimePart` is a 42-member superset shared across `Extract`, `Datepart`,
`Dateadd`, `Datediff`, `DateTrunc`, `Datetrunc`, `Interval`, `Timestampadd`,
and `Timestampdiff` — its own XML doc says explicitly that not every field is
valid for every function or dialect. `SQLA0100` cannot express that: the
construct itself *is* supported, so a call like
`Extract(DateTimePart.Epoch, x)` targeting Oracle passes the construct-level
check and fails only when the database runs it (`EPOCH` is a PostgreSQL-only
`EXTRACT` field). `SQLA0104` closes that gap at the argument level, for the
eleven (function, dialect) pairings below:

| Function | Checked dialect(s) |
|---|---|
| `Extract` | MySQL, Oracle, PostgreSQL — each against its own field list |
| `Datepart`, `Dateadd`, `Datediff` | SQL Server — all three share one datepart list |
| `DateTrunc` | PostgreSQL |
| `Datetrunc` | SQL Server |
| `Interval` | MySQL — the same unit list as MySQL's `Extract` |
| `Timestampadd`, `Timestampdiff` | MySQL — their own list of nine simple units, narrower than `Extract`'s (no compound units like `DAY_HOUR`) |

```csharp
// sqlartisan_syntax_oracle = any
var q = Select(Extract(DateTimePart.Epoch, u.CreatedAt)).From(u);
// warning SQLA0104: 'Epoch' is not a valid datepart for 'Extract' on Oracle
```

```csharp
// sqlartisan_syntax_sqlserver = any
var q = Select(Datetrunc(DateTimePart.Weekday, u.CreatedAt)).From(u);
// warning SQLA0104: 'Weekday' is not a valid datepart for 'Datetrunc' on SQL Server
```

Each list is built from the vendor's own reference and spot-verified against
a live engine. Three cases stay silent, never a false positive:

- **The argument is not a compile-time constant** — a variable holding a
  computed `DateTimePart` cannot be resolved, the same
  provable-from-the-expression-or-silent contract [Context
  rules](#context-rules-sqla0102) follows.
- **This rule has no list for the (function, dialect) pair** — e.g.
  `Extract` targeting SQL Server, which `SQLA0100` already rejects outright.
- **The matrix already flags the construct unsupported on that dialect at
  its declared version** — `SQLA0100`/`SQLA0101` own that verdict; `SQLA0104`
  would otherwise double-report the same usage.

Suppression is per rule ID (`#pragma warning disable SQLA0104`, a
`[SuppressMessage]` attribute, or `dotnet_diagnostic.SQLA0104.severity`); the
`sqlartisan_construct_*` override keys do not apply — overriding "this
function runs on my engine" is not a claim that every `DateTimePart` value
does.

---

## Correlated DML target (SQLA0300)

An UPDATE or DELETE whose subquery references a column of the **unaliased**
target table is a silent tautology: the bare outer column resolves to the
inner table, so the condition compares a row to itself and the statement
updates or deletes every row. `Build()` rejects exactly this statement at
run time; `SQLA0300` is the same finding surfaced at compile time, where
the fix is cheapest.

```csharp
// sqlartisan_syntax_postgresql = any
UsersTable u = new();
OrdersTable o = new("o");
var q = DeleteFrom(u)
    .Where(Exists(Select(o.Id).From(o).Where(o.UserId == u.Id)));
// warning SQLA0300: The target of a correlated UPDATE or DELETE must be aliased
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
statements, a chain head selected by a conditional expression, a table class
compiled into a referenced assembly — stays silent. A missing warning therefore never means the statement is safe;
`Build()` remains the enforcement.

Suppression is per rule ID, the standard Roslyn way
(`#pragma warning disable SQLA0300`, a `[SuppressMessage]` attribute, or
`dotnet_diagnostic.SQLA0300.severity`). The `sqlartisan_construct_*`
override keys do not apply — the construct's dialect support is not what
this rule reports.

---

## Schema-aware warnings (SQLA0200)

`SqlArtisan.TableClassGen` records what the catalog says about each column on
the generated table class:

```csharp
[DbColumnMetadata(Nullable = false, HasDefault = false, TypeCategory = DbTypeCategory.Text)]
public DbColumn Code { get; }
```

Where a fact is recorded, the analyzer can settle questions the query text
alone cannot. The first is a predicate whose answer never depends on the data:

```csharp
// sqlartisan_syntax_postgresql = any
var sql = Select(t.Code).From(t).Where(t.Code.IsNull).Build();
// warning SQLA0200: 'Code' is NOT NULL, so 'IsNull' is always false
```

`IS NOT NULL` on the same column reports the mirror image — always `true`.
Neither is a dialect fact: the column's own declaration decides it on every
engine.

Past an **outer join** a NOT NULL column is legitimately NULL on the
null-supplied side, and `.Where(r.Id.IsNull)` after a `LeftJoin` is the
idiomatic anti-join — so the rule reports only where it can see there is no
such join. That takes two conditions, both required:

- The statement contains **no outer join** — no `LeftJoin`, `RightJoin`,
  `FullJoin`, `NaturalLeftJoin`, `NaturalRightJoin`, `NaturalFullJoin`,
  `LeftJoinLateral`, or `OuterApply`. Which side a join null-supplies is not
  worked out; any one of them silences the statement. (`InnerJoin` and
  `NaturalJoin` null-supply nothing and are not on the list.)
- The statement **builds its own query** — the chain starts at `Select` /
  `Update` / `DeleteFrom` / `MergeInto` / `With` / `WithRecursive` right there.
  A chain held in a variable, returned by a helper method, kept in a field,
  or whose head is selected by a conditional expression
  (`(flag ? Select(...) : Select(...)).Where(...)`) is left alone: the join
  that would decide the answer is somewhere this rule cannot read.

The trade is deliberate — the rule misses real constant predicates in order not
to call a working anti-join a mistake.

The second is SQL's oldest trap. `NOT IN` over a subquery is not "none of
these" when the subquery can yield a NULL — the comparison becomes NULL for
every row, and the query matches nothing at all:

```csharp
var sql =
    Select(t.Id)
    .From(t)
    .Where(t.Id.NotIn(Select(s.Ref).From(s)))   // s.Ref is nullable
    .Build();
// warning SQLA0201: 'Ref' is nullable, so this NOT IN matches no rows at all
// when the subquery yields a NULL
```

Reach for `NOT EXISTS` instead, or filter the NULLs out of the subquery —
adding `.Where(s.Ref.IsNotNull)` also silences the warning. `IN` is
unaffected — there a NULL merely fails to match — so only `NOT IN` is
reported.

The third is a row the engine rejects: an `INSERT` whose column list leaves out
a column that is `NOT NULL` and has no default. What the catalog cannot show is
a `BEFORE INSERT` trigger that fills the column in — where one exists, the
statement is valid and the warning is a false alarm to suppress.

```csharp
var sql = InsertInto(t, t.Note).Values("x").Build();
// warning SQLA0202: 'Code' is NOT NULL with no default and is missing from
// this INSERT's column list
```

A column the engine assigns itself — identity, auto-increment, generated, or
one with a `DEFAULT` — is recorded as defaulted and never reported; omitting it
is the normal thing to do. Only the explicit-column-list form is checked: the
positional `InsertInto(t).Values(...)` supplies every column by construction,
and `InsertIgnoreInto` asked for error-raising rows to be skipped.

> **MySQL caveat.** Outside strict mode MySQL does not reject this statement —
> it substitutes an implicit default (`0`, `''`) and warns. `STRICT_TRANS_TABLES`
> is on by default from MySQL 5.7, so the warning matches what the default
> configuration does; on a non-strict server, read it as flagging a column that
> will silently receive an implicit default rather than the value you meant.

The fourth is not a mistake at all, which is why it ships switched off.
`COUNT(column)` counts the rows where that column is not NULL — correct SQL, and
sometimes exactly what you meant, but a surprise when you wanted the row count.
Name it explicitly to turn it on; a category-wide severity does not reach a
rule that is disabled by default:

```ini
[*.cs]
dotnet_diagnostic.SQLA0203.severity = suggestion
```

```csharp
var sql = Select(Count(t.Note)).From(t).Build();   // t.Note is nullable
// info SQLA0203: 'Note' is nullable, so this COUNT skips its NULL rows.
// Use Count(Asterisk) to count rows.
```

At `suggestion` this appears in the IDE and in a SARIF log, but **not** in
`dotnet build` output at any verbosity — a plain build prints nothing below
warning level. Use `dotnet_diagnostic.SQLA0203.severity = warning` to see it in
a build or in CI.

Only the plain `Count(expr)` form is considered: `Count(Asterisk)` counts rows
already, and `Count(Distinct, expr)` asks for distinct values, which `COUNT(*)`
cannot give. Like `SQLA0200` it stays out of any statement with an outer join,
where counting the column is precisely how you count the matched rows and
`COUNT(*)` would count the unmatched ones too — the same reason a `NOT NULL`
column is never reported in a plain query, where it and `COUNT(*)` agree.

The fifth is about the shape of a filter, not its cost. An index on a column can
only be used when the filtered side is the bare column: wrap it in a function, or
anchor the pattern with a leading `%`, and the engine has to look at every row.

```csharp
var sql = Select(t.Id).From(t).Where(Upper(t.Name) == "SMITH").Build();
// warning SQLA0204: 'Name' leads an index, but this filter has it wrapped in
// Upper, so no index on it can be used
```

Whether the planner *would* have chosen the index is a cost question, and cost
questions stay out — statistics and data volume are the optimizer's domain. What
this reports is only the form: the predicate as written gives the index nothing
to range over. The remediation is the same in every case — leave the column bare
on the filtered side and move the work to the other side, or index the expression
itself, which the generator then records as unknown and the rule stops reporting.

Only `WHERE` and `ON` are checked. The same call in a select list or an
`ORDER BY` costs no index, and `HAVING` filters groups after any index has done
its work. A condition built apart from its clause is left alone: nothing at that
point shows it will ever reach a `WHERE`. A call that *is* the predicate — full-text
`Contains` / `Freetext`, the JSONB containment and existence predicates
(`JsonbContains`, `JsonbExists*`), the array predicates (`ArrayOverlaps`,
`ArrayContains`, `ArrayContainedBy`) — is never a wrapping: those are often
exactly the spelling that uses the index on that column. The JSON *element
access* operators (`->`, `->>`, `#>`, `#>>`) are different — they return a
value, not a condition, so wrapping a column in one still reports.

`Indexed` records only whether the column **leads** an index. A composite index
on `(a, b)` is fully usable from a predicate on `a` alone, so `a` is recorded and
`b` is not — a predicate on `b` alone could not have used that index anyway, and
"the query constrains `b` but not `a`" is a cost judgment (Oracle's index skip
scan and MySQL's skip-scan optimization both exist) rather than a fact. A
**partial** (filtered) index claims nothing either way: whether its predicate
covers your query is an expression the generator refuses to interpret, so a
column that leads only a partial index stays unknown. On Oracle, one
function-based index makes **every** column of that table record nothing — its
expression text is stored in a form the tool does not read, so the whole table
degrades to unknown rather than guess.

To learn what the planner actually did, ask the engine: feed `sql.Text` and its
bind parameters to your engine's plan report, and read it against data of
production-like volume. The spelling differs — MySQL `EXPLAIN`, Oracle
`EXPLAIN PLAN` (which writes to `PLAN_TABLE` for you to read back), PostgreSQL
`EXPLAIN`, SQLite `EXPLAIN QUERY PLAN` (plain `EXPLAIN` there returns bytecode),
SQL Server the `SET SHOWPLAN_XML ON` session option. That volume condition is
the whole of it: the choice turns on table sizes and statistics your test
database does not have, so a plan read there describes that dataset rather than
what production will do with the same query.

The sixth can change which rows come back, not just how fast they come back. A
column compared to a value of another type category leaves the engine to
reconcile the two, and MySQL reconciles by turning both sides into
floating-point numbers:

```csharp
var sql = Select(t.Id).From(t).Where(t.ZipCode == Bind(1500001)).Build();
// warning SQLA0205: 'ZipCode' is text, but this compares it to numeric.
// Cast one side to say which you mean.
```

On MySQL that predicate also matches `'01500001'` and `'1500001 Nowhere St'`,
because each converts to the same number — the query answers a question you did
not ask. On PostgreSQL the same comparison is rejected outright (`operator does
not exist: character varying = integer`), which is at least loud. The index on
the column is unusable either way, but the wrong-rows half is why this is a
warning rather than a note about speed.

The category is coarse on purpose — `Text`, `Numeric`, `Temporal`, `Binary`,
`Boolean` — carrying no length, precision, or scale. A `numeric(10,2)` column
compared to an `int` is one category against itself and reports nothing, and so
is a `varchar` column compared to a `char`. Carrying width would mean judging
values rather than types.

A truth value and a number count as one category. T-SQL offers no boolean
literal, so `WHERE is_active = 1` on a `bit` column is the only spelling it has,
and MySQL's `BOOLEAN` is `TINYINT(1)`, so the mirror of that is idiomatic there.
Comparing a boolean column to text still reports.

Only comparisons are checked. `SET` spells its assignment with `==` as well, but
assignment coercion is fixed per engine and cannot change which rows match — and
there would be no second side to cast. A condition built apart from its clause —
held in a variable, returned by a helper — is left alone rather than guessed at,
since nothing at that point shows which of the two it will become.

An explicit `Cast(...)` on either side silences it: you have said which type you
mean, so there is nothing left for the engine to decide. A type name the
generator does not recognize records no category, and a comparison between two
bound values names no column, so neither reports.

**All six are silent unless the fact was recorded.** An attribute the generator
never wrote, a fact it could not determine (an absent named argument), a
hand-written table class, or a column reached through
`new DbTable("t").Column("x")` — which has no declaration to carry metadata —
all produce nothing. Regenerate your table classes to opt in; nothing else
changes. `SQLA0201` additionally reads only a select list it can see: a
subquery held in a variable, one whose chain does not begin at `Select(...)`
(a `WITH`-headed query), or one selecting anything other than a single column,
is left alone — as is one whose own filter it cannot read, since a condition
held in a variable may be the `.Where(s.Ref.IsNotNull)` that already fixes it. `SQLA0202` skips a statement whose column list it cannot read in
full — a column array built elsewhere — since a column it failed to read would
otherwise look omitted.

Like every rule here, it stays silent until a dialect is configured, even
though the verdict itself is dialect-independent. Suppression is per rule
ID, the standard Roslyn way; the `sqlartisan_construct_*` override keys do not
apply, since the construct's dialect support is not what this rule reports.

---

## Mixed-dialect projects

`.editorconfig` sections scope by file path, so a project that emits SQL for
more than one engine — each **file** written for one dialect — can give each
area its own target:

```ini
root = true

[src/Reporting/Postgres/**.cs]
sqlartisan_syntax_postgresql = any

[src/Reporting/MySql/**.cs]
sqlartisan_syntax_mysql = any
```

The analyzer resolves the target per source file, so this is the supported
pattern for a codebase split by file — there is no per-call target inference
(e.g. reading the literal argument to `.Build(Dbms.MySql)`); a file's target
comes only from its `.editorconfig` scope or the MSBuild property.

This is a different axis from
[checking a set of dialects at once](#checking-a-set-of-dialects-at-once): a
path-scoped section picks *one* dialect per file, while
`sqlartisan_syntax_*` can name *several* dialects for the same file. The two
compose freely — a path-scoped section can itself set more than one
`sqlartisan_syntax_*` key.

---

## CI gates and stricter enforcement

**Promote a dialect mismatch to a hard failure:**

```ini
dotnet_diagnostic.SQLA0100.severity = error
```

Every confirmed mismatch fails the build; escape hatches (`supported`
overrides) still apply first, so only genuinely unconfirmed constructs fail.

**Whitelist mode** — failing on any construct the matrix hasn't verified — is
deliberately not offered. `SQLA0100` fires only on a *confirmed* mismatch (a
construct the matrix doesn't know stays silent rather than guess), so there is
no "unverified construct" diagnostic to promote: the matrix's completeness is
the safety net, and this repository enforces it. A coverage test fails when any
public method, property, field, or overloaded operator ships without a matrix
entry or a documented dialect-neutral exclusion, and an integration-test sweep
executes the entries against a live engine per dialect (the versions in the
table below), asserting that accept/reject outcomes match the matrix both ways.
Two entries are excluded by name, and a dozen more skip individual engines
where the shared runner or the container image cannot execute the statement —
SQL Server's image ships without Full-Text Search, for example.

---

## Verified-against versions

The matrix's `verified` entries were checked against one representative
version per dialect (the same engines the integration test matrix runs
against):

| Dialect | Verified against |
|---|---|
| MySQL | MySQL 8.0 |
| Oracle | Oracle Database XE 21c (`gvenzl/oracle-xe:21.3.0-slim-faststart`), plus Oracle Database Free 23ai (`gvenzl/oracle-free:23-slim-faststart`) for the version-bound entries `SQLA0101` reports |
| PostgreSQL | PostgreSQL 16 |
| SQLite | `SQLitePCLRaw.bundle_e_sqlite3` 3.0.3 (via `Microsoft.Data.Sqlite` 9.0.5) |
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
  `Trunc(expr[, format])` is the example: a numeric argument is Oracle,
  PostgreSQL and SQLite 3.35+, a date/time argument is Oracle-only, and both shapes
  compile to the exact same C# overload. It has no matrix entry and never
  warns either way. The `IntervalLiteral(...)` field markers (`Year(...)`,
  `Month(...)`, ..., `ToSecond(...)`) share this gap for the same reason:
  Oracle's leading/fractional-digit precision is an optional argument, not a
  separate overload, so the matrix can't see whether a call used it.
- **`SQLA0104`'s lists don't model a source-type or column-type constraint.**
  Oracle's `EXTRACT` rejects `HOUR`/`MINUTE`/`SECOND` on a plain `DATE`
  source (it needs a `TIMESTAMP`) and the four `TIMEZONE_*` fields need
  `TIMESTAMP WITH TIME ZONE` specifically — `SQLA0104` accepts all ten Oracle
  fields regardless of what `Extract`'s own source expression evaluates to.
  SQL Server's `DATETRUNC` similarly rejects `MICROSECOND` on a `datetime2`
  column while accepting it elsewhere; `SQLA0104` treats `Microsecond` as
  valid there unconditionally. Both are staying-permissive gaps, not
  false-positive risks: an argument this rule accepts can still fail at
  execution for a source-type reason it doesn't check.
- **`sqlartisan_construct_*` key names fail silently on a typo** (see above)
  — there is no diagnostic for an unrecognized `sqlartisan_construct_*` *key
  name*, only for a recognized key with an unrecognized *value*. Value
  validation covers the keys derived from the matrix; an override key naming
  a member the matrix has no entry for is honored when its value is valid,
  but a typo in its *value* is silently ignored too. `sqlartisan_syntax_*`
  key names do not share this gap — a typo there is `SQLA0001`.
- **Absence of an entry still means silence, not endorsement.** The matrix
  covers every referencable public method, property, field, and overloaded
  operator except a
  documented set of dialect-neutral plumbing (`Build`, the result and
  configuration objects) and `Trunc` above — gate-enforced by the
  repository's tests — but a member in that excluded set never warns either
  way. See
  [`DialectMatrix.cs`](https://github.com/h-tacayama/SqlArtisan/blob/main/src/SqlArtisan.Analyzers/DialectMatrix.cs)
  for what's entered.
- **The dialect-independent rules need a configured target too.**
  `SQLA0300` and the schema-aware `SQLA0200`–`SQLA0205` report facts that
  hold on every engine, but the analyzer as a whole stays silent until a
  dialect is configured — without one, `SQLA0300`'s `Build()` guard is the
  only report and the schema rules have none.
