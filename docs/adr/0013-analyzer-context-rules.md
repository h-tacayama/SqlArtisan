# ADR 0013 — Analyzer context rules: position-dependent verdicts under their own diagnostic

**Status:** Accepted

## Context

The dialect matrix (ADR 0003) keys a support verdict on a construct's
identity — member name plus optional arity. Some restrictions are invisible
to that scheme because the verdict depends on *where* the construct sits,
not on it existing: MySQL supports `LIMIT` yet rejects it directly inside an
`IN`/`ANY`/`ALL`/`SOME` subquery (ER_NOT_SUPPORTED_YET), and supports
`GROUPING()` yet rejects it in a query without the `WITH ROLLUP` suffix.
Both are decidable from the fluent chain alone — no engine version or schema
knowledge needed — and both were silent-until-the-database errors
(`.claude/rules/dbms-differences.md` "context-bounded validity"; register
seeded by the #225/#232 triage, materialized by #264).

The third registered candidate — Oracle's `PRIOR` outside `CONNECT BY` — is
not implementable: `PRIOR`/`CONNECT BY` has no SqlArtisan API surface
(wontfix under ADR 0010's vendor-recommended-workaround clause; the
recursive-CTE spelling covers the intent), so there is no invocation to
anchor a rule on.

## Decision

**Context rules: per-rule operation-tree walks in `ContextRules.cs`,
reported under a new diagnostic ID `SQLA0004`, registered from the same
`DialectUsageAnalyzer`.**

- **A new ID, one ID for all context rules.** `SQLA0002`'s message embeds
  the construct-override remediation (`Set '<key> = supported' ...`), which
  is wrong here — the construct *is* supported, and an ADR 0008 key that
  silenced a context warning would also silence that construct's real matrix
  warnings. One shared ID (not one per rule) matches how `SQLA0002` covers
  ~230 constructs and `SQLA0006` covers many positions; per-rule severity is
  the same known granularity limit those rules already document.
- **Suppression is standard Roslyn only** (`#pragma`, `[SuppressMessage]`,
  `dotnet_diagnostic.SQLA0004.severity`). No new `.editorconfig` key family
  ships now — `ValidateConfiguration` (SQLA0001) is untouched. If MySQL ever
  lifts a restriction version-wise or the register grows enough for per-rule
  opt-out, rule-scoped keys (`sqlartisan_context_*`) are the compatible later
  addition.
- **Same analyzer, separate rule class.** Registration mirrors
  `IdentifierLengthRule`: a second `OperationKind.Invocation` action,
  name-filtered on the trigger members (`Limit`, `Grouping`) before config
  resolution, dispatching into `ContextRules`. Walks go upward-only from the
  trigger invocation — bounded, concurrent-safe, no tree scans.
- **Establishment discipline (the ADR 0003 extension).** A rule reports only
  when the context is proven from the same expression, and each direction
  has its own proof obligation:
  - *Presence* (the `Limit` rule): the chain is walked to the argument edge,
    and the bound parameter's type (`ISubquery`) plus the host member name
    identify the position — the overload that actually bound, not a guess.
  - *Absence* (the `Grouping` rule): provable only through the type system —
    `WithRollup()` is declared solely on the stage `GroupBy(...)` returns,
    so a chain whose call after `GroupBy` is anything else can never acquire
    the suffix. A chain still ending at `GroupBy(...)`, or any unrecognized
    parent shape (variable indirection, helper methods), stays silent.
  A reflection contract test pins the API facts each proof rests on, so core
  drift breaks the build rather than the rules' soundness.
- **Hard-coded rules, not a declared table.** Two rules don't warrant the
  `DialectMatrix`-style representation; extract a table when the register
  grows.
- **Every rule needs a primary source and a live proof.** Each shipped rule
  carries an engine-rejection test in the integration suite (the acceptance
  twin is already proven by the sweep or the per-engine facts). A context
  rule cannot be a sweep case — the catalog is keyed by `MatrixKey` and
  these verdicts have no matrix entry — so the proofs live in the per-engine
  test class.

## Rejected alternatives

- **Reusing `SQLA0002`.** Needs a second message format under one ID and
  entangles construct-key suppression with context verdicts (above).
- **A sibling `DiagnosticAnalyzer`.** Duplicates the config-resolution and
  assembly-gate plumbing for no isolation benefit; the housed-rule-class
  pattern already exists (`IdentifierLengthRule`).
- **Widening the matrix key with a context axis.** The key space is the
  cross product of constructs and positions; almost every cell is
  meaningless, and ADR 0008 already rejected a much smaller key-space
  expansion on the same grounds.

## Consequences

- A query can legitimately draw both warnings — `GroupBy(Rollup(x))` on
  MySQL fires `SQLA0002` for `Rollup` and `SQLA0004` for a `Grouping` in the
  same query (it has no `WITH ROLLUP`). Both are facts; neither is
  special-cased away.
- False negatives are accepted by design: any shape the walks don't
  recognize is silent, so refactoring a query into variables or helpers can
  hide a real violation. The database remains the backstop, as everywhere
  else in ADR 0003.
- The trigger set is deliberately minimal: `Limit` alone covers the
  quantified-subquery rule (`Offset` rides the same chain;
  `OffsetRows`/`FetchNext`/`FetchFirst` are already matrix-`false` on MySQL,
  so `SQLA0002` fires regardless of position).
- The Analyzer ADR cluster grows to 0003 + 0008 + 0009 + 0013.
