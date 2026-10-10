# ADR 0026 — The analyzer's contract: what a release may change in its IDs and configuration

**Status:** Accepted

## Context

The analyzer exposes no types, but users write its names into their builds:
`#pragma warning disable SQLA0xxx`, `[SuppressMessage]`, `<NoWarn>`,
`<WarningsAsErrors>`, `dotnet_diagnostic.SQLA0xxx.severity`,
`dotnet_analyzer_diagnostic.category-SqlArtisan.*.severity`, the
`sqlartisan_*` keys and the `<SqlArtisanSyntax<Dbms>>` properties. A stale ID or
category in any of those is silent: a build with `TreatWarningsAsErrors` and a
nonexistent ID or category in every one of those positions reports nothing
(#655).

Before 1.0, `docs/versioning.md` said only that "analyzer diagnostic updates
(the SQLA rules) may also land in a minor release". Read literally, that let a
minor release renumber, remove or reuse an ID, rename a category or a key, or
change what a value means. Three records disagreed with it or with each other:

- ADR 0009 ships "a matrix correction" as a **patch** release, while
  `versioning.md` classes analyzer updates as **minor** — and a correction that
  adds a `SQLA0100` report fails a `severity = error` build.
- ADR 0022 calls moving a value domain off `SQLA0104` later "additive" and
  "breaks nothing". It is a split: `dotnet_diagnostic.SQLA0104.severity =
  error` stops reaching the moved domain, and `<NoWarn>SQLA0104</NoWarn>` lets
  its warnings back in.
- ADR 0022 also cites `versioning.md` as permitting the retirement of an ID
  "only before 1.0". No such rule existed.

The release that added `SQLA0002` labelled it **Breaking:**, because it fails
`TreatWarningsAsErrors` builds, while later ones added `SQLA0301`/`SQLA0302` and
widened `SQLA0104` as plain additions. Nothing said which was right.

## Decision

**After 1.0, these are covered: changing them is a major-version change.**

- Each diagnostic ID and the rule it names. An ID is never renumbered or
  reused — the bands exist so no rule has to move again (ADR 0018) — and is
  removed only by a major that retires its rule or merges its reports into
  another ID; the removed ID stays retired
  (`DiagnosticOrderingTests.RetiredId_IsNeverReused`).
- The four category strings and their bands (ADR 0018).
- Each rule's default severity and whether it is enabled by default, in the
  raising direction. A user who weighed a rule at its default — `SQLA0203`,
  Info and off because it advises on correct code — accepted that default; a
  release that raises it changes the rule's terms, not merely what it finds.
  Lowering either is minor.
- How reports are partitioned among IDs. Splitting an ID makes an escalation
  of the old one miss the moved reports, and merging two lets a suppression of
  one silence the other, so the partition is settled before a rule ships, by
  who would suppress or escalate the reports together.
- The key names: `sqlartisan_syntax_<dbms>`, and `sqlartisan_construct_<member>`
  / `sqlartisan_construct_<member>_arity<N>` with the rule deriving them from a
  member's name and declared parameter count. The `SqlArtisanSyntax<Dbms>`
  MSBuild properties, spelled as the `Dbms` member.
- The values and what they mean: `any`, `none`, an engine version (the release
  year on SQL Server; elsewhere the engine's own version number, major with an
  optional minor and patch), `supported`, `unsupported` — all matched without
  regard to case.
- The precedence between them: `.editorconfig` over the MSBuild property, an
  arity key over a member key, an override over the matrix.
- Silence until a dialect is configured: no rule from `SQLA0100` on reports
  before one is.
- The removed `sqlartisan_target_*` keys and properties keep reporting
  `SQLA0001` throughout 1.x (ADR 0024).

**Not covered: a diagnostic's title, message text and help-link anchor.**

**Any other change that adds reports is a minor release; any other that only
removes reports is a patch.** A new rule takes the next ID in its band; a new reason joins an
existing ID; a matrix correction or a new table cell starts reporting
something. Each can fail a build that escalates warnings, and each is the point
of upgrading an analyzer the user opted into: the analyzer ships inside the
package, so it changes only when the user upgrades it — unlike an analyzer that
ships in the SDK, which `AnalysisLevel` exists to hold still. Gating each rule
behind a level key was rejected: an opt-in layered on an opt-in, plus a
per-rule version table to maintain. Treating every new report as major was
rejected: it would freeze the guard rails ADR 0010 exists to grow. From the
first release after 1.0, the CHANGELOG marks each such entry **New warning:**
(`.claude/rules/docs-style.md`) so a build that escalates warnings can find
what an upgrade adds; before then any release may break, and entries carry no
mark. A new key, value or DBMS token is
minor too.

**A key is retired by deprecating it first.** A minor release accepts both
spellings and reports the old one under its own `SqlArtisan.Configuration` ID,
so it can be silenced without silencing configuration errors (ADR 0019's
splitting test); the next major removes it, and from then a set key reports
`SQLA0001` rather than configuring nothing (ADR 0024). An ID is retired only in
a major.

### Two partitions this ADR records

- **`SQLA0204` shares one ID between a column wrapped in a function and a
  leading-wildcard pattern.** The descriptor justified the sharing by one
  remediation — leave the column bare on the filtered side — which does not fit
  `t.Name.Like("%x")`, where the column is already bare. The shared verdict is
  the real reason: in both, the predicate as written gives the index nothing to
  range over, and a user who decides that finding does not matter for a column
  decides it for both shapes. The remedies differ and the docs say how.
- **`SQLA0103` stays apart from `SQLA0104`**, though an over-long identifier
  literal is a literal value. ADR 0022 joined the domains a dialect *rejects*
  under one ID; PostgreSQL does not reject an over-long identifier, it truncates
  it, so two names can silently collide. And `SQLA0103` reports once per
  dialect with that dialect's own limit and unit, where `SQLA0104` joins every
  failing dialect into one report.

## Consequences

- `docs/versioning.md` gains the user-facing statement of the above, shaped
  like TableClassGen's command-line surface.
- ADR 0009's "a matrix correction ships as a core patch release" holds only for
  a correction that removes reports; one that adds a report is a minor release.
- ADR 0022's two statements are corrected: moving a domain off `SQLA0104`
  after 1.0 is a split, a major-version change; and the retirement rule it
  cited is the one stated here.
- `.claude/rules/public-api-design.md` carries the contract as a rule clause,
  matched by the files that define the IDs, keys and values and that decide
  precedence and the unconfigured silence, so a diff changing any of them meets
  it — except a `*Rule.cs` file that re-points a report at another existing ID,
  which review holds; `.claude/rules/docs-style.md` carries the **New warning:** mark, where a
  CHANGELOG edit meets it.

Related: #655 (this change), ADR 0009 (bundled distribution), ADR 0018 (the
bands), ADR 0019 (the splitting test), ADR 0022 (the value domains), ADR 0024
(a removed key is reported).
