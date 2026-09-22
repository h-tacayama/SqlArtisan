# ADR 0022 — Value-domain arguments: every literal value verdict under one diagnostic ID

**Status:** Accepted

## Context

ADR 0003's dialect matrix keys a verdict on a construct's identity — member
name plus optional arity. ADR 0013 added the position dimension the matrix
cannot express (`SQLA0102`). `SQLA0104` had already added a third: **one value
of one argument**, for a literal `DateTimePart` a function's grammar does not
accept.

#528 and #529 surfaced two more instances of that same third dimension:

- #528 (from #527): `RegexpOptions.ExcludingWhiteSpace` emits `'x'`, which
  MySQL 8.0's `match_type` has no letter for — it rejects the call with
  `Incorrect arguments to regexp_like`, while Oracle XE 21.3.0 and
  PostgreSQL 16.13 accept it. The matrix says `RegexpLike` exists on MySQL,
  and it is right: the *function* exists, the *value* has no spelling.
- #529 (from #525): `Top(-1)`, `FetchFirst(-1)`, `FetchNext(-1)` and
  `Limit(-1)` are rejected by SQL Server 2022, PostgreSQL 16.13 and MySQL 8.0
  in their own combinations — and **accepted** by Oracle XE 21.3.0 (a negative
  `FETCH` runs) and by SQLite 3.50.4 (`LIMIT -1` means "no limit").

ADR 0012 ruled both out as eager factory-call guards and recorded each as a stated
non-goal: the alphabets diverge per engine (its condition 3) and an engine
accepts each value (its condition 1). It named the remaining question as a
`SQLA01xx` one, and for the `RegexpOptions` half it was more specific than
that — "an `SQLA0104`-class table (the `DatepartValidity` shape), not a guard",
a classification `.claude/rules/guards-and-empty-states.md` repeats in a
section marked *decided — do not re-file*.

Two facts make the three domains less alike than that suggests.
`RegexpOptions` is `[Flags]`, so its rule reads a *combination* rather than a
single member, which `DatepartValidity`'s lookup does not do; and #529's
trigger is a numeric literal over an unbounded integer domain, not an enum
member at all. So the open question was not whether to report them — it was
whether the differences between the three values are differences a *user* of
the diagnostics should see.

## Decision

**They are not. All three value domains report under `SQLA0104`, backed by two
tables with the shapes their domains actually have.**

- **One ID, and specifically the existing one.** `SQLA0104` is widened from
  "datepart argument" to "argument value", exactly as `SQLA0001` was widened
  from "invalid configuration value" to "any analyzer configuration problem"
  (ADR 0019), and by the same mechanism: several `DiagnosticDescriptor`
  instances sharing one id, each with its own message format. No id is
  allocated, moved or retired, so no `#pragma`, `[SuppressMessage]`,
  `NoWarn` or `dotnet_diagnostic.*` line changes target.
- **The splitting test says merge.** ADR 0019 fixed when a reason earns its own
  id: when a lever that silences one would wrongly reach into the other —
  there, a severity override aimed at a deprecation nag reaching real
  config-error detection. None of these three is a nag. Each reports a value
  the named engine rejects outright, each is remedied the same way (change the
  value, or stop targeting that dialect), and a user who silences one wants the
  others silenced too. That is the condition for sharing, and it holds across
  all three.
- **The granularity unit is the verdict, not the argument type.** ADR 0013 put
  seven unrelated context walks under `SQLA0102` on the reasoning that
  `SQLA0100` covers ~230 constructs under one id. Splitting "an argument value
  the dialect rejects" on the basis of *which enum carries the value* is a
  granularity the repo uses nowhere else, and ADR 0021 makes the companion
  point for the matrix key: apparent free precision on a user-facing surface
  is not free.
- **Two table shapes, because the domains differ where the user does not see
  it.** `DatepartValidity` keys (member, dialect) → accepted `DateTimePart`
  names. `ArgumentValueValidity` holds the other two: an accepted
  `RegexpOptions` member-name set per dialect — keyed by dialect rather than by
  function, because `match_type` is one grammar per engine across its
  `REGEXP_*` functions — and a set of `(construct, dialect)` cells measured to
  reject a negative constant, because an integer domain has no allowlist to
  write. Which table a fact lives in is an implementation detail; the reported
  verdict is not.
- **Absence is silence, and two absences are load-bearing.** Oracle's
  acceptance of a negative `FETCH` and SQLite's `LIMIT -1` are not gaps in the
  table; they are the reason those cells must stay empty. Reporting either
  would flag code that runs today, which is the defect this rule exists to
  avoid.
