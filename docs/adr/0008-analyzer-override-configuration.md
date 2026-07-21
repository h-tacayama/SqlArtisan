# ADR 0008 — Analyzer override configuration: keys, precedence, and what's out of scope

**Status:** Accepted

## Context

ADR 0003 commits to a permissive API plus an opt-in analyzer whose "value
rests on a verified per-DBMS matrix." Two problems follow directly from that
commitment, both raised while designing the analyzer (#93) rather than
discovered after shipping it:

1. **The matrix will be wrong sometimes.** A dialect's real-world behavior
   varies by engine version (`MERGE` landed in PostgreSQL 15; a construct the
   matrix marks unsupported today may work on a newer release), and the
   matrix itself is hand-curated from docs/tests/CHANGELOG entries rather
   than exhaustively verified against every version. ADR 0003 already
   anticipates *incompleteness* (an unentered construct stays silent) but
   not *incorrectness* (an entered-but-wrong verdict) — with no escape
   hatch, a wrong `false` is a false positive the user cannot silence short
   of disabling the whole rule.
2. **Some overloaded members have different dialect support across their
   own overloads.** `StringAgg`'s 3-argument inline-`ORDER BY` form is
   PostgreSQL-only; its 2-argument form is also valid on SQL Server. A
   single support verdict per member name is too coarse for these.

## Decision

**Per-construct override keys**, read from the same `AnalyzerConfigOptions`
surface as the target-dialect setting (`.editorconfig` primarily, an MSBuild
property as a secondary path):

```ini
sqlartisan_construct_<member> = supported | unsupported
sqlartisan_construct_<member>_arity<N> = supported | unsupported
```

- **Two key levels, not one.** A bare member-level key applies to every
  overload of that member (including ones added in a later version); an
  arity-level key (suffixed `_arity<N>`, where `N` is the declared parameter
  count) applies only to that one overload. This is the minimum granularity
  that covers the `StringAgg` case without inventing a type-based key scheme
  for the rarer case (two overloads with the *same* arity but different
  parameter types — see "Rejected" below).
- **Specific wins.** Resolution order is: user arity-level override → user
  member-level override → matrix arity-level entry → matrix member-level
  entry → silent. An override is checked *before* the matrix is even
  consulted, so a user can override a construct the matrix has no opinion
  on at all (harmless — nothing will ever match an override key for a
  member name that isn't real).
- **Two values, not three.** `supported` silences a matrix `false`;
  `unsupported` forces a warning despite a matrix `true` (or despite no
  matrix entry). A `banned`-style third value for team policy ("we've
  decided not to use this construct," independent of dialect fact) was
  considered and rejected — see below.
- **No DBMS axis in the key.** The key does not encode *which* dialect the
  override applies to; it applies to whichever target is resolved for that
  file. This was a deliberate simplification over a per-construct ×
  per-dialect key space: the target is already scoped per file via
  `.editorconfig` sections (see "Mixed-dialect projects" in
  `docs/analyzer.md`), so a second DBMS axis in the override key itself would
  only ever be exercised by a project targeting more than one dialect from
  the *same* file — a case ADR 0003 already puts out of scope.

## Rejected alternatives

- **A `banned` value / dedicated policy rule.** Distinguishing "my engine
  can't run this" (dialect fact, `unsupported`) from "our team has decided
  not to use this" (policy, independent of engine) is real, but severity is
  already controlled per rule ID (`dotnet_diagnostic.SQLA0002.severity`), so
  a policy value would need its own rule ID to be independently escalatable
  — a second rule purely for a use case `BannedApiAnalyzers` already serves
  well. Deferred rather than built speculatively; can be added later without
  breaking the existing two-value scheme (owner decision).
- **Full construct × dialect override matrix.** Considered and rejected for
  key-space size: ~230 public members × 5 dialects is over a thousand
  possible keys, most of which would never be set, and a typo in an unused
  corner of that space is exactly as silently ineffective as today's smaller
  space (Roslyn cannot enumerate configured keys to catch a typo either
  way) — more keys only means more surface for that same failure mode.
- **Type-based keys for same-arity, different-parameter-type overloads**
  (e.g. distinguishing MySQL's `Match(object, params object[])` from
  SQLite's `Match(DbTableBase, object)`, both declared with two parameters).
  The matrix instead enters the *union* of such overloads' support, which
  can under-warn but never false-positive (ADR 0003's priority order).
  Revisit only if a real member needs it and the key naming can stay
  readable.
- **Per-call target inference** (reading a literal `.Build(Dbms.X)` argument
  elsewhere in the same file as a hint, to support same-file multi-dialect
  code without `.editorconfig` sections). Ruled out as a data-flow analysis
  ADR 0003 already scoped out for the general case ("per-call mixing within
  one file — covered by docs, not the analyzer"); directory-scoped
  `.editorconfig` sections are the supported mixed-dialect pattern instead.

## Consequences

- The override mechanism only ever *narrows* false positives or *widens*
  detected true positives — it cannot invent a warning for a construct the
  matrix and the user both stay silent on, preserving ADR 0003's "an
  incomplete matrix cannot false-positive" property.
- A member-level override's scope automatically grows if SqlArtisan ships a
  new overload of that member later; this is stated as intended behavior
  (not a version-compatibility bug) in `docs/analyzer.md`.
- Key-name typos remain permanently undetectable (a Roslyn API limitation,
  not a design gap this ADR could close) — documented as a known limitation
  rather than silently left unmentioned.
- If a `banned`/policy value is added later, it is additive (a third
  recognized value plus a new rule ID) and does not require revisiting the
  precedence order above.
