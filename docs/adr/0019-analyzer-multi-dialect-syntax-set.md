# ADR 0019 — Analyzer multi-dialect syntax set: `sqlartisan_syntax_*`, one key per DBMS

**Status:** Accepted

## Context

The analyzer assumes a project targets **one** DBMS (`sqlartisan_target_dbms`)
and writes that dialect deliberately — ADR 0001's posture, and right for most
users. A second population exists: ISVs and packaged software shipping one
codebase against several customer-chosen engines, Oracle→PostgreSQL migration
projects keeping the old dialect checked while the new one comes online, and
multi-vendor procurement requirements. Today they get nothing: naming one
dialect leaves a construct that breaks on an engine they also ship to
completely silent.

This does not weaken ADR 0001. What that ADR rules out is rewriting one query
per database — a portability abstraction. This is detection, not rewriting:
the SQL a call produces is still exactly the SQL that dialect's grammar
allows; checking it against a *set* of dialects instead of one is ADR 0010's
guard rail applied along a wider axis, not a new kind of guard.

Also out of scope: ISO/IEC 9075 conformance. "Runs on the engines I ship to"
neither implies nor is implied by standard conformance (`MERGE` is standard
but absent from MySQL/SQLite; `FETCH FIRST` is standard but MySQL spells it
`LIMIT`). Naming all five DBMS is the practical proxy for "standard SQL only,"
and it reuses evidence the shipped `DialectMatrix` already carries.

## Decision

**One `.editorconfig` key per DBMS, presence-based, valued by version or
`any`/`none`:**

```ini
sqlartisan_syntax_postgresql = 16
sqlartisan_syntax_oracle     = 19
sqlartisan_syntax_sqlite     = any
```

- **Two values only: an `EngineVersion` string, or `any`.** No `true`/`yes`
  aliases — a smaller domain is easier to document and validate, and a
  presence-based key with the DBMS already in its name reads naturally
  without one. `none` explicitly excludes a DBMS in a narrower
  `.editorconfig` scope even when a broader one (or the MSBuild property)
  named it — the only way to carve an exception out of a family that has no
  "primary" key to override.
- **No primary target, no second-class diagnostic.** An early draft kept a
  primary target plus a separate "runs today, blocks migration" finding.
  Both are dropped: a construct that fails on a migration candidate is a
  real problem for this population, exactly as weighty as one failing
  today, so there is one class of matrix finding, not two, and no DBMS is
  privileged. This adds no diagnostic to the dialect or schema bands.
- **Every rule becomes set-valued**, not per-DBMS-diagnostic:
  `SQLA0100`'s message joins every failing dialect's name into one
  diagnostic (its `messageFormat` has one variable slot for the dialect
  list). `SQLA0101` and `SQLA0103` report **one diagnostic per failing
  DBMS** instead — their message carries a per-dialect value (a required
  version, a length limit and its unit) that cannot be joined without
  redesigning the template. `SQLA0102`'s per-construct dialect check and
  the schema rules' (`SQLA0200`–`SQLA0205`) "is a target configured at
  all" gate both become "is this DBMS in the set" / "is the set non-empty."
  A construct can fail two different ways across the set at once — unsupported
  on one dialect, merely version-bound on another — and both are reported;
  dropping either would hide a real, differently-actionable fact.
- **A `sqlartisan_construct_*` override is resolved once per usage, not once
  per DBMS.** It is the user's own claim about their configuration
  ("supported"/"unsupported" reads as "on every DBMS I've named"), which is
  dialect-independent — unlike the matrix, which genuinely differs per
  DBMS. `DialectSupportResolver`'s old single `Resolve()` entry point is
  split into an override half (`ResolveOverride`, called once) and a matrix
  half (`MatchMatrixEntry` + `Evaluate`, called once per DBMS in the set)
  to make that difference a shape in the code, not just a documented rule.
- **Family wins outright over the legacy pair — never merged, no
  desugar-when-absent.** If any `sqlartisan_syntax_*` key is present
  anywhere in a file's effective options (`.editorconfig` or the
  MSBuild-property fallback), the family governs the whole resolution and
  `sqlartisan_target_dbms` / `sqlartisan_target_version` are not consulted
  at all — not even to fill in a DBMS the family didn't name. An earlier
  draft made desugaring conditional on "no family key visible," which left
  two cases unresolved (a family key visible only through the MSBuild
  property; both families set entirely via MSBuild properties, no
  `.editorconfig` at all). Family-wins-outright removes both by
  construction, at the cost of a real one-way door: a root-scoped legacy
  key merging into a narrower family scope would force every such scope to
  write `none` for every dialect it doesn't want, just to keep the legacy
  key from injecting one back in.
