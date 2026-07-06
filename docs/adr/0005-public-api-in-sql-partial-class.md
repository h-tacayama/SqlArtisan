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
  helper method's return type, an accumulator variable, a `List<>` element —
  are public by the same necessity: `SqlExpression`, `SqlCondition`,
  `ISubquery`, `TableReference`, `SortOrder`, `ExpressionAlias`, and
  `CommonTableExpression`. That naming test is the boundary rule: a type
  leaves `Internal` when a mainstream flow must write its name and no
  root-namespace alternative exists. Every concrete node under the roots
  (`AndCondition`, `LikeCondition`, `CountFunction`, …) stays in `Internal/`
  and is held only through them; pure-mechanism bases with no user-facing
  members (`SqlPart`) stay too. *(Moved from `Internal` to the root
  namespace, #244.)*
- **Everything under `Internal/` is implementation detail**, even where a type is
  `public` for technical reasons.

## Consequences

- Discoverable: `Sql.` surfaces the whole function API, in small per-letter files.
- Internals refactor freely without breaking consumers.
