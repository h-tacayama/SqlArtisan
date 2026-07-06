# Versioning & Support

[← Back to README](https://github.com/h-tacayama/SqlArtisan/blob/main/README.md) ·
[Reference Home](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/README.md)

How version numbers, breaking changes, deprecations, and support windows work
for the published packages — `SqlArtisan`, `SqlArtisan.Dapper`, and
`SqlArtisan.TableClassGen`.

## SemVer commitment

From **1.0**, SqlArtisan follows [Semantic Versioning](https://semver.org/):
breaking changes are reserved for major releases. Until then (the 0.x
pre-releases), any release may contain breaking changes; each one is marked
**Breaking:** in the
[CHANGELOG](https://github.com/h-tacayama/SqlArtisan/blob/main/CHANGELOG.md).
If a breaking change ever slips into a minor or patch release by mistake,
that is treated as a bug — please open an
[issue](https://github.com/h-tacayama/SqlArtisan/issues).

## What counts as breaking

Two cases are specific to this library, beyond the usual API-level changes:

- **Emitted SQL is part of the contract.** A change to the SQL text emitted
  for the same input is at minimum a **minor** release — even when it is a bug
  fix — and is called out in the CHANGELOG, so you can review your query
  output before upgrading. It is **major** when it can change query
  *semantics* (which rows are read or written) for code that previously
  built and ran without error.
- **Builder-stage interfaces are not for user implementation.** The
  `I*Builder*` fluent-chain stage types (e.g. `ISelectBuilderPaginated`) exist
  solely to type the fluent chain; their only implementations are internal.
  Adding members to them is therefore a **minor** change, so new fluent-chain
  functionality can keep landing in ordinary releases — source and binary
  compatibility for *callers* is preserved as usual.

New or corrected analyzer warnings (`SQLA0001` / `SQLA0002` dialect-matrix
updates) may appear in a minor release: they change build-time diagnostics,
never runtime behavior.

## Deprecation

API slated for removal is first marked `[Obsolete]` — with a message naming
the replacement — in a **minor** release, and removed **no earlier than the
next major**. Removal without that prior `[Obsolete]` step is treated as a
bug — please open an
[issue](https://github.com/h-tacayama/SqlArtisan/issues).

## Support statement

- **Runtime**: all packages target `net8.0` and run on .NET 8 or later.
- **Verified engines**: every release passes the real-database
  [integration test matrix](https://github.com/h-tacayama/SqlArtisan/tree/main/tests/SqlArtisan.IntegrationTests)
  against one representative version each of MySQL, Oracle, PostgreSQL,
  SQLite, and SQL Server — the exact versions are listed in the analyzer's
  [verified-against table](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/analyzer.md#verified-against-versions).
  Other engine versions generally work — the emitted SQL is plain text — but
  dialect-availability claims are only made for the verified versions.
- **Support window**: fixes land in the latest release only — pre-1.0 that is
  the latest pre-release; from 1.0, the latest minor of the current major.
  For vulnerabilities, see
  [SECURITY.md](https://github.com/h-tacayama/SqlArtisan/blob/main/SECURITY.md).

## Release cadence

No fixed schedule: releases ship when a coherent set of changes is ready.
Bugs that cause silently wrong query results — the wrong rows read or
written — are treated as the highest-priority class of fix.
