# Architecture Decision Records (ADRs)

This folder holds SqlArtisan's **architecture decision records** — the canonical,
versioned account of *why* the library is shaped the way it is. ADRs live in the
source tree (not only in issues) so they are reviewed via PR and travel with the
code.

- A GitHub **issue** is for discussion and tracking; an **ADR** is the durable
  record of the decision it reached. They cross-link.
- Format: lightweight — **Status / Context / Decision / Consequences**.
- ADRs are immutable once Accepted; to change a decision, add a new ADR that
  supersedes the old one (note it in both).

## Index

ADRs are ordered as a narrative: the philosophy first, then handling DBMS
divergence, then the output, API, performance, and enforcement-boundary
decisions — closed by the mission that ties them together.

| ADR | Title | Status |
|-----|-------|--------|
| [0001](0001-faithful-sql-output.md) | The SQL you write is the SQL that runs; portability is a non-goal | Accepted |
| [0002](0002-dbms-differences-in-dialect-layer.md) | Handling DBMS differences: tokens in the dialect layer, constructs as per-dialect APIs | Accepted |
| [0003](0003-dbms-difference-safety.md) | DBMS-difference safety: permissive API + opt-in analyzer | Accepted |
| [0004](0004-automatic-parameterization.md) | Values are automatically parameterized | Accepted |
| [0005](0005-public-api-in-sql-partial-class.md) | Public API location | Accepted |
| [0006](0006-performance-allocation-light.md) | Performance is a feature: allocation-light building | Accepted |
| [0007](0007-validity-enforcement-boundary.md) | What the library rejects: grammatical completeness vs dialect availability | Accepted |
| [0008](0008-analyzer-override-configuration.md) | Analyzer override configuration: keys, precedence, and what's out of scope | Accepted |
| [0009](0009-analyzer-distribution-bundled.md) | Analyzer distribution: bundled in the core package, coupled only by contract | Accepted |
| [0010](0010-deterministic-guard-mission.md) | The mission: deterministic guard rails for AI-assisted SQL | Accepted |
| [0011](0011-sqlserver-aliased-dml-target.md) | A bounded exception to ADR 0007: rejecting an aliased INSERT/UPDATE/DELETE target on SQL Server | Accepted |
