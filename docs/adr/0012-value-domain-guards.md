# ADR 0012 — Value-domain guards: rejecting an argument value no engine accepts

**Status:** Accepted

## Context

ADR 0007 has the library reject only *incomplete* constructs — ungrammatical on
every dialect because a mandatory element is missing — and emit everything else
faithfully, with dialect availability surfaced by the opt-in analyzer (ADR 0003)
and ultimately the database. Its dividing test:

> Is there any supported dialect, in any configuration, where this exact text is
> valid SQL? If no → reject. If yes-somewhere → dialect availability →
> permissive.

#295 surfaced a case that test resolves but the rationale never covered:
`PercentileCont(1.5)`. The construct is **complete** — nothing is missing — yet
the dividing test still answers **no**: the SQL standard fixes the percentile
fraction's domain to 0..1, and no supported engine, in any version or
configuration, accepts a fraction outside it. Where the function exists at all
(Oracle, PostgreSQL, SQL Server — per the live-verified matrix), an out-of-range
fraction fails at execution; where it doesn't (MySQL, SQLite), the text is
invalid regardless of the fraction. The out-of-range value is valid *nowhere*,
including on every engine that accepts the in-range form.

The same constructors already refused a non-finite fraction (NaN / infinity) —
the same shape, admitted before this rationale was written down. This ADR
records the category both guards belong to.

This is **not dialect availability**. Availability presumes divergence — some
engine accepts what another rejects, so the author might be deliberately
targeting the engine that does, and the analyzer can advise per target. Here
there is no divergence to advise about and no engine the author could be
targeting; neither of ADR 0007's permissive-side mechanisms has a role to play.

## Decision

The library **may reject an argument value eagerly, at the factory call**, when
all three of the following hold:

1. **Universally invalid.** The emitted text carrying this value is valid on no
   supported dialect, in any version or configuration — the domain is fixed by
   the SQL standard (or identically by every engine), so no engine update can
   widen it. This makes a false positive structurally impossible: the property
   dialect availability lacks, and the reason this guard needs no opt-in or
   override.
2. **Literal-embedded and call-site-fixed.** The value is a scalar the library
   itself will print into the SQL text, fixed at the factory call — so the
   eager-throw rule applies (`.claude/rules/guards-and-empty-states.md`). Values
   that travel as **bind parameters are never domain-checked** — data validation
   belongs to the database (ADR 0004); this category covers only what becomes
   part of the statement text.
3. **Dialect-independent.** The guard fires before any `Dbms` is chosen and
   behaves identically for every target — it encodes no per-engine knowledge. A
   domain any engine widens or narrows disqualifies the guard; divergent domains
   stay permissive (analyzer / database).

The message follows the guard grammar (names the construct, states the
requirement) and is exact-message tested:

> `The percentile fraction must be in the range 0 to 1.`

Enumerated instances: the percentile fraction guards on `PercentileCont` /
`PercentileDisc` — finite (pre-existing) and 0..1 (#295).

## Consequences

- **ADR 0007's dividing test is unchanged; its rationale now covers both ways
  to land on "no".** A construct can be valid nowhere because a mandatory
  element is missing (incomplete — ADR 0007) or because an embedded value lies
  outside a universally fixed domain (this ADR). Both reject; everything
  valid-somewhere still emits faithfully.
- **Distinct from ADR 0011's exception, and cheaper to admit.** ADR 0011 rejects
  a construct that *is* valid on some dialects, so it must clear a high,
  enumerated bar (analyzer blind spot *and* no valid spelling). A value-domain
  guard withholds nothing valid, so it needs no such bar — but it must satisfy
  all three conditions above; absent any one, the default stays ADR 0007's:
  emit faithfully.
- **Guardrail: never generalize to data.** The category must not creep into
  validating bound values, column contents, or anything the database receives
  as a parameter — condition 2 is the boundary, and a guard proposal that
  inspects a `BindValue` fails this ADR by construction.
- New instances append to the enumerated list here; when in doubt about engine
  divergence, stay permissive. Complements ADR 0007 and ADR 0011; supersedes
  neither. See #295.
