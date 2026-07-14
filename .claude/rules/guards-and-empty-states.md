---
description: Guard conventions — the enforcement boundary, empty-state policy, eager vs Build()-time timing, exception message grammar
paths:
  - "src/SqlArtisan/Internal/SqlBuilder/**/*.cs"
  - "src/SqlArtisan/Internal/SqlPart/Condition/**/*.cs"
  - "src/SqlArtisan/Sql/*.cs"
---

# Guards and empty states

The #225 audit's worst findings were statements that *silently* emitted invalid
or wrong SQL (bare `WHERE`, `()` from nested empty groups, correlated-DML
tautologies). Guards exist to convert that failure class into loud errors —
follow these conventions so every new guard lands on the same policy.

## The enforcement boundary (ADR 0007 + 0011 + 0012)

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
  aggregate without `.WithinGroup(...)` (#190).
- *Value-domain*: percentile fraction — finite (pre-existing) and 0..1 (#295).
- *Bounded exception*: aliased `INSERT`/`UPDATE`/`DELETE` target on SQL Server
  (ADR 0011).

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
Still per #243: the empty `IN` collection and empty `VALUES` rows guards
(ERG-05/ERG-07). New guards must land on this policy; never cite a row as
already-enforced without checking the code.

| Position | All-empty behavior |
|---|---|
| Any written condition clause — `.Where(...)` (SELECT/UPDATE/DELETE), `.Having(...)`, aggregate `.Filter(...)`, JOIN/MERGE `.On(...)`, CASE `When(...)`, MERGE `.WhenMatched(cond)` / `.WhenNotMatched(cond)` / `.WhenNotMatchedBySource(cond)` / `.DeleteWhere(...)` | **throw at Build()** |
| Empty SELECT list (#236); empty `IN` collection, empty `VALUES` rows (#243) | throw **eagerly** |

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

## When to throw: eagerly vs at Build()

- **Eagerly (in the factory / clause method)** only when the fact is fixed at
  the call site: a `params` array length, a collection count. Precedent:
  `PartitionBy` (#69) and the empty-`Select()` guard (`SelectItemResolver.ResolveOrThrow`, #236).
  A **value-domain guard** (an argument value no engine accepts, e.g. a
  percentile fraction outside 0..1) is also eager — its three admission
  conditions are ADR 0012 (#295); never domain-check a bound value.
- **At Build()/format time** when later mutation can change the fact:
  conditions (`operator &` mutates a held `AndCondition`, so an empty tree at
  `.Where(...)` time can legitimately become non-empty before `Build()`) and
  builder stages. An eager check here would misfire on legal code.

## Message grammar

One sentence; name the construct by its **SQL spelling**; state the
requirement. Unit tests assert the message verbatim (see the unit-tests rule),
so the wording is part of the contract.

- ✓ `PARTITION BY requires at least one expression.`
- ✓ `The target of a correlated UPDATE or DELETE must be aliased.`
- ✗ `Invalid input.` — names nothing, states nothing.
