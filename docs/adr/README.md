# Architecture Decision Records (ADRs)

This folder holds SqlArtisan's **architecture decision records** — the canonical,
versioned account of *why* the library is shaped the way it is. ADRs live in the
source tree (not only in issues) so they are reviewed via PR and travel with the
code.

- A GitHub **issue** is for discussion and tracking; an **ADR** is the durable
  record of the decision it reached. They cross-link.
- Format: lightweight — **Status / Context / Decision / Consequences**.
- ADRs are immutable once Accepted; to change a decision, add a new ADR that
  supersedes the old one (note it in both). See **Consolidation trigger** below
  for when a cluster should be superseded by a single ADR.

## Index

ADRs are ordered as a narrative: the philosophy first, then handling DBMS
divergence, then the output, API, performance, and enforcement-boundary
decisions — closed by the mission that ties them together.

Some ADRs form **clusters** — a base decision refined by later ADRs that must
be read together for the full picture. The cluster column marks these; reading
only part of a cluster produces incomplete (and potentially wrong) conclusions.

| ADR | Title | Cluster | Status |
|-----|-------|---------|--------|
| [0001](0001-faithful-sql-output.md) | The SQL you write is the SQL that runs; portability is a non-goal | | Accepted |
| [0002](0002-dbms-differences-in-dialect-layer.md) | Handling DBMS differences: tokens in the dialect layer, constructs as per-dialect APIs | | Accepted |
| [0003](0003-dbms-difference-safety.md) | DBMS-difference safety: permissive API + opt-in analyzer | Analyzer | Accepted |
| [0004](0004-automatic-parameterization.md) | Values are automatically parameterized | | Accepted |
| [0005](0005-public-api-in-sql-partial-class.md) | Public API location | | Accepted |
| [0006](0006-performance-allocation-light.md) | Performance is a feature: allocation-light building | | Accepted |
| [0007](0007-validity-enforcement-boundary.md) | What the library rejects: grammatical completeness vs dialect availability | Boundary | Accepted |
| [0008](0008-analyzer-override-configuration.md) | Analyzer override configuration: keys, precedence, and what's out of scope | Analyzer | Accepted |
| [0009](0009-analyzer-distribution-bundled.md) | Analyzer distribution: bundled in the core package, coupled only by contract | Analyzer | Accepted |
| [0010](0010-deterministic-guard-mission.md) | The mission: deterministic guard rails for AI-assisted SQL | | Accepted |
| [0011](0011-bounded-exception-validity-boundary.md) | Bounded exceptions to the validity-enforcement boundary: when a construct valid on some dialect may still be rejected | Boundary | Accepted |
| [0012](0012-value-domain-guards.md) | Value-domain guards: rejecting an argument value no engine accepts | Boundary | Accepted |
| [0013](0013-analyzer-context-rules.md) | Analyzer context rules: position-dependent verdicts under their own diagnostic | Analyzer | Accepted |

### Clusters

- **Boundary** (0007 + 0011 + 0012) — *What does the library reject?* 0007
  draws the line (incomplete → reject; dialect availability → permissive);
  0011 carves one enumerated exception (aliased DML target on SQL Server);
  0012 adds value-domain guards (a universally invalid embedded value also
  rejects). All three are required to answer "will the library throw for
  this?"
- **Analyzer** (0003 + 0008 + 0009 + 0013) — *How does the dialect analyzer
  work?* 0003 chooses the permissive-API + opt-in-analyzer approach; 0008
  designs the override configuration; 0009 decides bundled distribution;
  0013 adds position-dependent context rules (SQLA0004).

## Consolidation trigger

ADR refinement chains (one base decision + N follow-on ADRs) are the normal
outcome of incremental design. They become a navigation problem when
synthesizing the answer to one question requires reading too many documents.
The mitigation layers are, in order:

1. **Cluster notes above** — sufficient while the cluster is small and the
   refinements are self-contained.
2. **The decision table in `.claude/rules/guards-and-empty-states.md`** — the
   operational synthesis agents and contributors load at edit time.
3. **Supersession** — a new ADR that consolidates the cluster into one
   document, marking the originals as Superseded.

**Trigger for supersession:** when a cluster's **rejection categories or
enumerated exceptions reach five or more**, the cluster has outgrown layers 1
and 2. At that point, write a single consolidated ADR that supersedes the
cluster members (note "Superseded by ADR NNNN" in each original's Status
line). This threshold is decided now so it does not re-litigate per case.
