---
description: Guard conventions — the enforcement boundary, empty-state policy, eager vs Build()-time timing, exception message grammar
paths:
  - "src/SqlArtisan/Internal/SqlBuilder/**/*.cs"
  - "src/SqlArtisan/Internal/SqlPart/**/*.cs"
  - "src/SqlArtisan/Sql/*.cs"
  - "src/SqlArtisan/SqlBuilder/*.cs"
  - "src/SqlArtisan/SqlPart/**/*.cs"
---

# Guards and empty states

The #225 audit's worst findings were statements that *silently* emitted invalid
or wrong SQL (bare `WHERE`, `()` from nested empty groups, correlated-DML
tautologies). Guards exist to convert that failure class into loud errors —
follow these conventions so every new guard lands on the same policy.

## The enforcement boundary (ADR 0007 + 0011 + 0012 + 0017)

This table synthesizes the current boundary from the ADR cluster. Use it as
the first check when deciding whether a new case should throw; the ADRs carry
the full rationale.

| Category | Library behavior | Mechanism | Dividing test |
|----------|-----------------|-----------|---------------|
| **Incomplete construct** — a mandatory element is missing (e.g. window function without `.Over(...)`) | **Reject** | Compile-time preferred (pending type → only the completing call yields `SqlExpression`); runtime `ArgumentException` backstop in `object`-typed positions | No supported dialect, in any configuration, accepts the bare token — the expression is unfinished |
| **Value-domain violation** — an embedded literal value lies outside a universally fixed domain (e.g. percentile fraction outside 0..1) | **Reject eagerly** at the factory call | `ArgumentException`; all three conditions must hold: (1) universally invalid, (2) literal-embedded and call-site-fixed, (3) dialect-independent | The emitted text carrying this value is valid on no supported dialect — the domain is fixed by the SQL standard or identically by every engine |
| **Bounded exception** — a complete construct valid on some dialect but structurally invisible to the analyzer *and* with no valid spelling on the resolved target | **Reject at Build(Dbms)** | `Validate(Dbms)` hook on `SqlBuilderBase`; dialect-scoped, position-scoped | The analyzer cannot see it (value-level, not construct-level) *and* the resolved target has no valid spelling — both conditions required |
| **Dialect availability** — a complete construct that some engine does not support | **Emit faithfully** (ADR 0001); surfaced by the opt-in analyzer (ADR 0003) and ultimately the database | Permissive | Valid on at least one supported dialect |

**Enumerated instances of each rejection category:**

