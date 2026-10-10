# Versioning & Support

[← Back to README](https://github.com/h-tacayama/SqlArtisan/blob/main/README.md) ·
[Reference Home](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/README.md)

How version numbers, breaking changes, deprecations, and support windows work
for the published packages — `SqlArtisan`, `SqlArtisan.ArrayBind`,
`SqlArtisan.Dapper`, and `SqlArtisan.TableClassGen`.

## SemVer commitment

From **1.0**, SqlArtisan follows [Semantic Versioning](https://semver.org/) —
breaking changes only in major releases. Until then (0.x), any release may
contain breaking changes, each marked **Breaking:** in the
[CHANGELOG](https://github.com/h-tacayama/SqlArtisan/blob/main/CHANGELOG.md).
A breaking change that slips into a minor or patch release by mistake is
treated as a bug — please open an
[issue](https://github.com/h-tacayama/SqlArtisan/issues).

## What the public API is

The packages expose these namespaces, and they carry different promises.

- **`SqlArtisan`** — the API. Every type here is yours to name in a
  declaration, and everything on this page applies to it in full. One
  exception: deriving from `BindValue` is not covered. It is unsealed only so
  that `BindArrayValue` can derive from it; construct it, don't subclass it.
- **`SqlArtisan.Internal`** — the values the API hands back. A `Sql.*` call
  returns a type from here because the chain is typed: `Sql.Sum(...)` has to
  return something that offers `.Over(...)` where `Sql.Abs(...)` does not.
  You receive these values and call the members the reference documents.

  **Covered**: each type's name, and the members the reference documents — so
  code that receives one of these values keeps compiling, and an assembly
  compiled against one release keeps binding.

  **Not covered**: deriving from one, and any member the reference does not
  document. For the classes here, deriving is not merely uncovered but
  impossible: none has a constructor your assembly can reach — you get each one
  from the `Sql.*` call, operator, or chain step that produces it. The
  interfaces are the half no constructor can close. A stage composing
  `ISubquery` is closed anyway, since that interface declares an internal
  member; the rest are implementable, which is what "builder-stage interfaces
  are not for user implementation" below is about.
- **`SqlArtisan.Dapper`** and **`SqlArtisan.ArrayBind`** — the integration
  packages' own API, covered exactly like `SqlArtisan`.
- **`SqlArtisan.TableClassGen`** ships as a command-line tool and exposes no
  public API at all; what it commits to is its
  [command-line surface](#tableclassgens-command-line-surface).

The split is gated, not merely stated. A public type in `SqlArtisan.Internal`
that no public signature hands back fails the test suite, as does one that
offers a constructor another assembly can reach — so the namespace cannot
quietly accumulate surface the commitment above was never meant to cover.

## What counts as breaking

Three cases are specific to this library, beyond the usual API-level changes:

- **Emitted SQL is part of the contract.** A change to the SQL text emitted
  for the same input is at minimum a **minor** release, even as a bug fix,
  and is called out in the CHANGELOG. It's **major** when it can change
  query semantics — which rows are read or written.
- **Builder-stage interfaces are not for user implementation.** The
  `I*Builder*` fluent-chain stage types (e.g. `ISelectBuilderPaginated`)
  and the cross-cutting capability interfaces they compose
  (`IPagination`, `IForUpdate`, `IJoinOperator`, `ISetOperator`,
  `IReturning`, `IUpsert`) exist only to type the fluent chain; all
  implementations are internal. Adding a member to any of them is a
  **minor** change; caller compatibility is preserved as usual.
- **Public enum values are append-only.** `Dbms`, `DateTimePart`,
  `SearchModifier`, `RegexpOptions`, and `DbTypeCategory` carry explicit
  numeric values;
  a new value gets the next unused number, and no existing value's number
  changes. Reassigning a shipped value would silently change behavior for a
  caller who hasn't rebuilt against the new version — the same class of risk
  the emitted-SQL rule above guards against. A member is replaced by
  appending its successor at the next unused value and marking the old one
  `[Obsolete]`, never by an alias that repeats its number.

The analyzer's diagnostics follow their own rules, below: a new warning may
land in a minor release, since it changes what a build reports, never what a
query does.

## The analyzer's IDs and configuration

The analyzer exposes no types, but you write its names into your build — a
`#pragma`, `[SuppressMessage]`, `<NoWarn>`, `<WarningsAsErrors>`, a
`dotnet_diagnostic.SQLA0xxx.severity` or category-severity line, the
`sqlartisan_*` keys and MSBuild properties — and an ID, a category or a
property name that stops matching is silent there. So, after 1.0:

- **Covered** — a change is breaking:
  - each diagnostic ID and the rule it names. An ID is never renumbered or
    reused, and is removed only by a major that retires its rule or merges its
    reports into another ID; a retired ID stays retired;
  - the four categories (`SqlArtisan.Configuration`, `SqlArtisan.Dialect`,
    `SqlArtisan.Schema`, `SqlArtisan.Validity`) and their ID bands;
  - each rule's default severity and whether it is on by default: raising
    either is breaking, lowering either is minor;
  - which reports share an ID: an ID is split or merged only in a major, since
    either moves reports out from under a suppression or escalation you wrote;
  - the key names `sqlartisan_syntax_<dbms>`, `sqlartisan_construct_<member>`
    and `sqlartisan_construct_<member>_arity<N>`, the rule that derives a
    construct key from a member's name and declared parameter count, and the
    `<SqlArtisanSyntax<Dbms>>` MSBuild properties;
  - the values and their meaning: `any`, `none`, an engine version (the release
    year on SQL Server; the engine's own version number, major with an optional
    minor and patch, on the others), `supported` and
    `unsupported`, matched without regard to case;
  - which setting wins: `.editorconfig` over the MSBuild property, an arity key
    over a member key, your override over the shipped matrix;
  - silence until you configure a dialect: no rule from `SQLA0100` on reports
    before then;
  - the removed `sqlartisan_target_dbms` / `sqlartisan_target_version` keys and
    their properties, which report `SQLA0001` for as long as 1.x ships.
- **Not covered**: a diagnostic's title, its message text, and its help-link
  anchor.
- **Minor: any other change that adds reports** — a new rule, at the next ID in
  its category's band; a new reason under an existing ID; a matrix correction
  or a new verdict that starts reporting something. Each can fail a build that
  treats warnings as errors, so the CHANGELOG marks it **New warning:**. A new
  key, value, or DBMS token (for a new `Dbms` member) is minor too, and so is
  lowering a default severity or turning a rule off by default.
- **Patch: any other change that only removes reports**, such as a
  false-positive fix or a matrix correction that stops reporting.

## TableClassGen's command-line surface

What the tool commits to is what a script, or a later release of the tool, reads
back:

- **Covered** — a change is breaking:
  - option names, their values and defaults, the `--config` keys, and the
    `SQLARTISAN_DB_PASSWORD` environment variable;
  - the exit codes: `0` success or in sync, `1` drift, `2` error;
  - the `--format json` keys and values
    ([JSON output](https://github.com/h-tacayama/SqlArtisan/blob/main/src/SqlArtisan.TableClassGen/README.md#json-output));
  - in a generated file, for a given catalog: the class and property names
    ([Generated names](https://github.com/h-tacayama/SqlArtisan/blob/main/src/SqlArtisan.TableClassGen/README.md#generated-names)), the constructor
    `(string tableAlias = "")`, the base type, and the table and column literals.
- **Not covered**: the text report, the wording of errors on stderr, the `--help`
  text, and the interactive prompts.
- **Minor: an addition** — a new option or `--config` key, a new value for an
  existing option (a `--dbms` value for a new `Dbms` member, say), a new
  `--format json` key, or a new `mode` or `status` value. A script reading the
  JSON ignores a key it does not know and does not fail on a value it does not
  know.
- **Minor, called out in the CHANGELOG**: any other change to a generated file's
  text — a new or changed `DbColumnMetadata` fact, the layout, the header — so an
  analyzer rule that needs a new schema fact can ship in a minor release, as a
  new analyzer rule may. Until `--fix` regenerates such a file, `--check` reports
  it `modified`, so a scheduled `--check` fails from the upgrade until `--fix`
  runs.

A generated file needs a `SqlArtisan` at least as new as the tool that wrote it:
the tool writes its own release's attribute arguments and `DbTypeCategory`
members, and an older package rejects one it lacks. Install the tool in a local
tool manifest and upgrade it together with the package
([Installation](https://github.com/h-tacayama/SqlArtisan/blob/main/src/SqlArtisan.TableClassGen/README.md#installation)).

## Deprecation

API slated for removal is first marked `[Obsolete]` — with a message naming
the replacement — in a **minor** release, and removed **no earlier than the
next major**. Removal without that prior `[Obsolete]` step is treated as a
bug — please open an
[issue](https://github.com/h-tacayama/SqlArtisan/issues).

The analyzer's configuration follows the same path. A key slated for removal
keeps working beside its replacement in a **minor** release, and setting it
reports a warning of its own, so you can silence that warning without
silencing configuration errors. It is removed **no earlier than the next
major**, and from then a key still set reports `SQLA0001` rather than quietly
configuring nothing. A diagnostic ID is retired only in a major, and never
reused.

## Support statement

- **Runtime**: all packages target `net8.0` and run on .NET 8 or later.
- **Verified engines**: every release passes the
  [integration test matrix](https://github.com/h-tacayama/SqlArtisan/tree/main/tests/SqlArtisan.IntegrationTests)
  against one representative version each of MySQL, Oracle, PostgreSQL,
  SQLite, and SQL Server — Oracle, PostgreSQL and SQL Server at a second,
  newer one too, where their version-bound entries are proven (exact versions
  in the analyzer's
  [verified-against table](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/analyzer.md#verified-against-versions)).
  Other engine versions generally work — the emitted SQL is plain text —
  but dialect-availability claims are only made for the verified ones.
- **Support window**: fixes land in the latest release only — the latest
  pre-release before 1.0, the latest minor of the current major after.
  Vulnerabilities: see [SECURITY.md](https://github.com/h-tacayama/SqlArtisan/blob/main/SECURITY.md).

## Release cadence

No fixed schedule — releases ship when ready. Bugs causing silently wrong
query results are the highest-priority fix class.
