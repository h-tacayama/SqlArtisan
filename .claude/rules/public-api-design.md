---
description: Public API design decisions — naming categories, overload split for analyzer arity, collection parameters, operator overloads, factory return types, no opinion-holes
paths:
  - "src/SqlArtisan/Sql/*.cs"
  - "src/SqlArtisan/SqlPart/**/*.cs"
  - "src/SqlArtisan/Internal/SqlBuilder/**/I*.cs"
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
   (expression) operators whose glyph has a canonical reading** — precedent
   `JsonArrow` (`->`) / `JsonArrowText` (`->>`) / `DoublePipe` (`||`, #234).
   A symbol operator that returns a *predicate* instead gets a semantic name:
   `ArrayContains` (`@>`), `ArrayContainedBy` (`<@`), `ArrayOverlaps` (`&&`),
   `JsonbContains` (`@>`), `JsonbExists`/`JsonbExistsAll`/`JsonbExistsAny`
   (`?`/`?&`/`?|`), `TsMatch` (`@@`) all took the semantic name, not a glyph.
   So does a value-returning operator whose glyph has no established reading:
   the six pgvector distance operators (#343) are `L2Distance` (`<->`),
   `CosineDistance` (`<=>`), `NegativeInnerProduct` (`<#>`), `L1Distance`
   (`<+>`), `HammingDistance` (`<~>`), `JaccardDistance` (`<%>`) — pgvector's
   own docs name them by distance, nobody says "less-hash-greater", and a
   glyph name would collide with unrelated same-glyph constructs elsewhere
   (MySQL's `<=>` is NULL-safe equality). None of the six collides with a
   common BCL static-utility type. When adding a new symbol operator, check
   which side it falls on before naming it.
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

**Recorded instance, a different constraint — `IntervalLiteral`:** the CLR
*could* overload it onto `Interval` (every overload differs by arity or
parameter type), but `Interval(object quantity, DateTimePart)` binds its value
while `IntervalLiteral(string, IntervalField)` inlines it. One name would let
the second argument's static type pick the emission mode silently — the trap
the separator section below forbids. The qualifier is also the standard's own
term (`<interval literal>`), not an invention (#554).

## Builder stage names: `I<Statement>Builder<State>`

A fluent-chain stage interface names the statement it builds, then what the
chain has written: `I` + `Select`/`Insert`/`InsertIgnore`/`Update`/`Delete`/
`Merge`/`With` + `Builder` + a state word. The bare `I<Statement>Builder` is
the entry that opens the statement (`ISelectBuilder`, `IDeleteBuilder`, …;
`IWithBuilder` opens a `WITH`). The state word names SQL, not C# methods:

- the clause just written (`ISelectBuilderWhere`; `IMergeBuilderTarget` after
  `MergeInto`; `IInsertIgnoreBuilderTable` / `IInsertIgnoreBuilderColumns`
  for the two `InsertIgnoreInto` arities; `IMergeBuilderWhere` after the
  update action's `Where`, with no `Update` qualifier because the insert
  action's `Where` has no stage of its own to tell it from, #587);
- the clause position several steps return to (`ISelectBuilderFrom` after
  `From`, `On`, `Using`, `CrossJoin`, …; `ISelectBuilderPaginated`;
  `IMergeBuilderWhen`);
- then the clause the stage offers or awaits, when that is what sets it apart
  (`IDeleteBuilderDeleteOutput` extends `IDeleteBuilderDelete` with `Output`;
  `ISelectBuilderLimitOffset` after `Limit`, `ISelectBuilderOffsetFetch` after
  `OffsetRows`; `IUpdateBuilderJoinOn` awaits `ON`).

A detour through an optional clause returns the stage without it:
`Output(...).Into(...)` returns `IDeleteBuilderDelete`, the base of the
`IDeleteBuilderDeleteOutput` it left, not a new stage. A
stage several statements share takes its capability's name
(`IReturningBuilder`, from `IReturning.Returning`). A capability composed into
stages carries no `Builder` (`IPagination`, `IForUpdate`, `IJoinOperator`,
`ISetOperator`, `IUpsert`, `IReturning`); `IReturning` also ends an upsert
action (`DoNothing()`, `DoUpdateSet(...).Where(...)`).

**A member that returns a capability shared across statements ends its
statement's chain.** `IReturning` and `ISqlBuilder` are shared by several
statements' stages, so a clause that may follow such a member in its own
statement can later be added only by offering it on every stage that shares
the capability, or by changing the member's return type (a binary break).
Before 1.0, check each such member against the engines' grammar for a clause
that can follow it, and either give the member its own stage or record the
continuation as not offered — #581's record under "Opinions…" is the worked
example. Checked (#582): `DoNothing()` and `DoUpdateSet(...).Where(...)`
(#581); `ForUpdate(...)`, given its own stage (#590), and `Into(...)` (under
"Opinions…");
`OnDuplicateKeyUpdate(...)`, after which MySQL's reference syntax for `INSERT`
lists no clause; and `Returning(...)` (under "Opinions…"), whose shared
stage offers `Into(...)` alone, while PostgreSQL's reference syntax ends the
statement at `RETURNING`.

The same check covers what a return type drops: a member whose stage stops
carrying a capability its receiver carries, such as `ISubquery`, removes that
position for every caller, or leaves an `object` overload to take it in another
form (`In(...)` read a locked query as a one-value list), and restoring it is
the same binary break (`ForUpdate(...)`, #590). Swept (#590): the other members that leave
`ISubquery` behind return a stage that still awaits a clause — a join's
`ON`/`USING` (`ISelectBuilderJoin`) or a set operator's right-hand query
(`ISelectBuilderSetOperator`) — which is no complete query to embed.

Why not name every stage after the method that returns it: several methods
land on one stage (`ISelectBuilderFrom`), and a method's name is not the
clause it writes (`MergeInto` writes the target, `OffsetRows` the `OFFSET`)
(#568). `BuilderStageNamingTests` pins the shape over every stage a public
member returns; the state word is review's call.

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

**Recorded scope — `Values(IEnumerable<object[]> rows)`:** the hazard needs a
single value that is itself a collection of the element type, and no value
SqlArtisan binds is an `IEnumerable<object[]>`. `IEnumerable` is what lets a
lazy sequence of rows pass straight through; typed `IReadOnlyCollection`, a
lazy sequence would fall to `params object[]` as one value and be rejected
at the call. `Values_LazyRowSequence_CorrectSql` pins the lazy path.

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

The trap is an `object` overload beside the `string` one. Overloads whose types
never overlap may split the emission: `JsonArrow(object, string)` and
`(object, int)` inline the key, and `(object, SqlExpression)` emits the
expression it is given — `Bind(key)` when a caller wants it bound (#557). With
no `object` overload, a key typed `object` does not compile instead of silently
binding. Watch implicit conversions, which overlap types the signatures do not:
a `char` converts to `int`, so `JsonArrow` carries a `char` overload that
inlines it as a key rather than letting it become an array index.

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

**Recorded exception — simple `Case(object expr, params SimpleCaseWhenClause[])`**
keeps the bare tail and its eager guard, although its searched sibling splits
out the first `WHEN`. The bare tail is what lets a `WHEN` list built at run
time pass straight through as an array for a `CASE` with no `ELSE`; splitting
out the first `WHEN` would turn `Case(expr, whenArray)` into a compile error
and remove that form. Only the literal `Case(expr)` is at stake, and it throws
at the call site. A split would cost that form and a binary break to catch a
misuse that already fails loudly on first run (#552).

## A token that takes arguments anywhere is a method, not a property

A parameterless property is right only for a token that takes no argument on
any supported dialect (`CurrentDate`, `ToHour`). If any dialect accepts an
argument — a precision, a count — the factory is a method, even while no
caller needs the argument yet: C# forbids a property and a method of the same
name on one type (CS0102), so a property can never grow the argument form
later without a major version, and the only additive escape would be an
invented name this file forbids.

**Shape: an overload pair, `X()` and `X(int precision)` — never one
`X(int? precision = null)`.** Three reasons, all measured (#553):

- An optional-parameter method group has a natural delegate type, so a caller
  who forgets the parentheses in an `object` position — `Select(X)`,
  `col == X`, `Values(..., X)` — compiles with only warning CS8974 and throws
  an opaque `Invalid type ... AnonymousDelegate` at run time. Two overloads
  give the group no natural type, so the same slip is error CS1503/CS0019.
- The pair gives the argument form its own declared arity, so the analyzer can
  key it separately (`("CurrentTimestamp", 1)`); one optional parameter hides
  it at arity 1 either way.
- Neither overload takes `null`: "no argument" is spelled by calling `X()`,
  not by passing `null` (`BindNull()` / `BindNull(DbType)` follows the same
  pair for the same reason).

`CurrentTimestamp()`/`CurrentTimestamp(int)`, `CurrentTime()`/`CurrentTime(int)`,
the interval fields (`Day()`/`Day(int)`, …, `ToSecond()`/`ToSecond(int)`) and
`BindNull()`/`BindNull(DbType)` are the shipped instances.

**Scope: `Sql.*` static factories only.** Public constructors keep optional
nullable metadata (`BindValue(object, DbType?, ParameterDirection?, int?)`,
`OutputParameter(string, DbType, int? size)`): a constructor is never a method
group, so the missing-parentheses slip cannot happen; the analyzer keys `Sql.*`
members, not constructors; and three independent optional parameters would
need eight overloads where named arguments read naturally. The difference
between `BindNull(DbType)` and `BindValue`'s `DbType?` is this boundary, not
an inconsistency.

## Operator overloads: operand orders that fall to a C# built-in

When no user-defined operator applies, C# falls back to its built-in ones.
`string + object` is built in, so while `+` took only `(SqlExpression, object)`
the call `"Dr. " + col` compiled as string concatenation through
`ToString()`: the text `"Dr. SqlArtisan.DbColumn"` was auto-parameterized, and
the query ran with wrong rows. `+` takes `(string, SqlExpression)` too for that
reason (#613). For every overloaded operator, enumerate the operand orders a
caller can write, and compile each one: a
literal, a nullable variable, and an `object`-, interface- or
type-parameter-typed operand on either side, compound assignment (`+=`), and
`null`. Each order must either bind a SqlArtisan operator or fail to compile
(CS0019), except the orders listed below as open. Adding the missing order
after 1.0 changes which rows code that already compiles reads, which is a
major version (`docs/versioning.md`). Which overloads close an order is
decided by compiling them against every neighbouring order, not by this file.

- The emitted SQL keeps the C# operator tree because compound operands are
  parenthesized (`sql-building-style.md` rule 6).
- An order with no SQL meaning can be closed as a compile error instead: an
  operator marked `[Obsolete(..., error: true)]` whose message names the
  remedy. A string beside an `ExpressionAlias`, in either order, is closed so
  (#613): `.As(...)` binds before `+`, so `"Dr. " + col.As("n")` adds the
  string to the alias, and no SqlArtisan operator takes an alias as an operand
  (`col + col.As("n")` throws).
- Open today, each an analyzer candidate in #614:
  - `+` between a `string` and an operand typed as neither `SqlExpression`
    (or a type derived from it) nor `ExpressionAlias`, in either order (and
    `+=` on a string): a subquery stage (`"x" + Select(...)`), a function
    still pending its clause (`RowNumber()` before `.Over(...)`, which a column
    on the left would reject instead) or an `object`-typed value still
    compiles as C# concatenation;
  - `==` / `!=` whose left operand is a reference type not derived from
    `SqlExpression`, wherever C# has a reference conversion between the two
    operand types (a type parameter counts as its effective base class):
    `object`, `SqlPart`, or a type parameter constrained to `class` or to a
    base of `SqlExpression`, against any right operand; an interface against
    a right operand whose static type is not sealed (`SqlExpression`, not
    `DbColumn`). These compile to C# reference equality and bind a `bool`;
  - interpolation (`$"{col}"`), which reaches `ToString()` without any
    operator.
- A `dynamic` operand is outside the requirement: it binds at run time to
  whatever order its runtime type forms, so an `object` compares references.

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

**Recorded placement — `BindArrayValue` stays in the root.** It adds no member
to `BindValue`, but it is the only way to tell an array bind from a scalar one:
`SqlParameters.ForEach` hands every binding over as `BindValue`, and an
execution layer must type-test for the array case — Dapper would otherwise
expand the array into an `IN` list, so `SqlArtisan.Dapper` does exactly this
(`SqlParametersExtensions.cs`). Any other execution layer written against
`SqlParameters` needs the same test, so criterion 3 fails for it: no root type
names the distinction (#556).

A **public** type in `Internal/` is public only because a signature hands it
back, so it exposes **no public constructor**, and it is declared `internal`
when no signature names it. Both are gated (`PublicSurfaceBoundaryTests`), along
with the namespace set the two of them key on. Two spellings publish a
constructor without anyone writing `public`, which is how 30 nodes became
`new`-able (#487) — 28 by a **primary constructor** on a concrete class, whose
accessibility follows the class, and 2 by declaring **no constructor at all**.
On an abstract class either spelling is `protected` instead — reachable by
deriving from another assembly, which is how three bases could be subclassed to
emit an arbitrary operator token (#492). So write the constructor out: `internal`
on a public concrete node, `private protected` on a public abstract base. An
internal node can keep its primary constructor.

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

The converse is ADR 0010's triage: an addition earns its surface by
strengthening a deterministic layer or by unblocking adoption, whose test is a
query that cannot be written. A typestate that withholds a step is therefore
not a hole when another chain already writes a statement with the same effect —
that is an ergonomic difference, and it does not earn new states or a changed
return type.

**Same effect** means the same rows — read, or changed — not the same SQL
text. A rewrite inside the API that keeps the rows makes the gap ergonomic
however different its text: a derived table around a FROM-less `Select`,
a derived table filtered by `WHERE` for a `HAVING` with no `GROUP BY`, an
added `ORDER BY`, a column list for a positional insert. A rewrite that can change the rows is not the
same effect, and the gap is a hole: moving a `LEFT JOIN LATERAL ... ON cond`
predicate into the body puts it before a `LIMIT` there, where `ON` applied it
after — the body keeps different rows. Measure a gap against its rewrite on
that test before calling it either. Two #521 entries were declined on that
reading, after measuring:

- `InsertInto(t).Set(...)` takes no `.Output(...)`. `Set(...)` renders the
  same `(cols) VALUES (...)` the column-list chain renders — byte-identical,
  pinned by `InsertInto_SetAndColumnListChains_EmitTheSameStatement` — so
  `InsertInto(t, cols).Output(...).Values(...)` already writes it.
- `InsertInto(t)`, with no column list, takes no `.Output(...)` either. SQL
  Server 2022 runs `INSERT INTO t OUTPUT ... VALUES (...)`
  (`OutputWithoutColumnList_IsAcceptedByTheEngine`), but a positional row
  fills the columns in declaration order, skipping identity and computed
  ones (`PositionalValues_SkipAnIdentityColumn`,
  `PositionalValues_SkipAComputedColumn`), so naming the columns it fills in
  `InsertInto(t, cols)` writes the same insert. A `rowversion` column is the
  exception: a positional row must still fill its slot, with `DEFAULT`
  (`PositionalValues_DoNotSkipARowversionColumn`,
  `PositionalValues_FillARowversionSlotWithDefault`), which SqlArtisan has no
  spelling for, while a column list leaves it out — so there too the
  column-list chain writes no less. Reaching the columnless form took three
  new builder states and a binary-breaking return type on `Sql.InsertInto`
  when it was built (#541, withdrawn unmerged).

`COUNT(*)` sat on the other side of that line: the library had no spelling
for it (#233).

**Not yet offered (#582):** the A3 review found these DML forms with no
spelling here. Each would be a new member, overload or factory, which
`docs/versioning.md` counts as a minor change, and one is added when a user
needs it (ADR 0010), so a later audit that finds one missing has found this
list, not a new hole:
`INSERT ... SELECT` followed by `RETURNING`, `ON CONFLICT` or
`ON DUPLICATE KEY UPDATE`; `MERGE ... OUTPUT` (SQL Server) and
`MERGE ... RETURNING` (PostgreSQL 17); an `ON CONFLICT` target with an index
predicate, an expression, or `ON CONSTRAINT`; `DEFAULT VALUES` and a `DEFAULT`
value; `INSERT IGNORE ... ON DUPLICATE KEY UPDATE`; SQLite's
`INSERT OR IGNORE` / `OR REPLACE` and MySQL's `REPLACE`; MySQL's
`UPDATE`/`DELETE ... ORDER BY [LIMIT]`, multi-target `DELETE` and
`UPDATE`/`DELETE IGNORE`; PostgreSQL's `OVERRIDING SYSTEM VALUE`; Oracle's
`INSERT ALL`/`INSERT FIRST`. Several ON CONFLICT clauses in one `INSERT` are
decided separately, below.

**Several `ON CONFLICT` clauses in one `INSERT` are not offered (decided —
do not re-file):** SQLite runs `ON CONFLICT (id) DO UPDATE ... ON CONFLICT
(code) DO NOTHING` (3.45.1, live) and PostgreSQL 16 rejects the second
clause, so the form is SQLite's alone.

- *The fluent chain.* `DoUpdateSet(...)` returns its own stage, so an
  `OnConflict` added there later is additive, and it can return new stages
  whose actions carry `OnConflict` again; only `DoNothing()` and
  `DoUpdateSet(...).Where(...)` on the first clause return the shared
  `IReturning`, which cannot take `OnConflict` without offering it on the
  `UPDATE` and `DELETE` stages that take `RETURNING` too, so they stay
  closed short of a return-type change. Added later,
  the chain reaches every sequence whose first clause is an unfiltered
  `DO UPDATE`. The written order matters only for a row that violates two of
  the targets at once, where the first clause wins (live), so a leading
  `DO NOTHING` or filtered `DO UPDATE` can usually move behind an unfiltered
  one; one that must win over a later clause, or a statement with no
  unfiltered `DO UPDATE` to lead, stays out of the chain's reach.
- *A held stage.* Calling `OnConflict` twice on a held stage compiles, and
  the once-per-block walk (`SqlBuilderBase.ThrowIfDuplicateClauseInBlock`)
  rejects the second clause at `Build()` on every dialect, SQLite included.
  That departs on purpose from ADR 0007's test, which leaves text valid on
  some dialect to the engine, and ADR 0011 decides it: the walk reads a
  once-per-block clause a held stage writes twice as a reuse slip, and
  emitting this one for SQLite would make that slip the only spelling of a
  form the chain does not offer.

Reserving a stage type now would be a binary break paid for a form no caller
has asked for (#581).

**What may follow `ForUpdate(...)`, `Into(...)` or `Returning(...)` is not
offered (decided — do not re-file):** `Into(...)` returns the shared
`ISqlBuilder` and `Returning(...)` the `IReturningBuilder` stage that
`INSERT`, `UPDATE` and `DELETE` share, so a clause an engine takes after them
can be added only by changing the return type, a binary break, or, after
`Returning(...)`, by offering it on all three statements (§ "Builder stage
names"). `ForUpdate(...)` returns its own `ISelectBuilderForUpdate`, which
also embeds the locked query as a subquery (#590), so a clause after it stays
an additive change, left to demand (ADR 0010).

- *A second locking clause.* PostgreSQL 16 runs `FOR UPDATE OF u NOWAIT FOR
  UPDATE OF o SKIP LOCKED`, a wait policy per table
  (`PostgreSqlTests.RepeatedLockingClause_IsAcceptedByTheEngine`).
  SqlArtisan offers one locking clause per query block and no `FOR SHARE`,
  and the walk rejects a held stage's second `ForUpdate` as it does the
  second `ON CONFLICT` above.
- *A row limit after the lock.* PostgreSQL 16 also runs `FOR UPDATE LIMIT 1`
  and `FOR UPDATE OFFSET 1 ROWS FETCH FIRST 1 ROWS ONLY`
  (`PostgreSqlTests.RowLimitAfterLock_IsAcceptedByTheEngine`), an order MySQL
  rejects (`MySqlTests.ForUpdateBeforeLimit_IsRejectedByTheEngine`). The
  chain offers the row limit before `ForUpdate` only (#520).
- *SQLite's `ORDER BY` / `LIMIT` after `RETURNING`.* SQLite's grammar
  (`src/parse.y`) puts them after `RETURNING` on `UPDATE` and `DELETE`, in
  builds with `SQLITE_ENABLE_UPDATE_DELETE_LIMIT`; the bundled build lacks
  the option and rejects them
  (`SqliteTests.RowLimitAfterReturning_IsRejectedByTheEngine`).
- *MySQL's `INTO`.* MySQL 8.0 runs `SELECT ... FOR UPDATE INTO @id`
  (`MySqlTests.IntoAfterLock_IsAcceptedByTheEngine`). SqlArtisan offers no
  `SELECT ... INTO`; added later, it can reach a query ending in
  `ForUpdate(...)` through that stage.
- *Oracle's `LOG ERRORS`.* Oracle runs an `error_logging_clause` after
  `RETURNING ... INTO`
  (`OracleTests.LogErrorsAfterReturningInto_IsAcceptedByTheEngine`).
  SqlArtisan offers `LOG ERRORS` on no statement; added later to the DML
  stages, it would not reach a statement ending in `RETURNING ... INTO`.

As with #581, a stage type reserved now would be a binary break paid for
forms no caller has asked for (#582).

**SQL Server's `OUTPUT ... INTO` beside a plain `OUTPUT` is not offered
(decided — do not re-file):** T-SQL takes one of each in a statement
(`SqlServerTests.OutputAfterOutputInto_IsAcceptedByTheEngine`), but
`Output(...).Into(...)` returns a stage without `Output`, and a held stage's
second `Output` is a reuse slip the walk rejects (ADR 0011). Whether offering
the pair later needs a return-type change is not settled here; it is weighed
when a user needs it (ADR 0010).

## Recorded trade-offs from the #149 freeze audit

Resolved:

- **`EqualityCondition`** (was `EqualityBasedCondition`) — promoted from
  `Internal/` to the root namespace (#488) and renamed there (#497): all three
  ADR 0005 criteria hold, and CS0216 makes the type permanent (the `==`/`!=`
  pair must share a return type). `Based` named the inheritance mechanism, the
  one thing a caller cannot see; the `=` node whose name it was avoiding became
  `EqualCondition`, which is the `Equal` its `GreaterThanOrEqual` siblings
  already spell and which keeps condition-class-to-`Operators`-member names
  1:1. It is still a *condition*, not an assignment — `!=` returns it too, and
  a `SET` list is what narrows it back down.

Reviewed and kept as-is — not defects, not scheduled for change:

- **`SqlArtisan.Dapper.SqlMapper` mirrors `Dapper.SqlMapper`'s simple name.**
  Both are static classes holding extension methods, and the mirroring is the
  point (same shape, `ISqlBuilder` in place of `(sql, param)`); the ambiguity
  only bites an explicit `SqlMapper.Member` reference with both namespaces
  imported (`CS0104`), which extension-method call syntax never triggers.
- **`SqlParameters.Get<T>`** returns the type's default when no parameter has
  the given name, rather than throwing. It reads a built statement's bound
  values, which no SQL depends on, so the loud-failure mission does not reach
  it. It is not an existence check: for a value type `T`, a missing name and a
  bound default read the same, so existence is tested with `ParameterNames`.
  Documented on the member itself.
- **`DbmsResolver`'s first-registration-wins policy** for
  `RegisterProvider` (the nine built-in ADO.NET-provider-to-`Dbms` mappings
  register in the static constructor, before any user code runs, and can
  never be overridden) is documented on the member; `Build()` still fails
  loudly on an unresolved connection rather than guessing.