- *Incomplete*: window/analytic function without `.Over(...)` (#150); ordered-set
  aggregate without `.WithinGroup(...)` (#190); any of `InnerJoin`/`LeftJoin`/
  `RightJoin`/`FullJoin`/`JoinLateral` with no `.On(...)`/`.Using(...)` —
  accepted on SQLite (all five) or MySQL (`InnerJoin`/`JoinLateral` only) as an
  unlabeled `CROSS JOIN` spelling, not a construct with independent meaning
  (ADR 0017); `Output(...)` (SQL Server) combined with `Returning(...)`, with
  `Using(...)` on `DELETE`, or with `OnConflict(...)`/`OnDuplicateKeyUpdate(...)`
  on `INSERT` — no dialect accepts both halves of any pairing (#400).
- *Value-domain*: percentile fraction — finite (pre-existing) and 0..1 (#295);
  `Ntile(buckets)` and `NthValue(expr, n)` — both positive; a `PRECEDING`/
  `FOLLOWING` frame-bound offset — non-negative; a window frame's bound kind
  order (`UNBOUNDED PRECEDING` < `PRECEDING` < `CURRENT ROW` < `FOLLOWING` <
  `UNBOUNDED FOLLOWING`) — a `BETWEEN` start must not rank later than its end,
  a `BETWEEN` end must not be `UNBOUNDED PRECEDING` and a `BETWEEN` start must
  not be `UNBOUNDED FOLLOWING` (both absolute, so a same-kind pair of either is
  rejected outright), and a single bound (implicitly paired with `CURRENT ROW`)
  must not rank past it; the numeric offset itself is never compared, so two
  `PRECEDING`/`FOLLOWING` bounds of the same kind may still legally invert
  (`BETWEEN 3 PRECEDING AND 5 PRECEDING`) (#402); an `IntervalLiteral` field's
  precision — 0..9 — and its field range — one of the seven pairings Oracle's
  grammar admits, with a trailing precision only on `SECOND`, that position
  being the fractional-seconds count. A *sole* field's precision stays
  unguarded even for `SECOND` (the digits read as Oracle's leading precision
  there, so the text is valid and condition 1 fails) (#436); `Numtoyminterval`/
  `Numtodsinterval`'s `interval_unit` — restricted to the exact set each
  Oracle function accepts (`YEAR`/`MONTH`, `DAY`/`HOUR`/`MINUTE`/`SECOND`),
  since no other engine has either function (#448); `Wait(seconds)`'s second
  count — non-negative, the only `FOR UPDATE WAIT` domain Oracle (the sole
  engine with the clause) parses, live-verified at 21c and 23ai (#483).
- *Bounded exception*: aliased `INSERT`/`UPDATE`/`DELETE` target on SQL Server
  (ADR 0011).

"Structurally invisible to the analyzer" is a per-guard fact, not a law: the
correlated-DML guard's provable subset now has an advisory analyzer duplicate
(SQLA0300, ADR 0014) — the `Build()` guard remains the enforcement boundary,
and suppressing the diagnostic never disables the throw.

## The empty-state policy (#236)

Never elide a clause the caller wrote. A written condition clause with no
runnable condition (every operand excluded) **fails loudly at Build()** —
eliding it would silently change the query, and even a `SELECT` `WHERE`-less
read is a load risk, so "the SQL you write is the SQL that runs" is honored by
refusing to guess rather than by quietly dropping the clause. "No restriction"
is expressed by **omitting the clause** entirely.

**Status:** shipped in #236 — the recursive emptiness check (`SqlPart.IsEmpty`),
the shared `ConditionGuard.ThrowIfEmpty` used by every condition clause's
`Format`, the eager empty-`Select()` guard
(`SelectItemResolver.ResolveOrThrow`), and the freeze-after-Build guard (#245).
The empty `IN`/`NOT IN` collection and empty `VALUES` row guards (ERG-05/ERG-07,
#243) shipped in #396, alongside the same sweep's guards for empty `SET`
(`UpdateBuilder`/`InsertBuilder`), empty `DO UPDATE SET` / `ON DUPLICATE KEY
UPDATE` / MERGE `.ThenUpdateSet()`, an empty `SELECT` `.From()`, and an empty
`Sql.Decode(...)` pairs array; #397 added the INSERT column-list/VALUES-row
width cross-check alongside them; the 1.0 release review added the empty
explicit `INSERT` column-list guard — an empty `columns` array silently became
a positional `INSERT` and bypassed the #397 width check. New guards must land
on this policy; never cite a row as already-enforced without checking the code.

| Position | All-empty behavior |
|---|---|
| Any written condition clause — `.Where(...)` (SELECT/UPDATE/DELETE), `.Having(...)`, aggregate `.Filter(...)`, JOIN/MERGE `.On(...)`, CASE `When(...)`, MERGE `.WhenMatched(cond)` / `.WhenNotMatched(cond)` / `.WhenNotMatchedBySource(cond)` / `.DeleteWhere(...)` | **throw at Build()** |
| Empty SELECT list (#236); empty `SELECT`/`UPDATE` `.From()`; empty `IN`/`NOT IN`, empty `VALUES` row (#243); empty `SET`/`DO UPDATE SET`/`ON DUPLICATE KEY UPDATE`/MERGE `.ThenUpdateSet()`; empty `Sql.Decode(...)` pairs (#396); INSERT column-list vs. `VALUES` row width mismatch (#397); empty explicit `INSERT` column list (1.0 release review) | throw **eagerly** |

There is **no elision** — omitting a clause is the only "no restriction". The
throw lives in the clause node's own `Format` (Build()-time), so it fires
whichever statement reuses the node; `WhereClause` is shared by SELECT/UPDATE/
DELETE and the aggregate `FILTER`, which intercepts first with its own message.

Condition emptiness is **recursive**: a tree whose operands are all empty is
empty. Never test an operand with `is EmptyCondition` — that is the bug that
emitted `()` for nested all-empty groups even in mixed states; use the recursive
`IsEmpty`, **including `NOT`** — a `NOT` over an empty operand is itself empty
(`NOT ()` is the probe-confirmed hazard a plain AND/OR walk misses). An excluded
operand *beside* an active one still drops out inside a non-empty AND/OR (that is
`ConditionIf`'s contract); only an entirely empty clause throws.

## Null arguments: where the runtime-guard obligation stops

The guard mission targets **silent** wrongness — a build that succeeds with SQL
the caller did not mean. Judge a null argument by which failure it produces:

- **Silent acceptance** (the statement still builds): guard it, whatever the
  parameter's type. Shipped instances: `object`-typed value positions
  (`ExpressionResolver`'s "Use `Sql.Null`…" message), string identifiers
  (`StringGuard`), null elements inside arrays/`params` (#403), a null
  subquery in `CteBase.As` (previously emitted `WITH "c" AS ()`),
  `new BindValue(null)` (a never-true `= NULL` predicate the factory already
  rejected), and `default(OutputParameter)` — a struct default no annotation
  can flag, revalidated at format time.
- **Loud failure** (a `NullReferenceException` from dereferencing a single
  non-nullable reference parameter — `Column(DbColumn)`,
  `Exists(subquery)`, the condition operators): the nullable annotation *is*
  the contract — the compiler warns (CS8604), and the throw lands either at
  the factory call (`Column`) or at `Build()` (a stored subquery or
  condition). Either way the statement never builds, so nothing is silently
  wrong. No runtime guard is owed; do not file these in review.

Settled during the 1.0 release review, where one panel seat filed the loud-NRE
class as a defect and another declined the identical class as
annotation-enforced — this clause exists so the next review doesn't relitigate
it. (`DbColumn`'s constructor owner guard predates the clause and stays.)

`FactoryGuardSweepTests` enforces the silent-acceptance side mechanically for
every public `Sql` factory whose return type its `TryBuild` embeds into a
statement: a degenerate argument must throw or its exact SQL must sit in the
acceptance catalog. What `TryBuild` cannot embed — pending types awaiting a
completing call, and a handful of complete clause objects not yet wired in —
is a *recorded* blind spot: `ReturnTypes_AreEmbeddableOrRecorded` fails on any
return type that is neither embedded nor in the `UnembeddedReturnTypes`
ledger, so the gap cannot grow silently and shrinks whenever `TryBuild`'s
switch is extended. Instance members that take clause objects (`.Over(...)`,
`.WithinGroup(...)`) are outside the sweep entirely — that surface stays on
manual review, and its one audited silent acceptance (`Over(null)` emitting
`OVER ()`) is guarded at `OverClause.Of`.

The loud-failure exemption above covers only positions whose null the
compiler tracks: a **single** non-nullable reference parameter (CS8604 at the
call site). An element inside an array or `params` tail is flagged only as a
literal (`[null]` draws CS8625); a *computed* element — a default-initialized
slot, a value from untracked flow — reaches the call with no warning at all,
so the silent-acceptance bullet governs elements regardless of how loudly
they later fail, which is why #403 and the `INSERT` column-list element guard
convert those NREs to named exceptions.

## When to throw: eagerly vs at Build()

- **Eagerly (in the factory / clause method)** only when the fact is fixed at
  the call site: a `params` array length, a collection count. Precedent:
  `PartitionBy` (#69), the empty-`Select()` guard (`SelectItemResolver.ResolveOrThrow`, #236),
  and the `WithRecursive(...)` column-name guard (`WithRecursiveClause` — the
  anchor's resolved select items are fixed at the call, #263).
  A **value-domain guard** (an argument value no engine accepts, e.g. a
  percentile fraction outside 0..1) is also eager — its three admission
  conditions are ADR 0012 (#295); never domain-check a bound value.
- **At Build()/format time** when the position's own architecture calls for
  it: conditions (`WhereClause`/`HavingClause`/etc. are shared by every
  statement type that embeds them — SELECT/UPDATE/DELETE and the aggregate
  `FILTER` — so the throw lives in the clause node's own `Format`, letting one
  implementation serve all of them; see "no elision" above) and builder stages
  (`SqlBuilderBase._parts` only reaches its final shape once every stage call
  has run). An eager check here would misfire on legal code.

## Message grammar

One sentence; name the construct by its **SQL spelling**; state the
requirement. Unit tests assert the message verbatim (see the unit-tests rule),
so the wording is part of the contract.

- ✓ `PARTITION BY requires at least one expression.`
- ✓ `The target of a correlated UPDATE or DELETE must be aliased.`
- ✗ `Invalid input.` — names nothing, states nothing.

The `Invalid type for <X>: <type>` family is built by one helper,
`ExpressionResolver.UnresolvableValue`, and `<X>` names **the position the
value reached** — `SelectItem`, `OrderByItem`, `GroupByItem`, `InsertValue`,
`Assignment`, and `nameof(Bind)` where the factory resolves its own argument.
Naming instead the type that position requires reads as a tautology against
the offending type, never saying what the caller did wrong, and leaks an
internal name into the public failure surface: that is how three `SET`-list
guards said `Invalid type for EqualityCondition` until #497. The one type name
in the family is `SqlExpression`, for the generic value position whose
requirement *is* that — not a precedent for a narrower position.
