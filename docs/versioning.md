# Versioning & Support

[← Back to README](https://github.com/h-tacayama/SqlArtisan/blob/main/README.md) ·
[Reference Home](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/README.md)

How version numbers, breaking changes, deprecations, and support windows work
for the published packages — `SqlArtisan`, `SqlArtisan.Dapper`, and
`SqlArtisan.TableClassGen`.

## SemVer commitment

From **1.0**, SqlArtisan follows [Semantic Versioning](https://semver.org/):
no breaking changes in minor or patch releases. Until then (the 0.x
pre-releases), any release may contain breaking changes; each one is marked
**Breaking:** in the
[CHANGELOG](https://github.com/h-tacayama/SqlArtisan/blob/main/CHANGELOG.md).

## What counts as breaking

The usual SemVer rules apply — removing or renaming public API, changing
signatures, source or binary compatibility, or a documented behavior contract
is **major**. Two cases are specific to this library:

- **Emitted SQL is part of the contract.** A change to the SQL text emitted
  for the same input is at minimum a **minor** release — even when it is a bug
  fix — and is called out under a dedicated **Emitted SQL** heading in the
  CHANGELOG, so you can review your query output before upgrading. It is
  **major** when it can change query *semantics* (which rows are read or
  written) for code that previously built and ran without error.
- **Builder-stage interfaces are not for user implementation.** The
  `I*Builder*` fluent-chain stage types (e.g. `ISelectBuilderPaginated`) exist
  solely to type the fluent chain; their only implementations are internal.
  Adding members to them is therefore a **minor** change — source and binary
  compatibility for *callers* is preserved as usual, and each planned fluent
  extension would otherwise formally force a major.

New or corrected analyzer warnings (`SQLA0001` / `SQLA0002` dialect-matrix
updates) may appear in a minor release: they change build-time diagnostics,
never runtime behavior.

## Deprecation

API slated for removal is first marked `[Obsolete]` — with a message naming
the replacement — in a **minor** release, and removed **no earlier than the
next major**.

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
Confirmed wrong-SQL emissions — the class of bug that silently reads or
writes the wrong rows — are treated as high priority, ahead of other
pending work.
