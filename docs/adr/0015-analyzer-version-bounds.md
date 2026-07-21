# ADR 0015 — Analyzer version bounds: interval annotations on the dialect matrix, evaluated against a declared engine version

**Status:** Accepted

## Context

Every `DialectMatrix` entry is a single bool per dialect, verified against
one representative engine version (`VerifiedAgainstVersion`: MySQL 8.0,
Oracle XE 21c, PostgreSQL 16, the bundled SQLite build, SQL Server 2022).
That collapses a real axis of variation: `MERGE` did not exist before
PostgreSQL 15, `DATETRUNC` before SQL Server 2022, `EXCEPT` before
Oracle 21c. Today the only escape hatch is a per-construct
`sqlartisan_construct_*` override (ADR 0008) the *user* sets after
discovering the mismatch themselves — the matrix carries no version
knowledge of its own, so every user on an older or newer engine repeats the
same discovery. #262 reserved `sqlartisan_target_version` for exactly this
gap, pinning the value format, resolution order, and degradability
contract now so the eventual implementation could not paint the design into
a corner. This ADR is that implementation (#263).

A full version × dialect × construct matrix was the first shape considered
and rejected immediately: verification cost scales with every additional
pinned engine image, `VerifiedAgainstVersion`'s one-baseline-per-dialect
design exists specifically to bound that cost, and most constructs have no
known version boundary at all — a dense matrix would mostly encode "unknown"
as a guess.

## Decision

**A sparse, optional minimum-version floor per (construct, dialect) cell,
evaluated against a declared `sqlartisan_target_version`, reported as a new
diagnostic `SQLA0006`.**

- **Interval annotations, browserslist-style.** `DialectMatrix` gains a
  second table, `Bounds : Dictionary<MatrixKey, VersionBounds>`, keyed
  identically to the existing `Entries` bool table but populated only for
  constructs with a *recorded, primary-sourced* version boundary — most
  already existed as prose in an `Entries` comment (MySQL 8.0.31 for
  `EXCEPT`, PostgreSQL 15 for `MERGE`, and so on); this ADR is what makes
  that prose machine-actionable. A missing `Bounds` row means no boundary is
  known, not that none exists — the plain bool answers it, exactly as
  before this ADR (ADR 0003's degradable design: absence is silence, never
  a guess).
- **A parallel table, not a wider `DbmsSupport`.** Extending `DbmsSupport`
  itself with five more optional per-dialect fields would force every one
  of the ~200 existing `Entries` initializers to pay constructor syntax for
  a fact only a handful of rows have. A dictionary keyed the same way as
  `Entries` costs nothing on the unbounded rows and lets `Entries` stay
  byte-for-byte untouched.
- **The bound wins over the bool, in both directions, once a version is
  declared.** With `sqlartisan_target_version` set and a matching `Bounds`
  row: `supported ⇔ declared ≥ min`. A currently-`true` cell below its bound
  flips to unsupported (SQL Server 2019 asking about `Datetrunc`, `true` in
  `Entries`); a currently-`false` cell above its bound would flip to
  supported — no seeded bound sits on a false cell today (see the
  `WithRecursive` record below), but the semantics are symmetric by design.
  With no declared version, or no `Bounds` row for that exact
  key, the plain bool decides — identical to every build before this ADR.
  Per the #262 reservation, `sqlartisan_construct_*` overrides are checked
  *before* either table, so they keep the last word regardless of what a
  bound says.
- **`SQLA0006`, not `SQLA0002`.** A version shortfall and a dialect mismatch
  are different findings with different remediations — "declare a higher
  version, or override once you've verified your actual engine handles it"
  versus "this dialect fundamentally does not support the construct." Folding
  the version case into `SQLA0002`'s message would either drop the version
  numbers or force one message shape to explain two unrelated situations.
- **`EngineVersion`, a purpose-built comparable type.** `System.Version`
  rejects a bare single-segment string (`"2022"` needs at least a
  major.minor pair), which every SQL-Server/Oracle-year spelling in the
  #262 reserved format is. `EngineVersion` parses dot-separated segments,
  compares numerically with a missing trailing segment read as `0`, and
  drops a trailing letter run in a segment (`23ai` reads as `23`) — the
  exact contract #262 already promised in `docs/analyzer.md`.
- **A machine-checked invariant ties the two tables together.**
  `DialectMatrix.BaselineVersion` is the numeric twin of the
  `VerifiedAgainstVersion` prose, and a test asserts
  `entry.IsSupported(dialect) == (BaselineVersion[dialect] >= bound)` for
  every `Bounds` row — the bool and the bound must agree about what the
  verification baseline itself would report, or the two tables are telling
  two different stories about the same engine.
- **Seed criterion.** A construct enters `Bounds` only with (a) evidence
  already recorded in a primary source (an existing `Entries` comment, an
  XML doc remark, or the #263 issue's seed register), (b) a below-bound
  engine still in vendor support or wide practical use within roughly five
  years (excludes blanket rows like SQL Server 2012 or Oracle 12c), and (c)
  a specific-construct boundary, not one that would blanket a
  `DbmsSupport.All` group. A currently-`false` cell flipping to `true`
  above its bound needs one more thing: **live proof**, because flipping a
  false cell is the one direction ADR 0003's "never a new false positive"
  promise depends on getting right. A pinned
  `gvenzl/oracle-free:23-slim-faststart` sweep lane
  (`Oracle23aiBoundSweepTests`) exists for exactly this — it derives its
  expectations directly from `Bounds`, so any future above-baseline Oracle
  bound is pulled in and proven automatically, with no test-file change.
  `LeftJoinLateral` (inferred from an existing comment about Oracle's
  pre-23ai boolean-literal gap, not a documented "landed in 23ai" fact) did
  not survive this discipline and was dropped before ever seeding it —
  live-proving a chained inference would only confirm the statement parses,
  not that the inference was the actual reason.

  `WithRecursive`'s Oracle-23 candidate is the discipline's fullest recorded
  exercise — it was ultimately **disproven**, in three rounds. The first
  live run rejected SqlArtisan's emission with `ORA-02000: missing AS
  keyword`, initially misread as the select-list alias needing an explicit
  `AS`; a fix forcing `AS` there produced the *identical* error, which is
  what a wrong diagnosis looks like. A raw-SQL grammar probe then settled
  it: both pinned images reject `WITH RECURSIVE` in every spelling — with or
  without the CTE column list, quoted or bare name (21c: `ORA-00905`, 23ai:
  `ORA-02000`, the parser reading `RECURSIVE` as the query name) — while
  both accept the plain-`WITH` recursive form with a column list, Oracle's
  own 11gR2 syntax. The register's "accepted at 23ai" premise was simply
  wrong, and the bound was withdrawn for good. The lasting fix that came out
  of the investigation is orthogonal to the bound: Oracle's recursive `WITH`
  requires the CTE column list, so `WithRecursive()` now derives and emits
  it on every dialect (`WithRecursiveClause`), which every other engine
  accepts.

## Rejected alternatives

- **A full version × dialect × construct matrix.** Rejected in Context above
  — verification cost, and most constructs have no known boundary to encode.
- **Reusing `SQLA0002` with a version argument appended.** Same rule ID for
  two causally different findings (dialect rejects it outright vs. dialect
  accepts it only from some version on) would make severity/suppression
  configuration unable to distinguish them, and the override-key remediation
  text differs in what it's asking the user to verify.
- **Widening `DbmsSupport` to carry the bound inline.** Rejected in Decision
  above — forces every existing `Entries` row to pay syntax for a fact only
  a minority have.

## Consequences

- `Bounds` inherits `Entries`' evidence discipline: every row cites its
  source, mirroring the comment style `Entries` already uses.
- The orphan gate (every `Bounds` key must resolve to a real `Entries` key)
  transitively guarantees every bounded construct already has integration
  sweep coverage, since `MatrixSweepCatalogTests` already requires that of
  every `Entries` key.
- A future above-baseline flip (a currently-`false` cell gaining a bound)
  needs its own live-proof lane before it can ship — the pinned 23ai lane
  already exists for Oracle, and this ADR's `WithRecursive` record is
  the discipline's proof of value: a plausible register claim was caught by
  the lane, a plausible-but-wrong fix was falsified by the same lane, and a
  raw-SQL grammar probe settled the underlying engine fact before anything
  shipped as a silenced false positive.
- When a lane rejects a candidate, diagnose against the *engine's grammar*
  with a raw-SQL probe before patching SqlArtisan's emission — the
  `WithRecursive` record shows an emission-side fix built on a misread
  error reproducing the identical failure.
- The Analyzer cluster (`docs/adr/README.md`) grows to
  0003 + 0008 + 0009 + 0013 + 0014 + 0015. Six members is within reach of
  the consolidation trigger's threshold, though that trigger counts
  rejection categories and enumerated exceptions specifically, which this
  ADR does not add — noted here as a signal for a future consolidation
  discussion, not acted on now.
