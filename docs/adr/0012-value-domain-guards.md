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
   widen it. This makes a false positive structurally impossible — the property
   dialect availability lacks — and is why this guard needs no opt-in or
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
   stay permissive (analyzer / database). Dialect *count* does not decide this:
   the dividing question is whether the domain is closed — fixed by the
   construct's own definition or by a standard grammar unlikely to grow — or
   open, a vendor's own accepted-value list that has changed, or could still
   change, release to release. A domain attributed to "a vendor's grammar" can
   still be closed in this sense (#454).

The message follows the guard grammar (names the construct, states the
requirement) and is exact-message tested:

> `The percentile fraction must be in the range 0 to 1.`

Enumerated instances: the percentile fraction guards on `PercentileCont` /
`PercentileDisc` — finite (pre-existing) and 0..1 (#295); `Ntile(buckets)` and
`NthValue(expr, n)` — both positive; a `PRECEDING`/`FOLLOWING` frame-bound
offset — non-negative; a window frame's bound kind order — a `BETWEEN` start
must not rank later than its end, a `BETWEEN` end must not be `UNBOUNDED
PRECEDING` and a `BETWEEN` start must not be `UNBOUNDED FOLLOWING` (both
absolute, so a same-kind pair of either is rejected outright), and a single
bound (implicitly paired with `CURRENT ROW`) must not rank past it; the
numeric offset itself is never compared, so two `PRECEDING`/`FOLLOWING`
bounds of the same kind may legally invert (#402); an `IntervalLiteral`
field's precision — 0..9 — and its field range — one of the seven pairings
Oracle's grammar admits, with a trailing precision only on `SECOND`, that
position being the fractional-seconds count (#436). A *sole* field's
precision is deliberately unguarded even for `SECOND`, where the digits read
as Oracle's leading precision: valid text there, so condition 1 fails and the
permissive default stands. `Numtoyminterval`'s and `Numtodsinterval`'s
`interval_unit` argument — restricted to `YEAR`/`MONTH` and
`DAY`/`HOUR`/`MINUTE`/`SECOND` respectively, the exact sets Oracle's own
function definitions accept; no other engine has either function, so any
other value is invalid everywhere (#448). `Wait(seconds)`'s second count —
non-negative: Oracle is the only engine whose `FOR UPDATE` takes a `WAIT`
clause, and it rejects every negative count with ORA-30005 at parse time,
before lock contention can matter (#483).

**Stated non-goals.** Six value shapes look like candidates and are not,
because condition 1 fails outright:

- `BindValue`'s `direction` and `size`. `Size = -1` is SqlClient's own
  `varchar(max)`/`nvarchar(max)` spelling, so it is meaningful rather than
  invalid, and an undefined `ParameterDirection` is rejected loudly by every
  ADO.NET provider at bind time. Both also sit on the data side of condition 2.
- A zero pagination or `FETCH` count. Condition 1 asks for a value no engine
  accepts, and `LIMIT 0` runs on PostgreSQL 16.13 and SQLite 3.50.4
  (live-verified), returning no rows rather than an error — one accepting
  engine settles it, so the other three are not claimed here.
- SQLite's `LIMIT -1`, which means "no limit" — a meaningful value, so the
  `LIMIT`/`OFFSET` family cannot be guarded as universally invalid.
- A negative `TOP`/`FETCH`/`LIMIT` row count (#523), which fails **conditions 2
  and 1 both**. `TopClause`, `FetchClause` and `LimitClause` each carry the
  count as a `BindValue`, so the emitted text is `TOP (@0)` and `FETCH FIRST :0
  ROWS ONLY` — the value never becomes part of the statement, and this ADR's
  guardrail puts a bound value on the data side by construction (ADR 0004).
  Condition 1 fails independently: Oracle XE 21.3.0 executes both
  `FETCH FIRST -1 ROWS ONLY` and `OFFSET -1 ROWS` without complaint, while
  PostgreSQL 16.13 (`LIMIT must not be negative`), SQL Server 2022 (`A TOP N or
  FETCH rowcount value may not be negative.`) and MySQL 8.0 (a parse error on
  `LIMIT -1`) reject theirs — all live-verified. A per-dialect analyzer
  diagnostic could still flag a constant negative count at the call site; that
  is a `SQLA01xx` question, not a guard one.
- A negative `Lag`/`Lead` offset (#523). This one *is* literal-embedded —
  `AnalyticLagFunction` prints the offset into the text — so condition 2 holds,
  and condition 1 is what fails: PostgreSQL 16.13 and SQLite 3.50.4 both accept
  it and read it as the mirror function (`LAG(id, -1)` returns what
  `LEAD(id, 1)` does, live-verified on each), while Oracle XE 21.3.0
  (ORA-01428), SQL Server 2022 (`Offset parameter for Lag and Lead functions
  cannot be a negative value.`) and MySQL 8.0 (a parse error) reject it. Two
  accepting engines settle it. ADR 0011 does not reach it either: on every
  engine that rejects the offset, the mirror function is a valid spelling of
  the same intent, so that ADR's second condition fails as well.

- A `RegexpOptions` match parameter (#523). The letters *are* printed into the
  text (`REGEXP_LIKE(x, p, 'ci')`), so condition 2 holds, and conditions 1 and
  3 both fail. Condition 1: every letter the enum can emit is valid on some
  engine that has the functions at all — `'x'` runs on Oracle XE 21.3.0 and
  PostgreSQL 16.13 though MySQL 8.0's `match_type` has no `'x'` (it takes
  `'u'`, which the other two reject), and a contradictory pair is *accepted*
  on all three, each applying the last letter (`'ci'` matches
  case-insensitively, `'ic'` case-sensitively — live-verified on each). So the
  "mutually exclusive" pair the enum's docs described is a meaningful value,
  not an invalid one. Condition 3: the alphabets diverge per engine, which is
  the definition of a domain no dialect-independent guard can encode. The
  per-value gap MySQL's missing `'x'` leaves is invisible to the
  construct-level matrix, which keys on `RegexpLike` rather than on the option
  — an `SQLA0104`-class table (the `DatepartValidity` shape), not a guard.

Nothing in this family is left open: the negative row count and the negative
`Lag`/`Lead` offset above, and the `RegexpOptions` alphabets, were the
standing questions, and each was excluded by an engine that accepts the
value, where admitting one would have taken a rejection on every engine —
the asymmetry is condition 1's, not the evidence's.

## Consequences

- **`Interval`/`Timestampadd`/`Timestampdiff`'s `unit` deliberately stays
  permissive (`SQLA0104`-only), unlike `Numtoyminterval`'s/`Numtodsinterval`'s
  eager guard above, and this is not scheduled for unification (#454).**
  `Numtoyminterval`'s `YEAR`/`MONTH` restriction is the function's own
  definition — no Oracle revision can widen it without changing what the
  function means. MySQL's unit sets in `DatepartValidity` are copied from a
  vendor grammar table that has itself changed across releases (`MICROSECOND`
  was added, `FRAC_SECOND` deprecated), so the same value could become valid
  on a newer engine — condition 1's false positive is not structurally
  impossible there, only currently absent. An eager guard, fixed before any
  `Dbms` or version is known, would have to commit to one accepted set forever
  and either over-reject a newer engine or under-reject an older one;
  `SQLA0104`'s per-(member, dialect) table is a plain data set an analyzer
  release can revise without touching the shipped throw behavior at all — the
  reason this one stays a diagnostic rather than an exception. The two
  functions' argument sets look identical in shape (a literal `DateTimePart`)
  but differ in this one respect, which is why they take different routes.
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
- **The premise is empirically anchored, not asserted:** the guard now
  intercepts every out-of-range fraction before a build, so no builder path can
  reach the database with one — condition 1 (universally invalid) is instead
  confirmed by raw-SQL integration tests that bypass the guard and execute the
  out-of-domain form directly against each live engine:
  `PercentileCont_FractionOutOfRange_Rejected` (#295) in `OracleTests` /
  `PostgreSqlTests` / `SqlServerTests`;
  `WindowFrame_ValueDomainViolations_Rejected` (#402) in those three and
  `MySqlTests`; `Wait_NegativeSeconds_Rejected` (#483) in `OracleTests`.
- New instances append to the enumerated list here; when in doubt about engine
  divergence, stay permissive. Complements ADR 0007 and ADR 0011; supersedes
  neither. See #295.
