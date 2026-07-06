# ADR 0005 — Public API location

**Status:** Accepted

## Context

The public surface is the library's contract: it must be discoverable and clearly
separated from internals that are free to change.

## Decision

- The public API is one **`public static partial class Sql`**, split across
  `src/SqlArtisan/Sql/Sql.{A..W}.cs` — one file per leading letter of the function
  name.
- The remaining public surface lives under `src/SqlArtisan/SqlBuilder/`.
- A narrow set of **public table-reference types** lives under
  `src/SqlArtisan/SqlPart/TableReference/` — the types consumers subclass or
  instantiate to name relations and reference columns: `DbTableBase` / `DbTable`,
  `CteBase` / `Cte`, `DerivedTableBase` / `DerivedTable` — and the `DbColumn` they
  expose lives beside them under `src/SqlArtisan/SqlPart/Expression/`. These are
  public by necessity and part of the contract. *(Erratum 2026-07: `DbColumn`'s
  path corrected; the decision itself is unchanged.)*
- The types application code must **name** in a declaration position — a
  helper method's return type, an accumulator variable, a `List<>` element, a
  shared `static readonly` field — are public by the same necessity:
  `SqlExpression`, `SqlCondition`, `ISubquery`, `TableReference`, `SortOrder`,
  `ExpressionAlias`, `CommonTableExpression`, and `DbSequence`. The boundary
  rule has three conjuncts — a type leaves `Internal` only when **all** hold:

  1. It is a query's **content** — a relation, value, predicate, sort item, or
     handle that helps determine the result set. *Not* clause syntax
     (`OrderByClause`, `PartitionByClause`, `OfClause`, `SeparatorClause`),
     *not* statement decoration that leaves the result set unchanged
     (optimizer hints, lock behaviors), and *not* a pending intermediate
     (an incomplete window/ordered-set aggregate — naming it is the
     anti-pattern the pending design prevents).
  2. A **mainstream flow must write its name** in a declaration position
     (`var` cannot infer a `null` initializer, a method signature, or a
     field).
  3. **No root type already names it** — clause fragments fail here too, since
     the natural unit of reuse is the completed expression, which the roots
     already name.

  Every concrete node under the roots (`AndCondition`, `LikeCondition`,
  `CountFunction`, …) stays in `Internal/` and is held only through them;
  pure-mechanism bases with no user-facing members (`SqlPart`) stay too.
  *(Moved from `Internal` to the root namespace, #244.)*
- **Everything under `Internal/` is implementation detail**, even where a type is
  `public` for technical reasons.

## Consequences

- Discoverable: `Sql.` surfaces the whole function API, in small per-letter files.
- Internals refactor freely without breaking consumers.
