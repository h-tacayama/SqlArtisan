# Versioning & Support

[← Back to README](https://github.com/h-tacayama/SqlArtisan/blob/main/README.md) ·
[Reference Home](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/README.md)

How version numbers, breaking changes, deprecations, and support windows work
for the published packages — `SqlArtisan`, `SqlArtisan.Dapper`, and
`SqlArtisan.TableClassGen`.

## SemVer commitment

From **1.0**, SqlArtisan follows [Semantic Versioning](https://semver.org/) —
breaking changes only in major releases. Until then (0.x), any release may
contain breaking changes, each marked **Breaking:** in the
[CHANGELOG](https://github.com/h-tacayama/SqlArtisan/blob/main/CHANGELOG.md).
A breaking change that slips into a minor or patch release by mistake is
treated as a bug — please open an
[issue](https://github.com/h-tacayama/SqlArtisan/issues).

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
  `SearchModifier`, and `RegexpOptions` carry explicit numeric values;
  a new value gets the next unused number, and no existing value's number
  changes. Reassigning a shipped value would silently change behavior for a
  caller who hasn't rebuilt against the new version — the same class of risk
  the emitted-SQL rule above guards against.

Analyzer dialect-matrix updates (`SQLA0001` / `SQLA0002`) may also land in a
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
  SQLite, and SQL Server (exact versions in the analyzer's
  [verified-against table](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/analyzer.md#verified-against-versions)).
  Other engine versions generally work — the emitted SQL is plain text —
  but dialect-availability claims are only made for the verified ones.
- **Support window**: fixes land in the latest release only — the latest
  pre-release before 1.0, the latest minor of the current major after.
  Vulnerabilities: see [SECURITY.md](https://github.com/h-tacayama/SqlArtisan/blob/main/SECURITY.md).

## Release cadence

No fixed schedule — releases ship when ready. Bugs causing silently wrong
query results are the highest-priority fix class.
