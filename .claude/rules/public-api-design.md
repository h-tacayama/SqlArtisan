---
description: Public API design decisions — naming categories, overload split for analyzer arity, collection parameters, factory return types, no opinion-holes
paths:
  - "src/SqlArtisan/Sql/*.cs"
  - "src/SqlArtisan/SqlPart/**/*.cs"
  - "src/SqlArtisan.Analyzers/DialectMatrix.cs"
  - "tests/SqlArtisan.IntegrationTests/Infrastructure/MatrixSweepCatalog.cs"
---

# Public API design

Decision principles distilled from the #225 expressibility triage
(#231–#245). The mechanical six-touch-point procedure stays in the
`sa-add-sql-function` skill; this rule covers the *decisions* made before it.

## Naming: three categories, in priority order

1. **SQL-token names** — underscores are the only word boundaries: each
   underscore-delimited segment gets one leading capital and the rest
   lowercase; a token with no underscore stays a single word
   (`ADD_MONTHS`→`AddMonths`, `DATETRUNC`→`Datetrunc`, never invented
   internal capitals). CLAUDE.md carries the one-line summary; this is the
   full rule.
2. **Symbol-only operators → glyph names, but only for value-returning
   (expression) operators** — precedent `JsonArrow` (`->`) / `JsonArrowText`
   (`->>`) / `DoublePipe` (`||`, #234). A symbol operator that returns a
   *predicate* instead gets a semantic name: `ArrayContains` (`@>`),
   `ArrayContainedBy` (`<@`), `ArrayOverlaps` (`&&`), `JsonbContains` (`@>`),
   `JsonbExists`/`JsonbExistsAll`/`JsonbExistsAny` (`?`/`?&`/`?|`), `TsMatch`
   (`@@`) all took the semantic name, not a glyph. When adding a new symbol
   operator, check which side it falls on before naming it.
3. **Non-token helpers** (`ConditionIf`, `Hints`, `Group`, `Bind`,
   `BindArray`, `Sequence`, `Asterisk`) — invented names are allowed *only*
   here, for API affordances that correspond to no SQL token.

Never invent a token-like name for a real construct: `CountAll()` was rejected
(#233) because no `COUNT_ALL` token exists — `COUNT(*)` landed as
`Count(Asterisk)` (#233/#242 — `Asterisk` is the shared qualified-star
marker, not a dedicated `Count` parameter), and real tokens like SQL Server's
`COUNT_BIG` keep their conventional names available.

**Escape hatch for a same-token cross-dialect collision the CLR can't
disambiguate:** `Contains` (SQL Server's full-text predicate,
`object, object`) and Oracle's `CONTAINS(doc, query)` score function share a
token but not a return category, and both would otherwise need the same
`(object, object)` signature — same-name overloading is impossible. Compare
`Match` (Sql.M.cs), where MySQL's `Match(object, params object[])` and
SQLite's `Match(DbTableBase, object)` *can* share one name because their
parameter types disambiguate; here they can't, so one side keeps the bare
token (`Contains`, the plainer predicate use) and the other gets an invented
qualifier (`ContainsScore`, #233-class invention, justified only by the CLR
constraint — record any new instance here rather than resolving it ad hoc).

## BCL simple-name collisions: record here, don't rename

A faithful SQL-token name can collide with a common BCL static-utility type of
the same name: a file combining `using static SqlArtisan.Sql;` with an
unqualified `Type.Member` call on the BCL type fails to compile (`CS0119`),
because the simple name resolves to the imported method first. The collision
is real but narrow: it only fires on member access in that one file (a sibling
file without the `using static` is unaffected), and it has a cheap
per-call-site fix (qualify the BCL type, e.g. `System.Array.Empty<T>()`).

This is **not** grounds for a rename — the SQL-token naming rule above still
wins, and inventing a non-token name to dodge a BCL collision has no more
precedent than inventing one to encode an opinion (the `CountAll` rejection,
above). Nor does it warrant a `<remarks>` on the factory: the failure is a
loud compile error that names the factory itself (and `sa-write-xml-docs`
keeps point-of-use docs out of this anyway). Record known instances here
instead — `Sql.Array` (`System.Array`), `Sql.Match`
(`System.Text.RegularExpressions.Match`) (#338), `Sql.Group`
(`System.Text.RegularExpressions.Group`), `Sql.JsonValue`
(`System.Text.Json.Nodes.JsonValue` — that BCL type also has a static
`Create` factory, so the collision is reachable, not just theoretical).
Before naming a new member after a literal SQL token, check it against common
BCL static-utility type names (`Array`, `Convert`, `Math`, `Type`, `Enum`,
`Console`, ...) so a new collision ships as a recorded trade-off, not a later
surprise.

A **type-name** (not a factory-name) collision is a different, narrower
mechanism: `DbColumn` (this library's expression node) and
`System.Data.Common.DbColumn` share a simple name, which only matters if a
file imports both `SqlArtisan` and `System.Data.Common` unqualified and
*declares* the ambiguous simple name — `using static` member access isn't the
trigger here, ordinary namespace resolution is (`CS0104` on the bare name,
resolved by qualifying either type). Record it here for the same reason:
not grounds for a rename, cheap to qualify at the one call site that needs it.

## Overload split for analyzer arity

The analyzer keys the dialect matrix by the **declared** parameter count
(`method.Parameters.Length`), so a single `params` overload can never carry an
arity-restricted matrix entry — every call site reports the same declared
arity. When dialect support differs by argument count, **split the overloads
so the declared arities differ**:

> Shipped shape (#234): `Concat(object, object)` (arity 2, all dialects) +
> `Concat(object, object, object, params object[])` (arity 4, `oracle: false`)
> lets `("Concat", 2)` / `("Concat", 4)` matrix entries warn on Oracle's
> 2-argument limit with zero analyzer-engine changes. `Grouping` (#235) was the
> first from-scratch use of this pattern.

Before adding a matrix entry, check the `MatrixKey` collision caveat in
`DialectMatrix.cs`: keys are (name, arity) with **no parameter types**, so
same-name same-arity overloads collide into a support union.

## Collection parameters: `IReadOnlyCollection<T>`, not `IEnumerable<T>`

> Worked example — do not repeat (#243 ERG-07): an `In<T>(IEnumerable<T>)`
> overload would silently re-bind `x.In("abc")` — today one bind via
> `params object[]` — into three `char` binds, because `string` implements
> `IEnumerable<char>` and a generic method in normal form beats `params`
> expansion. `string` does **not** implement `IReadOnlyCollection<char>`,
> while `List<T>`, arrays, and sets all do — so `IReadOnlyCollection<T>`
> keeps strings on the `params` path and still covers every runtime
> collection.

## Separator/format-string parameters: `string` = inline literal, `object` = bound

When a parameter's dialect grammar sometimes *requires* a literal there (SQL
Server `STRING_AGG`'s separator, ADR 0004's literal-position exception), type
it `string` and emit it inline; when the dialect accepts a bound expression,
type it `object` and resolve it normally. Shipped instances: `StringAgg(object
expr, string separator)` (inline — SQL Server requires a literal there) vs.
`Listagg`/`GroupConcat`'s `object separator` (bound — Oracle/SQLite accept a
parameter marker, live-verified by the sweep). Don't add a second, `object`-
typed `StringAgg` separator overload to reach PostgreSQL's bindable case: the
two overloads would pick their emission mode (inline literal vs. bind marker)
silently off the argument's static type, which is exactly the kind of trap
this convention exists to avoid — if PostgreSQL's case is ever needed, name it
distinctly instead of overloading.

## Minimum-arity enforcement: compile-time when the shape allows it

A factory requiring at least one argument enforces it two ways, both
shipped: a **leading required parameter** ahead of the `params` tail
(`Coalesce(primary, secondary, params object[])`, `Cube(element, params
object[])`, `Match(column, params object[])`) makes the empty call a compile
error; a **runtime eager guard** on a bare `params object[]` (`Array(...)`,
`Unnest(...)`, `Greatest(...)`, `Least(...)`, `DistinctOn(...)`) throws at the
call site per `guards-and-empty-states.md`. Prefer the leading-parameter shape
when the construct has a natural "first" argument to split out; use the
runtime guard when every argument plays the same role and splitting one out
would misdescribe the construct (there is no privileged "first" array
element in `ARRAY[...]`).

## Factory return types: the concrete node type, not `SqlExpression`

A public `Sql.*` factory returns its own concrete node type (`Sql.Null` →
`NullExpression`, `Sql.Abs` → `AbsFunction`) — never the general
`SqlExpression` supertype, even for a factory whose whole point is that
callers hold the result in a variable. Upcasting there discards the
type-level self-documentation the factory exists to provide; a caller who
wants the generic supertype can still declare one locally.

> Worked example (#282): the issue text that specified `Sql.Bind(value)`
> proposed `SqlExpression Bind(object value)` as its return type — an
> inconsistency with this rule caught in review and corrected to return the
> pre-existing `BindValue` node directly.

## Root namespace vs `Internal` (ADR 0005)

A type belongs in the root `SqlArtisan` namespace only when **all three** hold:

1. It is a query's **content** — a relation, value, predicate, sort item, or
   handle — not clause syntax, statement decoration, or a pending intermediate.
2. A mainstream flow must **write its name** in a declaration position.
3. No root type already names it.

Everything else — concrete nodes, clause types, builder internals — belongs in
`Internal/` and is held only through the root types.

> Worked example (#282): fixing `Sql.Bind` to return `BindValue` (above) put
> that type through criterion 2 — its entire feature is a caller holding the
> result across clauses (`BindValue p10 = Bind(10);`) — so `BindValue` moved
> from `Internal/` to `src/SqlArtisan/SqlPart/Expression/`, beside `DbColumn`,
> and gained full XML docs once it left the `Internal/SqlPart/**` CS1591
> suppression. A factory's return-type fix can surface a namespace question
> for the type it returns; check both together.

## Opinions live in docs and the analyzer, not in API holes

Never omit a legitimate SQL spelling to steer users toward a "better" one —
the `COUNT(*)` lesson (#233, #232): knowledge encoded as API absence is
invisible, unexplained, and unsuppressible, and the adoption test is binary.
Emit faithfully; put the guidance in `docs/`; let the matrix warn where a
dialect genuinely rejects the construct.

## Recorded trade-offs from the #149 freeze audit

Reviewed and kept as-is — not defects, not scheduled for change:

- **`SqlArtisan.Dapper.SqlMapper` mirrors `Dapper.SqlMapper`'s simple name.**
  Both are static classes holding extension methods, and the mirroring is the
  point (same shape, `ISqlBuilder` in place of `(sql, param)`); the ambiguity
  only bites an explicit `SqlMapper.Member` reference with both namespaces
  imported (`CS0104`), which extension-method call syntax never triggers.
- **`SqlArtisan.Internal.EqualityBasedCondition`** is the element type of the
  `Set`/`DoUpdateSet`/`ThenUpdateSet`/`OnDuplicateKeyUpdate` assignment
  arrays; a dynamic patch-`UPDATE` (building the assignment list at runtime,
  the `SET`-side twin of the `ConditionIf` `WHERE` pattern) needs to name this
  type and currently can only do so via the `Internal` namespace. If this is
  promoted to the root namespace later (ADR 0005 criterion 2), name it for its
  *role* (e.g. `Assignment`), not its current mechanism-describing name.
- **`SqlParameters.Get<T>`** returns the type's default when no parameter has
  the given name, rather than throwing — the one intentionally quiet lookup
  in a library whose mission is loud failure, kept for the common
  "does this bind exist" check; documented on the member itself.
- **`DbmsResolver`'s first-registration-wins policy** for
  `RegisterProvider` (the nine built-in ADO.NET-provider-to-`Dbms` mappings
  register in the static constructor, before any user code runs, and can
  never be overridden) is documented on the member; `Build()` still fails
  loudly on an unresolved connection rather than guessing.
