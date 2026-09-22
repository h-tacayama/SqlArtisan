# ADR 0022 — Analyzer value-domain diagnostics: one ID for a literal argument value a dialect rejects

**Status:** Accepted

## Context

ADR 0003's dialect matrix keys a verdict on a construct's identity — member
name plus optional arity. ADR 0013 added the position dimension the matrix
cannot express (`SQLA0102`). #528 and #529 surfaced a third dimension it cannot
express either: **one value of one argument**.

- #528 (from #527): `RegexpOptions.ExcludingWhiteSpace` emits `'x'`, which
  MySQL 8.0's `match_type` has no letter for — it rejects the call with
  `Incorrect arguments to regexp_like`, while Oracle XE 21.3.0 and
  PostgreSQL 16.13 accept it. The matrix says `RegexpLike` exists on MySQL,
  and it is right: the *function* exists, the *value* has no spelling.
- #529 (from #525): `Top(-1)`, `FetchFirst(-1)`, `FetchNext(-1)` and
  `Limit(-1)` are rejected by SQL Server 2022, PostgreSQL 16.13 and MySQL 8.0
  in their own combinations — and **accepted** by Oracle XE 21.3.0 (a negative
  `FETCH` runs) and by SQLite 3.50.4 (`LIMIT -1` means "no limit").

ADR 0012 already ruled both out as `Build()`-time guards and recorded each as a
stated non-goal: the alphabets diverge per engine (its condition 3) and an
engine accepts each value (its condition 1). It named the remaining question as
a `SQLA01xx` one. `SQLA0104` had meanwhile shown the technique — a
per-(value, dialect) table resolved by enum member name, never by the
underlying integer — so what was open was not *whether* to add a rule but what
shape the pair should take.

Two facts make the pair less alike than it first looks. `RegexpOptions` is
`[Flags]`, so the rule reads a *combination* rather than a single member, which
`DatepartValidity`'s lookup does not do; and #529's trigger is a numeric
literal, not an enum member at all, over a domain no allowlist can enumerate.

## Decision

**One diagnostic ID, `SQLA0105`, over both value domains, backed by two tables
with the shapes their domains actually have.**

- **One ID, not two.** The `SQLA0001`/`SQLA0002` split is the precedent that
  decides this: IDs separate when a lever that silences one would wrongly
  silence the other. Here neither half is a nag, both report a value the named
  engine rejects outright, and both are remedied the same way — change the
  value, or stop targeting that dialect. A user who silences one wants the
  other silenced too, so they share a severity knob. The message carries the
  kind of value in its own placeholder (`'{1}' is not a valid {2} for '{0}' on
  {3}`), so one format serves `match option` and `row count` alike.
- **`SQLA0104` is not folded in.** It is the same *class* of verdict, and
  merging it would be a breaking renumber — the third — against ADR 0018's
  intent that the bands make the IDs stop moving. A shared class does not buy
  enough to spend that.
- **Two table shapes, because the domains differ.** The match parameter gets
  an allowlist of accepted member names per dialect (`DatepartValidity`'s
  shape, keyed by dialect rather than by function, because `match_type` is one
  grammar per engine across its `REGEXP_*` functions). The row count gets a set
  of `(construct, dialect)` cells measured to reject a negative constant,
  because an integer domain has no allowlist to write.
- **Absence is silence, and two absences are load-bearing.** Oracle's
  acceptance of a negative `FETCH` and SQLite's `LIMIT -1` are not gaps in the
  table; they are the reason those cells must stay empty. Reporting either
  would flag code that runs today, which is the defect this rule exists to
  avoid.
- **Only a compile-time constant reaches the rule.** A count or option arriving
  through a variable or a computed expression stays the database's business
  (ADR 0004), the same provable-or-silent discipline ADR 0013 fixed for the
  context rules.
- **The never-both-fire contract is shared, not copied.** `SQLA0104`'s rule and
  this one both resolve it through `ValueDomainScope`: a dialect
  `SQLA0100`/`SQLA0101` already flags is theirs, an `unsupported` override
  hands the whole usage to `SQLA0100`, and a `supported` override re-arms the
  finer check — asserting the construct runs is not a claim about one value.
- **Every cell is live-verified and twinned.** The reporting cells and the
  silent ones alike are pinned in the per-engine integration tests, per
  `.claude/rules/dbms-differences.md`. `ArgumentValueValidityParityTests` gates
  the tables against the real public API and rejects a cell the matrix has
  already flagged unsupported, which could never fire.

### Scope this rule does not claim

The `OFFSET` family (`Offset`, `OffsetRows`) is out. The only negative-offset
cell pinned as a twin is Oracle's acceptance of `OFFSET -1 ROWS` (ADR 0012), so
no dialect has a rejection to report; filling the rest in would widen this
change past what #529 asked for, on evidence that is not yet in the repo.
`ArgumentValueValidityParityTests` names both members explicitly, so a new
pagination construct cannot join them by being forgotten.

## Consequences

- **`SqlArtisan.Dialect` gains its sixth rule**, in band per ADR 0018 — the
  next ID inside the band, not the next free number overall.
- **The `SQLA0104` rule changed shape** when its override/matrix skip moved to
  `ValueDomainScope`. Behavior is unchanged and its own suite still gates it;
  what the move buys is one place where the contract can be got wrong.
- **A value domain is now a recognized class of DBMS difference**, alongside
  the token-level, construct-level, version-bounded and context-bounded classes
  `.claude/rules/dbms-differences.md` already walks. A difference that is one
  argument value wide belongs in `ArgumentValueValidity.cs`, with a primary
  source and a live twin.
- **New cells append to the tables.** Each needs the same evidence: a
  live-verified rejection *and* the acceptance twin that proves the probe was
  well-formed. A cell nobody has measured stays out — an unmeasured engine is
  not a rejecting one.
- **Suppression is per rule ID** (`#pragma`, `[SuppressMessage]`,
  `dotnet_diagnostic.SQLA0105.severity`). No new `.editorconfig` key family
  ships; if the two domains ever want separate knobs, splitting the second one
  onto its own band ID is the compatible later move.

Related: #528, #529 (this change), #523 (the triage that surfaced both),
ADR 0012 (why neither is a `Build()` guard), ADR 0013 (the context dimension
and the provable-or-silent discipline), ADR 0018 (the bands), ADR 0004 (values
travel as data).
