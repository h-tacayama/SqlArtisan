# ADR 0027 — The public surface: one namespace, and capability return types

**Status:** Accepted — supersedes [ADR 0005](0005-public-api-in-sql-partial-class.md)'s
root-namespace criteria and its "everything under `Internal/` is implementation
detail" clause; ADR 0005's placement of the `Sql` partial class stands.

## Context

ADR 0005 kept the concrete nodes, clauses and builder stages in
`SqlArtisan.Internal`, public only because a `Sql.*` signature returned them.
A returned type is named in every caller's compiled IL, so renaming one breaks
an assembly built against the last release whatever the namespace says;
`docs/versioning.md` therefore covered those names, while ADR 0005 and CLAUDE.md
still called them free to refactor (#686). In .NET a `.Internal` namespace
signals the opposite of a promise — EF Core warns on using one (EF1001) — so
the records could not be reconciled by wording alone.

The namespace exported 304 types: 73 builder-stage interfaces, 221 sealed and
10 abstract classes. 188 of the sealed classes declared no public member and
appeared in no public parameter; every member they offered came from a base
(`SumFunction : UnfilteredAggregateFunction`, whose base declares `Over`).
Their names were surface that carried nothing, and 1.0 is the last release
that can take surface back.

## Decision

- **A factory, operator or fluent member returns the narrowest public type that
  carries every member its result offers.** A node keeps a type of its own only
  when it declares a member (`Over`, `Filter`, `WithinGroup`, `Escape`, `Then`,
  `AsTable`, `Percent`, …) or a parameter takes it (`DistinctKeyword`,
  `FrameBound`, …); the one recorded exception is `BindArrayValue`, which an
  execution layer type-tests to tell an array bind from a scalar one
  (`public-api-design.md`). Any other returns its nearest public base: the
  family base that declares its members (`AggregateFunction`,
  `UnfilteredAggregateFunction`, `AnalyticFunction`, `ValueAnalyticFunction`,
  `TableReference`), a base a parameter takes (`LockBehaviorBase`, which
  `ForUpdate` takes; `EqualityCondition`, which `Set` takes), or else
  `SqlExpression`, `SqlCondition` or `SqlPart`.
- **Every other concrete node is `internal`**, in `src/SqlArtisan/Internal/` and the
  `SqlArtisan.Internal` namespace, which exports nothing.
- **Every public type lives in the root namespace `SqlArtisan`**, its file
  under `src/SqlArtisan/Sql/`, `SqlBuilder/`, `SqlPart/` or `Metadata/`; the
  library exports no other namespace. `PublicSurfaceBoundaryTests` gates the
  namespace, that a type no caller constructs is handed back by some
  signature, that only an allowlisted handle offers a constructor, and that a
  public class declares a member, is taken by a parameter, or is another public
  type's base, `BindArrayValue` aside, and that a member returning a `SqlPart`
  declares its node's nearest public type.
- **No type is kept for a modifier not yet offered.** Which modifiers a later
  release might add is not an input: a criterion keyed on it would rest on each
  reviewer's forecast. A modifier added later to one function of a shared return
  type comes as an overload or a marker argument (`Count(Distinct, x)`'s shape),
  ahead of the existing `object` slots; where it shares an arity with an existing
  overload, the neighbouring argument orders are compiled first, since a `null`
  literal there can turn a call ambiguous (CS0121). Offering it as a member on
  the result needs a new return type, a binary break after 1.0.

## Consequences

- The library exports 147 types, down from 339, and a node can be renamed,
  split or merged after 1.0 without a major version.
- Every compiled caller must rebuild against this release, and code that named
  a concrete node type (`SumFunction s = Sum(x)`) no longer compiles; no docs
  page or recipe did.
- A tooltip shows `SqlExpression` where it showed `AbsFunction`; the factory's
  own XML docs say what it emits. Every public type carries XML docs, so the
  `Internal/SqlPart/**` CS1591 suppression is gone.
- Most returned types are no longer sealed, so a C# reference comparison between
  two SqlArtisan values that failed with CS0019 can compile and bind a `bool`.
  Those orders sit in `public-api-design.md`'s open list, and `SQLA0301` reports
  one only where neither operand rules out an expression (ADR 0023); keeping one
  sealed type per node to hold them shut is the cost this decision declines.
  `SQLA0302` proves a non-sealed value only where a SqlArtisan member or operator
  has just returned it, so an operator's result reports where its sealed node type
  once carried the proof, and a held result does not.
- An analyzer rule cannot tell nodes apart by a receiver's static type: the
  `FirstValue` / `LastValue` window rule reads the factory call `.Over(...)` is
  chained onto, and stays silent on a value held in a variable (ADR 0003). The
  parity gates read the node a factory builds from its IL, not its return type.
