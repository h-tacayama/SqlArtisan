---
description: Guard conventions — the empty-state policy, eager vs Build()-time timing, exception message grammar
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

## The empty-state policy (#236)

Elide a clause only where an all-empty condition plausibly means "no
restriction"; fail loudly everywhere else.

**Status:** this table is the policy *decided* in #236, not shipped behavior —
today the library still emits bare `WHERE ` / `SELECT  FROM` / `IN ()` for
these states (probed in the #225 follow-up). New guards must land on this
policy; never cite a row as already-enforced without checking the code.

| Position | All-empty behavior |
|---|---|
| SELECT `.Where(...)`, `.Having(...)`, aggregate `.Filter(...)` | **elide the clause** |
| UPDATE / DELETE `.Where(...)` | **throw at Build()** (eliding turns filtered DML into a full-table write) |
| JOIN `.On(...)`, CASE `When(...)`, MERGE `.On(...)`/`.WhenMatched(cond)`/`.DeleteWhere(...)` | throw at Build() |
| Empty SELECT list (#236); empty `IN` collection, empty `VALUES` rows (#243) | throw **eagerly** |

Condition emptiness is **recursive**: a tree whose operands are all empty
renders nothing. Never test an operand with `is EmptyCondition` — that is the
bug that emitted `()` for nested all-empty groups even in mixed states; use the
recursive emptiness check, **including `NOT`** — a `NOT` over an empty operand
is itself empty (`NOT ()` is the probe-confirmed hazard a plain AND/OR walk
misses).

## When to throw: eagerly vs at Build()

- **Eagerly (in the factory / clause method)** only when the fact is fixed at
  the call site: a `params` array length, a collection count. Precedent:
  `PartitionBy` (#69); the empty-`Select()` eager guard is decided in #236
  but not yet landed.
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