- **Only a compile-time constant reaches the rule.** A value arriving through a
  variable or a computed expression stays the database's business (ADR 0004),
  the same provable-or-silent discipline ADR 0013 fixed for the context rules.
- **The never-both-fire contract is shared, not copied.** Both rules resolve it
  through `ValueDomainScope`: a dialect `SQLA0100`/`SQLA0101` already flags is
  theirs, an `unsupported` override hands the whole usage to `SQLA0100`, and a
  `supported` override re-arms the finer check — asserting the construct runs
  is not a claim about one value.
- **Every new cell is live-verified and twinned.** The reporting cells and the
  silent ones alike are pinned in the per-engine integration tests, per
  `.claude/rules/dbms-differences.md`. `ArgumentValueValidityParityTests` gates
  the tables against the real public API and rejects a cell the matrix has
  already flagged unsupported, which could never fire.

### The alternative, and why it lost

A new `SQLA0105` was written first and rejected on review. Its case rested on
the claim that folding the new domains in would be a breaking renumber — but
that describes retiring `SQLA0104` *into* a new id, which nobody proposed.
Widening `SQLA0104` moves no id at all. A second case, that the datepart table
carries a false-positive risk the other two do not (its MySQL unit sets are
copied from a vendor grammar table that has itself changed across releases,
per ADR 0012), does not survive either: MySQL's match-parameter alphabet is
vendor grammar in the same sense, arriving whole with the ICU engine
replacement in 8.0. ADR 0012's stability passage separates a *guard* from a
*diagnostic*; it draws no line between these three.

What settled it is that the two directions are not equally reversible. If one
id later proves too coarse, splitting a domain onto its own band id is purely
additive — the move this ADR leaves open below. If two ids later prove
needlessly split, closing them means retiring a published id: a third
renumber, which ADR 0018 set out to make unnecessary and which
`docs/versioning.md` permits only before 1.0. At 0.10.0-beta.1, with
`SQLA0105` never shipped, merging costs no user anything, and it will never be
this cheap again.

### Scope this rule does not claim

The `OFFSET` family (`Offset`, `OffsetRows`) is out, and #532 settled that it
stays out. The engines do diverge — PostgreSQL 16.13 rejects a negative offset
in both spellings, SQLite 3.50.4 reads it as 0, Oracle XE 21.3.0 takes it, all
pinned as twins — so the exclusion is not for want of evidence. It is that the
offset is the argument callers do not write as a literal: the paging recipe
this repo teaches is `Page(int offset) => … .Limit(20).Offset(offset)`, where
the row count is the constant and the offset is the parameter. Cells here would
be correct and inert, while the defect that actually occurs — a computed offset
that goes negative — stays invisible to a rule that reads only constants. That
one is a documentation problem, and `docs/query-statements.md` carries the
note.
`ArgumentValueValidityParityTests` names both members explicitly, so a new
pagination construct cannot join them by being forgotten.

## Consequences

- **`SQLA0104` reports more than it did**, so a user who had already silenced
  it — it shipped in 0.9.0-beta.1 — silently gains that suppression over the
  two new domains. This is the same cost `SQLA0001`'s widening imposed one
  release earlier, and it is mitigated the same way: `CHANGELOG.md` says so
  explicitly. A user who had *not* silenced it sees exactly the warnings a
  separate id would have produced.
- **A value domain is now a recognized class of DBMS difference**, alongside
  the token-level, construct-level, version-bounded and context-bounded classes
  `.claude/rules/dbms-differences.md` already walks. A difference that is one
  argument value wide belongs in one of `SQLA0104`'s two tables, with a primary
  source and a live twin — not in a new rule with a new id.
- **New cells append to the tables.** Each needs the same evidence: a
  live-verified rejection *and* the acceptance twin that proves the probe was
  well-formed. A cell nobody has measured stays out — an unmeasured engine is
  not a rejecting one. ADR 0012's negative `Lag`/`Lead` offset is the next
  candidate on record, and it lands here rather than under a new id.
- **Suppression is per rule ID and now covers three domains at once**
  (`#pragma`, `[SuppressMessage]`, `dotnet_diagnostic.SQLA0104.severity`). No
  new `.editorconfig` key family ships. If one domain ever needs its own knob,
  moving it to the next free dialect-band id is additive and breaks nothing —
  the direction this decision deliberately keeps open.

Related: #528, #529 (this change), #523 (the triage that surfaced both),
ADR 0012 (why none is a `Build()` guard, and where it classified the
`RegexpOptions` gap), ADR 0013 (the context dimension and the
provable-or-silent discipline), ADR 0018 (the bands and what a renumber
costs), ADR 0019 (when a reason earns its own id, and the shared-id
mechanism), ADR 0004 (values travel as data).
