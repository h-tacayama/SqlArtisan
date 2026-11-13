# ADR 0013 — Analyzer context rules: position-dependent verdicts under their own diagnostic

**Status:** Accepted — #822 widens the verdict to a position an engine runs
with another meaning (the last section); everything else here stands.

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
reported under a new diagnostic ID `SQLA0102`, registered from the same
`DialectUsageAnalyzer`.**

- **A new ID, one ID for all context rules.** `SQLA0100`'s message embeds
  the construct-override remediation (`Set '<key> = supported' ...`), which
  is wrong here — the construct *is* supported, and an ADR 0008 key that
  silenced a context warning would also silence that construct's real matrix
  warnings. One shared ID (not one per rule) matches how `SQLA0100` covers
  ~230 constructs and `SQLA0103` covers many positions; per-rule severity is
  the same known granularity limit those rules already document.
- **Suppression is standard Roslyn only** (`#pragma`, `[SuppressMessage]`,
  `dotnet_diagnostic.SQLA0102.severity`). No new `.editorconfig` key family
  ships now — `ValidateConfiguration` (SQLA0001) is untouched. If MySQL ever
  lifts a restriction version-wise or the register grows enough for per-rule
  opt-out, rule-scoped keys (`sqlartisan_context_*`) are the compatible later
  addition.
- **Same analyzer, separate rule class.** Registration mirrors
  `IdentifierLengthRule`: a second `OperationKind.Invocation` action,
  name-filtered on the trigger members (`Limit`, `Grouping`) before config
  resolution, dispatching into `ContextRules`. Walks go upward-only from the
  trigger invocation — bounded, concurrent-safe, no tree scans. The `UNION`
  sort-key rule (#800) is the exception: it reads a table's declaration through
  ADR 0014's alias trace.
- **Establishment discipline (the ADR 0003 extension).** A rule reports only
  when the context is proven from the same expression, short of the false
  positives `docs/analyzer.md` states (ADR 0014 records the symbol-based one),
  and each direction has its own proof obligation:
  - *Presence* (the `Limit` rule): the chain is walked to the argument edge,
    and the bound parameter's type (`ISubquery`) plus the host member name
    identify the position — the overload that actually bound, not a guess.
  - *Absence* (the `Grouping` rule): provable only through the type system —
    `WithRollup()` is declared solely on the stage `GroupBy(...)` returns,
    so a chain whose call after `GroupBy` is anything else can never acquire
    the suffix. A chain still ending at `GroupBy(...)`, or any unrecognized
    parent shape (variable indirection, helper methods), stays silent. The
    `Returning` rule (#569) is the forward-reading case: `Into(...)` is the
    only step its stage declares, so a result built, or widened to
    `ISqlBuilder`, has visibly lost it — whatever the receiver was held in —
    while a result still typed as that stage stays silent.
  - *Declaring interface* (the four joined-DML rules, #523, a `WITH`
    inside `INSERT ... SELECT`, #569, and a `WHERE` on a `MERGE` action, #587;
    `FOR UPDATE` over a grouped query, #523's fifth, is an ordinary presence
    walk): a step name alone decides nothing —
    every one of them is declared on several stages or factories —
    but the *pair* of name and declaring interface pins one statement position,
    so the overload that bound **is** the position and no walk is needed:
    `From` on `IDeleteBuilderDelete` is a joined `DELETE` wherever it is
    written. This mode has no indirection blind spot: a receiver parked in a
    variable keeps its static type.
  A reflection contract test pins the API facts each proof rests on, so core
  drift breaks the build rather than the rules' soundness. For the third mode
  that means the *whole set* of interfaces declaring a trigger name, since a
  new one would silently join or escape a verdict.
- **Hard-coded rules, with a table only where the register earned one.** The
  walking rules stay one method each — their proofs have no common shape. The
  DML shapes (#523) do share one, so they are a `ClassifyDmlShape` switch over
  (member, declaring interface) plus a per-shape list of rejecting dialects.
- **Every rule needs a primary source and a live proof.** Each shipped rule
  carries an engine-rejection test in the integration suite (the acceptance
  twin is already proven by the sweep or the per-engine facts). A context
  rule cannot be a sweep case — the catalog is keyed by `MatrixKey` and
  these verdicts have no matrix entry — so the proofs live in the per-engine
  test class.

## Rejected alternatives

- **Reusing `SQLA0100`.** Needs a second message format under one ID and
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
  MySQL fires `SQLA0100` for `Rollup` and `SQLA0102` for a `Grouping` in the
  same query (it has no `WITH ROLLUP`). Both are facts; neither is
  special-cased away.
- False negatives are accepted by design: any shape the walks don't
  recognize is silent, so refactoring a query into variables or helpers can
  hide a real violation. The database remains the backstop, as everywhere
  else in ADR 0003.
- The trigger set is deliberately minimal: `Limit` alone covers the
  quantified-subquery rule (`Offset` rides the same chain;
  `OffsetRows`/`FetchNext`/`FetchFirst` are already matrix-`false` on MySQL,
  so `SQLA0100` fires regardless of position).
- A rule may name several dialects. The DML spellings are each valid on some
  engine and rejected by two or three others, so the message joins the
  configured targets its rejecting set covers and stays silent when the
  configuration names none of them.
- `From`, `InnerJoin` and `Using` are on the hot path of every query. The
  declaring-interface test — a name compare, no walk — runs before the
  per-tree config lookup, so an ordinary `SELECT` pays only that.
- The Analyzer ADR cluster grows to 0003 + 0008 + 0009 + 0013.

### A position that changes the meaning, added under this decision

#822 widened the verdict: a construct an engine runs with another meaning in a
position reports under `SQLA0102` too, as a token read as something else does
under `SQLA0100` and a value under `SQLA0104` (ADR 0022, #684), its twin
asserting that meaning in place of the rejection proof. The partition holds: a
position the engine misreads is suppressed for the reason a rejected one is. Two
instances came with it:

- SQL Server 2022 pages an embedded compound's `ORDER BY ... OFFSET ... FETCH`
  against its last branch, where a top-level compound's pages the combined
  result, in each of the twelve embedding positions its twin runs
  (`ContextRule_PagedCompoundInSubquery_PagesTheLastBranch`; the wrapped-first
  workaround, `PagedCompoundWrappedFirst_PagesTheCombinedResult`). Microsoft's
  `ORDER BY` reference limits its set-operator rule to a top-level query.
- MySQL 8.0 runs `FOR UPDATE` after a set operator and locks the last branch's
  rows alone. #764 left that to a docs warning; it now reports beside Oracle's
  and PostgreSQL's rejections
  (`MySqlTests.ContextRule_ForUpdateAfterSetOperator_LocksOnlyTheLastBranch`).

The same change read the compound's branches for the `ORDER BY`-without-a-row-limit
rule, which SQL Server rejects there too (Msg 1033,
`ContextRule_OrderedCompoundInSubquery_Rejected`); Oracle's verdict on an ordered
compound is unmeasured, so the rule stays silent there.
