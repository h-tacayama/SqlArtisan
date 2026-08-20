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
  declaration, and everything on this page applies to it in full.
- **`SqlArtisan.Internal`** — the values the API hands back. A `Sql.*` call
  returns a type from here because the chain is typed: `Sql.Sum(...)` has to
  return something that offers `.Over(...)` where `Sql.Abs(...)` does not.
  You receive these values and call the members the reference documents.

  **Covered**: each type's name, and the members the reference documents — so
  code that receives one of these values keeps compiling, and an assembly
  compiled against one release keeps binding.

  **Not covered**: deriving from one, and any member the reference does not
  document. Deriving is not merely uncovered but impossible: no type here has a
  constructor your assembly can reach — you get each one from the `Sql.*` call,
  operator, or chain step that produces it.
- **`SqlArtisan.Dapper`** and **`SqlArtisan.ArrayBind`** — the integration
  packages' own API, covered exactly like `SqlArtisan`.
- **`SqlArtisan.TableClassGen`** ships as a command-line tool and exposes no
  public API at all; what it commits to is its command-line surface.

The split is gated, not merely stated. A public type in `SqlArtisan.Internal`
that no public signature hands back fails the test suite, as does one that
offers a public constructor — so the namespace cannot quietly accumulate
surface the commitment above was never meant to cover.

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
  the emitted-SQL rule above guards against.

Analyzer diagnostic updates (the SQLA rules) may also land in a
minor release: they change build-time diagnostics, never runtime behavior.

## Deprecation

API slated for removal is first marked `[Obsolete]` — with a message naming
the replacement — in a **minor** release, and removed **no earlier than the
next major**. Removal without that prior `[Obsolete]` step is treated as a
bug — please open an
[issue](https://github.com/h-tacayama/SqlArtisan/issues).

## Support statement

- **Runtime**: all packages target `net8.0` and run on .NET 8 or later.
- **Verified engines**: every release passes the
  [integration test matrix](https://github.com/h-tacayama/SqlArtisan/tree/main/tests/SqlArtisan.IntegrationTests)
  against one representative version each of MySQL, Oracle, PostgreSQL,
  SQLite, and SQL Server — Oracle at two, since its version-bound entries are
  proven at 23ai (exact versions in the analyzer's
  [verified-against table](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/analyzer.md#verified-against-versions)).
  Other engine versions generally work — the emitted SQL is plain text —
  but dialect-availability claims are only made for the verified ones.
- **Support window**: fixes land in the latest release only — the latest
  pre-release before 1.0, the latest minor of the current major after.
  Vulnerabilities: see [SECURITY.md](https://github.com/h-tacayama/SqlArtisan/blob/main/SECURITY.md).

## Release cadence

No fixed schedule — releases ship when ready. Bugs causing silently wrong
query results are the highest-priority fix class.