- **The legacy pair still works, desugars exactly as before, and now also
  reports `SQLA0002`** — a dedicated id, not a fifth `SQLA0001` reason.
  `SQLA0001` exists specifically so a misconfiguration never goes silently
  unnoticed (a bad key name, a bad value, an empty resolved set); a
  Warning-severity deprecation nag sharing that id would hand every
  `NoWarn`/severity override that silences the nag the same reach into
  real config-error detection. `SQLA0002` fires unconditionally — even when
  the pair resolves perfectly correctly — and at Warning, not Info: a nag
  that only fires on misuse, or that stays out of build output, tells a
  `TreatWarningsAsErrors` project nothing before the major version that
  removes the pair does it for them. Both ids stay in `SqlArtisan.Configuration`
  (ADR 0018's band), so a category-wide severity setting still reaches both
  when that's what's wanted.
- **`SQLA0001` widens from "invalid value" to "configuration problem."**
  Moving the DBMS name from a key's *value* into its *name* makes a typo
  (`sqlartisan_syntax_postgres`) detectable for the first time —
  `AnalyzerConfigOptions.Keys` (public virtual on the referenced Roslyn
  4.8.0, verified by reflection) can enumerate keys carrying the
  `sqlartisan_syntax_` prefix and flag any suffix outside the five DBMS
  names. `Keys`' default implementation throws `NotImplementedException`
  on a host that doesn't override it (verified by reflection) — including
  this repository's own test double before this change — so the
  enumeration is wrapped in a try/catch and degrades to skipping key-name
  validation, never to taking the whole analyzer down. Two more report
  reasons join the id: a family present but every key resolved to `none`
  (the same "went quiet with no error" failure the legacy pair's backward
  compatibility section already names, reachable here by one
  typo-adjacent `none`), and the legacy pair coexisting with a present
  family (naming the DBMS the legacy pair would have contributed, since a
  message naming only the ignored key leaves "replaces, not adds" invisible).
  Four report reasons under one id is expressed as four `DiagnosticDescriptor`
  instances sharing `SQLA0001` — Roslyn allows more than one descriptor per
  id, and `DiagnosticOrderingTests`' stable sort keeps them in declared order.

## Rejected alternatives

- **`true`/`yes`/`off` as aliases of `any`/`none`.** A smaller value domain
  is easier to document and validate, and the presence-based key shape
  doesn't need a truthy alias to read naturally.
- **`sqlartisan_oracle` (no middle word).** The most elegant shape, but a
  future key family sharing no distinguishing prefix could collide, and
  `sqlartisan_syntax_` is what lets an older analyzer ignore a newer key
  family instead of misreading it — the same degradable property ADR 0003
  gives the matrix itself, extended to the config surface.
- **`engine_*`, `guard_*` / `conform_*` / `analyze_*` / `lint_*`,
  `rule_*` / `diagnostic_*` / `severity_*`, `grammar_*`.** Each either
  claims ownership of an engine the caller doesn't yet have (the exact
  migration case this feature exists for), loses a preposition once a DBMS
  name is appended, names analyzer machinery the config surface already
  uses for something else, or is a plausible runner-up with no
  distinguishing edge over `syntax` — the word the engine's own error
  message would have used.
- **Merging the legacy pair into the family instead of family-wins-outright.**
  A root-scoped legacy key would keep injecting its DBMS into every
  narrower family scope, so "here, only Oracle" would require writing
  `none` for a dialect that scope never named.
- **A primary target plus a separate portability-set diagnostic.** Dropped
  in Decision above — no DBMS in the set is second-class.
- **Per-DBMS-qualified `sqlartisan_construct_*` overrides**
  (`sqlartisan_construct_listagg_postgresql`). Deferred, not rejected — the
  override key space already trades off breadth for typo-safety (ADR 0008),
  and a per-DBMS axis multiplies that space by five before any real need is
  shown.

## Consequences

- Naming a new DBMS in the family can light up many warnings at once —
  `docs/analyzer.md` documents per-id severity, path-scoped `.editorconfig`
  sections, and `sqlartisan_construct_*` overrides as the staged-adoption
  levers, the same three a single-dialect project already has.
- A `TreatWarningsAsErrors` project using the legacy pair fails to build on
  upgrade with no config change of its own — the accepted cost of an
  unconditional Warning-severity deprecation, called out in `CHANGELOG.md`
  as a breaking change with its one-line migration and the `SQLA0002`-only
  escape hatch (not `SQLA0001`, not the `SqlArtisan.Configuration` category
  setting — silencing the nag must never silence real config-error
  detection).
- `DialectMatrix` and `MatrixSweepCatalog` are unchanged — this feature adds
  no dialect data, reusing all 310 entries as-is, which is what keeps it
  cheap and lets the existing coverage gate keep "silence = verified" true
  across a set exactly as it did for one target.
- The Analyzer cluster (`docs/adr/README.md`) grows to
  0003 + 0008 + 0009 + 0013 + 0014 + 0015 + 0018 + 0019. This ADR refines
  ADR 0008's precedence section specifically (a family key now exists
  alongside the per-construct override key ADR 0008 designed) — cross-linked
  both directions, not edited in place, since Accepted ADRs are immutable.

Related: #93 (the matrix), #218 (`sqlartisan_target_dbms`), #262
(`sqlartisan_target_version`), #263 / ADR 0015 (version bounds), #432 (this
change), #433 / ADR 0018 (the id bands this depends on), ADR 0003
(degradable design), ADR 0008 (override resolution order — refined here),
ADR 0010 (mission), ADR 0001 (portability as a non-goal).
