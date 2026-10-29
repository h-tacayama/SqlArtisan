---
description: Guard conventions — the enforcement boundary, empty-state policy, eager vs Build()-time timing, exception message grammar
paths:
  - "src/SqlArtisan/Internal/SqlBuilder/**/*.cs"
  - "src/SqlArtisan/Internal/SqlPart/**/*.cs"
  - "src/SqlArtisan/Sql/*.cs"
  - "src/SqlArtisan/SqlBuilder/**/*.cs"
  - "src/SqlArtisan/SqlPart/**/*.cs"
  - "src/SqlArtisan.TableClassGen/**/*.cs"
  - "src/SqlArtisan.Dapper/**/*.cs"
  - "src/SqlArtisan.ArrayBind/**/*.cs"
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
| **Bounded exception** — a complete construct valid on some dialect but structurally invisible to the analyzer *and* with no valid spelling on the resolved target | **Reject at Build(Dbms)** | `Validate(Dbms)` hook on `SqlBuilderBase`, run once per query block; dialect-scoped, position-scoped | The analyzer cannot see it (value-level, not construct-level) *and* the resolved target has no valid spelling — both conditions required |
| **Dialect availability** — a complete construct that some engine does not support | **Emit faithfully** (ADR 0001); surfaced by the opt-in analyzer (ADR 0003) and ultimately the database | Permissive | Valid on at least one supported dialect |

The dividing tests read the text as the construct the caller wrote. A
same-named function of another engine that reads the text as a different
function does not make the construct valid there: `Decode(x, [], d)` would
emit `DECODE(x, :0)`, which PostgreSQL 16.15 runs as its binary
`decode(text, format)`, so the empty-pairs guard below still rejects it
(#396, #661).

**Enumerated instances of each rejection category:**

- *Incomplete*: window/analytic function without `.Over(...)` (#150); ordered-set
  aggregate without `.WithinGroup(...)` (#190) — `Listagg`'s pending type rests on
  a same-rows ground instead, since Oracle XE 21.3.0 runs the bare call (ADR 0007,
  #664); an `ANY` / `ALL` / `SOME` operand off a comparison's right side, and
  MySQL's `INTERVAL n unit` off a date's `+` / `-` and `DATE_ADD` / `DATE_SUB` —
  each half of a construct, which completes only that slot (`SELECT ANY (...)` is
  rejected on all five baseline lanes,
  `QuantifiedOperand_OutsideAComparison_IsRejectedByTheEngine`, and a bare
  `INTERVAL` on MySQL 8.0,
  `MySqlTests.ContextRule_IntervalOutsideArithmetic_Rejected`); the three types are
  no `SqlExpression`, and only the slot each completes wraps one. A `LIKE` with
  `ESCAPE` is no such slot (PostgreSQL 16.15: `LIKE ANY (...) ESCAPE` is a syntax
  error; the same twins), so `.Escape(...)` rejects a quantified pattern. On PostgreSQL 16.15
  `COUNT(ALL (x))`, `SELECT ALL (...)` and `GROUP BY ALL (...)` run with `ALL` read
  as the set quantifier, another construct (the `Decode` line above), so they throw
  too (#666); any of `InnerJoin`/`LeftJoin`/
  `RightJoin`/`FullJoin`/`JoinLateral` with no `.On(...)`/`.Using(...)` —
  accepted on SQLite (all five) or MySQL (`InnerJoin`/`JoinLateral` only) as an
  unlabeled `CROSS JOIN` spelling, not a construct with independent meaning
  (ADR 0017); `Output(...)` (SQL Server) combined with `Returning(...)`, with
  `Using(...)` on `DELETE`, with `OnConflict(...)`/`OnDuplicateKeyUpdate(...)`
  on `INSERT`, with a `WITH` between `INSERT INTO` and its `SELECT` (SQL Server
  2022 takes one only ahead of `INSERT INTO`, `ContextRule_InsertSelectWith_Rejected`,
  and with `OUTPUT` too, `OutputBesideInsertSelectWith_IsRejectedByTheEngine`),
  with an `UPDATE` that joins before `SET` (MySQL's form), or with an
  `UPDATE ... FROM` that does not re-list its target (the form SQL Server
  refuses) — no dialect accepts both halves of any pairing (#400, #679); an
  alias on the `OUTPUT ... INTO` destination, which SQL Server, the one engine
  with `OUTPUT`, rejects (SQL Server 2022,
  `OutputInto_AliasedDestination_IsRejectedByTheEngine`; #679);
  `Returning(...).Into(...)` (Oracle's)
  beside `OnConflict(...)`, which Oracle lacks, or after a leading `WITH`, which
  Oracle takes on no DML statement, and Oracle's MERGE action `WHERE` /
  `DELETE WHERE` after a leading `WITH` too, since the engines with one refuse
  an action `WHERE`, and beside a `WHEN ... AND`, `THEN DELETE` or `BY SOURCE`
  branch anywhere in the same `MERGE`, which Oracle refuses (#679); `TOP ... WITH TIES` with no
  `ORDER BY` of its own query block, which a set-operator branch never has,
  since a compound's `ORDER BY` sorts the combined result (SQL Server 2022,
  `TopWithTiesInCompoundBranch_IsRejectedByTheEngine`; #569, #679);
  `Returning(...)` on a joined `DELETE ... FROM` or `UPDATE` that re-lists its
  target, or a direct-join `UPDATE` — those forms belong to MySQL and SQL
  Server, which have no `RETURNING`, and the engines with it reject the forms
  (#582; Oracle 23.26.3 rejects the re-listed `DELETE` lead,
  `Oracle23aiTests.JoinedDeleteLead_IsRejectedByTheEngine`); the typestate
  withholds it on a `DELETE ... FROM` and a direct join, and `Build()` backstops
  a held stage and the re-listed form, which instance identity decides. A
  `DELETE ... FROM` that does not re-list its target runs with
  `RETURNING ... INTO` on Oracle 23.26.3, so a held stage reaches it there
  (`JoinedDmlNotRelisted_ReturningInto_BindsOutputParameter`; the typestate's
  `Using(...)` route runs the same statement, #679); an `UPDATE`
  joining before `SET` and through `FROM` too — the typestate keeps the
  spellings apart, so only a held stage reaches both (#582); Oracle's MERGE
  action `WHERE` / `DELETE WHERE` in a conditioned (`WHEN ... AND`) or
  `WHEN NOT MATCHED BY SOURCE` branch — Oracle, the one engine with the
  filters, has neither branch form, and PostgreSQL and SQL Server reject an
  action `WHERE` (#582); the typestate withholds them there, and `Build()`
  backstops a held stage that lands one in such a branch; an
  `OnConflict()` with no conflict target paired with `.DoUpdateSet(...)` on
  PostgreSQL — SQLite takes the targetless form, so the guard is `Build(Dbms)`
  and `Dbms.PostgreSql`-scoped (ADR 0011; the earlier dialect-blind guard
  rejected SQLite's valid statement, and was corrected); an `OnConflict(...)`
  with no `DO NOTHING` / `DO UPDATE SET` — only a held stage builds before the
  action, so `Build()` backstops it on every dialect, as it does a MERGE `WHEN`
  with no action and a join with no `ON` (PostgreSQL 16.15 and SQLite 3.50.4
  reject the text, `OnConflict_WithoutAction_IsRejectedByTheEngine`; #675);
  `Returning(...)` beside `INSERT IGNORE` or
  `OnDuplicateKeyUpdate(...)` — MySQL, the only engine with either, has no
  `RETURNING`, so the typestate withholds it and `Build()` backstops a held
  stage (release audit pass 4); a leading `WITH` before `INSERT IGNORE` or an
  `INSERT` with `OnDuplicateKeyUpdate(...)` — the same MySQL-alone shape against
  MySQL's `INSERT`, which takes no leading `WITH`; the `WITH` states open no
  `InsertIgnoreInto` (ADR 0007's compile-time order), and `Build()` rejects ODKU
  on every target: its stages are shared with the `WITH`-less chain, and
  withholding it would take a `WITH`-led copy of the six `INSERT` stages (as
  `IInsertIgnoreBuilder*` is for `INSERT IGNORE`), a cost the guard avoids (#569); a fixed column list naming one column twice
  — an `INSERT` column list, a `SET` assignment list (the token the target
  *renders as*, never its owner: `MergeUpdateSetClause`, `DoUpdateSetClause`,
  `InsertSetClause` and `OnDuplicateKeyUpdateClause` always render the target
  unqualified, so two handles — or two different tables — sharing a column
  name are one token and are rejected eagerly; `UpdateSetClause` is the fifth
  case because `DmlJoinState.QualifiesSetTarget` re-qualifies it for the
  joined form, so that arm compares `alias.column` at `Build()`, where the
  shape is final — a joined multi-table `UPDATE` with distinct correlation
  names stays legal — after rejecting at `Set(...)` the pairs that share one,
  or that are both unaliased, which render one token either way), a `WITH` clause's CTE names, a `Values(...)`
  source's column names, an `UNNEST` column alias list (PostgreSQL 16.15 runs
  `unnest(...) u (x, x)`; only a later `u.x` is ambiguous, #664), an
  `ON CONFLICT` target, a join `USING` list, an `OUTPUT ... INTO` list — and
  an `OUTPUT ... INTO` list whose width differs from the `OUTPUT` list; a `SET` target another table owns, in an arm that
  renders it bare, and a column list entry another table owns (the paragraph
  below, #674); an `OutputParameter` name that is digits only, the positional
  bind markers' namespace (release audit pass 5), or is not shaped as an Oracle
  nonquoted identifier (a letter, then letters, digits, `_`, `$` or `#`), since
  its marker ends at any other character (`"0 "` rendered `INTO :0 ` beside a
  positional `:0`, #679); a `RETURNING ... INTO` variable list of another width
  than its `RETURNING` list; one naming a variable twice, since the parameters
  are keyed by name; and an unnamed variable, which `default(OutputParameter)`
  reaches past the constructor (#679).
  These are call-site defects rejected whatever the engine does (ADR 0011
  decides the class outside its bar) — eagerly, the `OUTPUT ... INTO` width
  at `Into(...)`, but for the joined-`UPDATE` arm's cross-name pairs above and
  every `UPDATE`'s `SET` owner check (#674), which wait for `Build()` — not ADR 0012 domain guards: only a duplicated CTE name is rejected everywhere.
  Live-verified (PostgreSQL 16, MySQL 8.0, SQLite 3.45): PostgreSQL rejects a
  duplicated `INSERT` list, `SET` list, and `USING` list but accepts a
  duplicated CTE/derived column list; MySQL rejects the `INSERT` and CTE
  column lists but accepts a duplicated `SET` or `USING` list (the last
  assignment wins); SQLite accepts every shape but a duplicated CTE name and a
  duplicated `ON CONFLICT` target, which it reads as matching no constraint. The
  `Duplicate*_Is{Accepted,Rejected}ByTheEngine` twins on those three lanes
  pin each cell; Oracle and SQL Server are not claimed (release audit pass 5,
  premise corrected after pass 8); an `INSERT ... SELECT`
  chain embedded as a subquery — no dialect takes an `INSERT` in a value or
  table position, though a CTE body stays permissive (PostgreSQL's
  data-modifying `WITH`), rejected at the call that embeds it, which
  `SqlBuilderBase.Composed` sees (#679); `Returning(expr.As(...)).Into(...)` — the output
  parameter names the value, so the alias is a contradiction, while the
  plain `RETURNING expr "alias"` is valid on PostgreSQL and SQLite and emits
  faithfully (release audit pass 5, reversing an earlier dialect-blind eager
  ban — ADR 0007's guardrail); a zero `ORDER BY` column ordinal
  (`OrderBy(0)`) — ordinals are 1-based wherever an engine resolves one, so
  `0` is no column position on any of them (live-verified on PostgreSQL 16
  and SQLite 3.50.4, and on MySQL 8.0 on its lane). That guard throws at the
  `OrderBy(...)` call and its dialect-scoped negative-ordinal sibling below at
  `Build()`, both for the statement position of every query block — a nested
  `SELECT` and a CTE body resolve their own ordinals — while `OVER (...)`,
  `WITHIN GROUP` and the inline aggregate orderings reject every number at the
  call (the bound-value paragraph below, #693).
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
  precision — 0..9 on `Year` to `Minute`, non-negative on `ToSecond`, which
  PostgreSQL 16.15 runs above 9, clamped to 6 (#661) — and its field range — one of the seven pairings Oracle's
  grammar admits, with a trailing precision only on `SECOND`, that position
  being the fractional-seconds count. A *sole* field's precision stays
  unguarded even for `SECOND` (the digits read as Oracle's leading precision
  there, so the text is valid and condition 1 fails) (#436); `Numtoyminterval`/
  `Numtodsinterval`'s `interval_unit` — restricted to the exact set each
  Oracle function accepts (`YEAR`/`MONTH`, `DAY`/`HOUR`/`MINUTE`/`SECOND`),
  since no other engine has either function (#448); `Wait(seconds)`'s second
  count — non-negative, the only `FOR UPDATE WAIT` domain Oracle (the sole
  engine with the clause) parses, live-verified at 21c and 23ai (#483); a
  whitespace-only string in a bare-token position — the `CAST` target type,
  the `NEXT VALUE FOR` sequence name, a `DbColumn` name — invalid on every
  dialect, while a quoted or literal position keeps accepting whitespace
  (an `IntervalLiteral` value aside, #669; the bare-token boundary below, gated by
  the factory sweep's whitespace injection; release audit pass 2); a cast-in
  undefined `DateTimePart` value
  (`ArgumentOutOfRangeException` at the factory call, naming the construct
  and the caller's parameter, not an index error at `Build()`; #661); a
  `CurrentTimestamp` / `CurrentTime`
  precision below 0, rejected by MySQL 8.0 and PostgreSQL 16.15 and, for
  `CURRENT_TIMESTAMP`, Oracle XE 21.3.0 (SQL Server and SQLite take no
  precision); the ceiling stays each engine's, since PostgreSQL 16.15 runs
  `CURRENT_TIMESTAMP(10)`, clamping it to 6 (#661).
- *Bounded exception*: aliased un-joined `INSERT`/`UPDATE`/`DELETE` target on
  SQL Server (on the joined forms the library requires the alias instead —
  next paragraph);
  aliased `INSERT` target on MySQL (its INSERT grammar has no target-alias
  slot); a joined `UPDATE` on SQL Server whose target is not re-listed in `FROM`
  (T-SQL's joined spelling takes the alias from `FROM`); a joined `DELETE ...
  FROM` whose target is not re-listed, off Oracle — the lead keeps
  `DELETE FROM target` before a second `FROM`, which Oracle 23.26.3 runs and XE
  21.3.0 and the other engines reject with the joined target's alias
  (`DeleteFromNotRelisted_*`, and `ContextRule_JoinedDeleteFromNotRelisted_Rejected`
  on XE 21.3.0; #679); a target re-listed through a join
  counts, on both statements, as one re-listed in `From(...)`; a joined
  `UPDATE` off SQL Server whose target IS re-listed in `FROM` — the mirror:
  that form's bare-alias lead is T-SQL's alone; a non-integer constant
  `ORDER BY` sort key on PostgreSQL and SQL Server (`OrderBy(2.5)`) — MySQL,
  SQLite and Oracle accept the no-op ordering, those two reject it, and the
  value is invisible to the analyzer; a negative constant `ORDER BY` ordinal
  on PostgreSQL, SQLite and SQL Server (`OrderBy(-1)`) — those three read it
  as a column position and reject it, MySQL and Oracle read it as a constant
  and accept it; `DeleteFrom(t).Using(...)` on SQL Server — T-SQL has no
  `DELETE ... USING` at all, and the message names the `From(...)` remedy
  the joined-target guard's own message could not reach from that chain
  (ADR 0011, "Later instances" section).

**Joined-target alias requirement (decided — do not re-file):**
`ThrowIfJoinedTargetUnaliased` fires for every joined `UPDATE`/`DELETE` shape
on **every** dialect, whatever an engine takes unaliased, at the `From` /
join / `Using` call that joins the target (#614). This is a
deliberate uniform requirement (#258, reaffirmed in the release audit after
independent reviews split on it; ADR 0011 decides it outside its bar): a
column is qualified only by its owner's correlation name (`DbColumn.Format`),
so an unaliased target's columns would render bare beside the joined
tables', and one dialect-independent rule keeps every target reference
qualified (a joined table's own alias stays the caller's choice). The guard is loud and the aliased spelling runs on each lane with
a joined-DML twin (`Joined*_Executes` on MySQL, PostgreSQL, SQLite and SQL
Server), so an engine that accepts an unaliased joined target is not an
over-guard finding at any tier.

**Which clause pairs no dialect runs get a guard (decided — do not re-file):** a
pair of statement clauses no dialect accepts together is rejected when both
halves spell one slot (`TOP` beside `LIMIT` / `OFFSET` / `FETCH`, save a compound's
`OFFSET ROWS` / `FETCH`, which SQL Server 2022 runs beside a branch's `TOP`, #669), or when one
half is a DML clause or statement form only some engines take and those engines
refuse the other half: another DML clause or form, or a clause they take in a
`SELECT` but refuse in that position (a `WITH` between `INSERT INTO` and its
`SELECT`, a leading `WITH` on Oracle). The owners are SQL Server's `OUTPUT` and
re-listed `UPDATE` target, Oracle's `RETURNING ... INTO` and MERGE action
filters, MySQL's `INSERT IGNORE`, `ON DUPLICATE KEY UPDATE` and direct-join
`UPDATE`, and the re-listed `DELETE ... FROM` MySQL and SQL Server share; the
*Incomplete* list above enumerates the pairs, and the walk's slot exclusivity (§
"The empty-state policy", `SqlBuilderBase`) holds the two that also spell one
slot, `ON DUPLICATE KEY UPDATE` beside `ON CONFLICT` and a joined `DELETE ...
FROM` beside `USING`; `INSERT IGNORE` beside `OUTPUT` or `ON CONFLICT` is held
by type alone, since the `IInsertIgnoreBuilder*` stages open from their own
entry point and offer neither `Output` nor `IUpsert`, so only a cast reaches the
pair (ADR 0011). Any other pair is left to the analyzer and the engine: a
`SELECT` clause an owner refuses in every position (`OUTPUT` beside a leading
`WITH RECURSIVE` or a feeding `SELECT`'s `LIMIT`), and the dozens of `SELECT`
pairs that join one engine's clause to another's (`TOP` with `FOR UPDATE` or
`NATURAL JOIN`, `CROSS APPLY` with `DISTINCT ON`). Each half there is a
dialect-matrix cell, so with the analyzer on, `SQLA0100` reports at least one
half on whatever dialect is configured, and the engine refuses the text or, as
SQL Server does with `MINUS`, reads a half as something else its own cell
already reports; a guard per pair would copy the matrix into the core. A guard
keyed on a `WITH` position takes `WITH RECURSIVE` there with it, though SQL
Server and Oracle refuse that clause everywhere: the position, not the keyword,
is what the guard reads. An expression in a position no engine allows is the
engine's for the same reason (the window-function paragraph below). This narrows
#684's "a pair no engine runs is a type or `Build()` guard" (#679). Oracle
23.26.3 runs `UPDATE ... FROM`, `DELETE ... USING` and a `DELETE ... FROM` that
does not re-list its target beside `RETURNING ... INTO`
(`Oracle23aiTests.JoinedDmlNotRelisted_ReturningInto_BindsOutputParameter`), so
those stay unguarded (#679).

**A `RegexpOptions` match parameter is never domain-checked (decided — do not
re-file):** no letter the enum emits is universally invalid, and a
contradictory pair is accepted on Oracle XE 21.3.0, PostgreSQL 16.13 and
MySQL 8.0 alike, each applying the last letter — so `CaseSensitive |
CaseInsensitive` is a meaningful value, not a mistake to reject. The letters
emit in enum order, so that pair is always `'ci'` and always resolves
case-insensitively, which is what the two members now document. MySQL's
`match_type` has no `'x'`, which is a per-value dialect gap for an
`SQLA0104`-class table to carry, never an ADR 0012 guard: its alphabet is
open, so condition 3 fails as well (#523). An undefined bit is not a letter:
it emits nothing today and would emit a new letter once a flag is appended at
that bit, so it throws `ArgumentOutOfRangeException` like an undefined
`DateTimePart` (#614).

**A `DateSub` interval literal MySQL cannot spell stays unguarded (decided —
do not re-file):** `DateSub(date, IntervalLiteral(...))` with no field or with
a field range emits `DATE_SUB(d, INTERVAL '3')` or `INTERVAL '1 2' DAY TO HOUR`,
which MySQL 8.0, the one engine with `DATE_SUB`, rejects as a syntax error
(`MySqlTests.DateSub_IntervalLiteralWithoutAUnit_IsRejectedByTheEngine`). That
is valid nowhere today, but ADR 0012 condition 3 fails: whether the text runs
rests on which engines carry a `date_sub`, an open list (PostgreSQL 16.15 has
`date_add` and `date_subtract`, and no `date_sub`). The failure is loud, and `SQLA0100` already
reports both shapes on MySQL through `IntervalLiteral`'s arity-1 and arity-3
rows, so the database and the analyzer are the arbiters (#661).

**A window function or a multi-array `UNNEST` in a position no engine allows
stays unguarded (decided — do not re-file):** `Where(RowNumber().Over(...) == 1)`
builds `WHERE ROW_NUMBER() OVER (...) = :0`, which PostgreSQL 16.15 rejects
(`window functions are not allowed in WHERE`) and SQLite 3.45.1 too (`misuse of
window function`), and so for `GROUP BY`, `HAVING`, `ON`, `SET` and a window
function nested in another or in an aggregate; `Select(Unnest(a, b))` builds
`SELECT UNNEST(a, b)`, which PostgreSQL, the one engine with `UNNEST`, takes only
in `FROM` (`function unnest(integer[], integer[]) does not exist`). Valid nowhere,
yet no `Build()` guard: these are complete constructs in a rejected position, the
`SQLA0102` class, not ADR 0007's incomplete one, and every engine fails loudly. A
check would walk the tree to each subquery boundary, where the same call is
valid, and withholding the multi-array form by type would cost the run-time array
shape § "Minimum-arity enforcement" keeps (`public-api-design.md`, #552). The
database is the arbiter (#664; the twins `WindowFunctionInWhere_IsRejectedByTheEngine`
on PostgreSQL and SQLite, `Unnest_SeveralArraysInSelectList_IsRejectedByTheEngine`).

**Five shapes the engines that run them reject stay unguarded (decided — do not
re-file):** a `RANGE` offset over two or more `ORDER BY` keys; a percentile
`WithinGroup(...)` over two or more keys; `WithRollup()` after a `GROUP BY`
holding a grouping element; a lateral or apply join given a derived table that
carries its own source, which it then emits twice; and an `INSERT ... SELECT` CTE
body given `.WithColumnList()` or read through a reference, since it has no
columns to name or read (with neither, the body runs, write-only). Each fails loudly where it was run — PostgreSQL 16.15 for
all but `WithRollup()` and the apply joins, SQLite 3.50.4 for the frame, SQL
Server 2022 for `WithRollup()`, the one dialect the matrix gives both halves, and
SQL Server 2022 and Oracle XE 21.3.0 for `CrossApply` — so there is no silent
wrongness to convert. None is a construct missing a part, which a held stage's
backstop takes (the *Incomplete* list): each is complete, in a shape its engines
refuse. A twin per shape asserts the rejection beside a control that runs (the
`*_IsRejectedByTheEngine` twins #675 added). The other engines are not run for
these: one that runs a shape, with any meaning, reopens it (#675's CI items).

**An `OUTPUT` item reading the target bare, and a `*` before `INTO`, stay
unguarded (decided — do not re-file):** on an `INSERT`, `UPDATE` or `DELETE`
that joins nothing, SQL Server 2022 rejects an `OUTPUT` item that reads the
target other than through `Inserted(...)` / `Deleted(...)` — a bare column (run
on `UPDATE` and `INSERT`), `t.*` or `*` (both on all three) — and Oracle XE
21.3.0 rejects `*` and `t.*` in `RETURNING ... INTO` on all three, even with one
output parameter per column, a width `Into(...)` refuses for one `Returning(Asterisk)`
item (`Output_*_IsRejectedByTheEngine`, `ReturningInto_Asterisk_IsRejectedByTheEngine`;
#669, #675). Each is complete and fails loudly, the paragraph above's ground. The
joined forms, whose `OUTPUT` reads a `FROM` table by name, and SQL Server 2025 and
Oracle 23ai are not run.

**A `GROUP BY` column ordinal stays permissive on the engines that do not read
it (decided — do not re-file):** SQL Server 2022 rejects a bare constant there
and Oracle XE 21.3.0 and 23.26.3 group by it as one constant, while MySQL
8.0.46, PostgreSQL 16.15 and SQLite 3.50.4 group by the position, so
`Build(Dbms)` could throw the way `OrderBy`'s non-integer sort key does — and
deliberately does not. ADR 0011's bar is an analyzer blind spot **and** no valid
spelling, and both halves fail: a call-site constant is exactly what `SQLA0104`
reads, and on those two engines `GroupBy(column)` spells the same intent. A
*non-constant* ordinal is a blind spot, but the valid spelling is still there,
so the second condition fails for it too and the database stays the arbiter.
The ordinal **below 1** is a different question and stays guarded on every
dialect: it names no select-list position, so an engine that reads one reads
another meaning (MySQL 8.0.46 groups `-1`, and Oracle XE 21.3.0 and 23.26.3
`0` and `-1`, as one constant group, #684), and PostgreSQL 16.15, SQLite
3.50.4 and SQL Server 2022 reject it. ADR 0012's condition 1 fails, so it is a
call-site defect ADR 0011 decides outside its bar (#521, #679).

**A negative row count and a negative `Lag`/`Lead` offset stay permissive
(decided — do not re-file):** `Top(-1)`, `FetchFirst(-1)` and `Limit(-1)`
carry the count as a `BindValue`, so it reaches the engine as a bind
parameter and never as statement text — ADR 0012 condition 2 excludes it, and
Oracle XE 21.3.0 accepts a negative `FETCH`/`OFFSET` outright, so condition 1
fails too. `Lag(x, -1)` *is* printed into the text, but PostgreSQL 16.13 and
SQLite 3.50.4 read it as the mirror function and accept it, so condition 1
fails there as well — and on the engines that reject it (Oracle, SQL Server
2022, MySQL 8.0) the mirror function is a valid spelling, which is why ADR
0011's second condition fails too. Both are enumerated non-goals in ADR 0012
and pinned by `Top_NegativeCount_BindsTheCountRatherThanPrintingIt`,
`FetchFirst_NegativeCount_BindsTheCountRatherThanPrintingIt` and the
`{Lag,Lead}_NegativeOffset_CorrectSql` pair. The `Lag`/`Lead` offset has a
live twin on all five lanes; the row count has one on all five too — a
rejection on MySQL, PostgreSQL and SQL Server, an acceptance on Oracle and
on SQLite, whose `LIMIT -1` means "no limit" (#523). The **negative `OFFSET`**
is the same call and a further step: `SQLA0104` reports the row counts but
deliberately not the offsets (#532), because `Build(Dbms)` cannot see a bound
value and the analyzer sees only a call-site constant — which a *negative*
offset never is, since one goes negative by arithmetic rather than by being
typed. All five engines are pinned as twins so the facts need not be
re-derived; the decision is recorded in ADR 0022's scope section.

**MERGE `WHEN` branch arity is the analyzer's, not a guard (decided — do not
re-file):** each engine bounds the branches differently — Oracle takes one per
WHEN clause (ORA-00905 on XE 21.3.0), SQL Server one per
clause-and-action pair *and* refuses any branch following an unconditional one
of the same clause, and PostgreSQL 16.13 applies that second rule too
(`unreachable WHEN clause specified after unconditional WHEN clause`). Each
branch is one the engine takes, refused only after another, so the shape is
*context-bounded* and `SQLA0102` reports it (`CheckRepeatedMergeBranch`, #582)
wherever the chain shows both branches. A `Build(Dbms)` guard for it was
written, measured and withdrawn (#523, #525): unlike the guard mission's
targets it fails **loudly** on the engine, naming the exact branch — no silent
wrongness to convert — and it cost a measured +320 B/build on SQL Server and
+176 B on Oracle for a plain two-branch upsert, the commonest MERGE those two
engines have, against ADR 0006. The per-engine limits are documented in
`docs/query-statements.md` and pinned by the `MergeRepeated*` twins on the
Oracle, SQL Server and PostgreSQL lanes (`BY SOURCE` on PostgreSQL 17).

**A same-named relation under SQLite's `RETURNING` stays unguarded (decided —
do not re-file):** inside a subquery or CTE body whose FROM or JOIN exposes an
aliased target's table name, the target keeps `"u".col` instead of SQLite's
`users.col` spelling (#595). The table name would bind the inner relation
silently. The alias fails loudly (`no such column` on SQLite 3.50.4), with no
silent wrongness to convert, so no `Build(Sqlite)` guard is added. A scope exposing the alias
keeps `"u".col` too, but there it binds the inner relation as written and
runs. The scope check matches by name, unlike the correlated-DML guard's
instance identity, because the capture comes from the library's own rewrite
and from any relation so named. It is pinned by
`Returning_Sqlite_SameNamedInnerRelation_IsRejectedByTheEngine` on the SQLite
lane.

**A sort key emitted as a bare name: rejected where no alias is in scope,
documented in the statement `ORDER BY` (decided — do not re-file):** an
alias key renders its name, and a column of an unaliased table renders
bare, so the engine may resolve either to another column (#672). Inside
`OVER`, `WITHIN GROUP` and the inline aggregate orderings PostgreSQL and
SQLite read no select alias (live), so `Sql.OrderBy(...)` and
`PartitionBy(...).OrderBy(...)` reject an alias key at the call, on every
dialect, even where an engine runs the text (ADR 0011 decides the class
outside its bar). In a
statement `ORDER BY` an alias the block selects is the valid spelling, and
telling it from one the block does not select, or a bare column from a
same-named output alias, takes the block's select list at `Build()` and,
for a compound, the branches' (PostgreSQL reads the first branch's names,
SQLite any branch's); that rewrite was weighed against how
rarely the collision is written and left out. The statement position emits
as written, and `docs/query-statements.md` § ORDER BY carries the
`[!WARNING]` (live: PostgreSQL 16, SQLite 3.45.1).

**A bound value is no sort, group, partition or `DISTINCT ON` key, nor a number
an expression-position sort key (decided — do not re-file):** a
bound `string`, `char`, `bool`, date/time, GUID or enum value is a constant, which
sorts or groups nothing (`ORDER BY $1` bound to `'a'` or to an enum's integer
returned the rows unsorted on PostgreSQL 16 and SQLite 3.45.1, live), so
`OrderByItemResolver` and `GroupByItemResolver` reject one at the call, on every
dialect, as a call-site defect (ADR 0011 decides the class outside its bar;
#678). `Bind(value)` keeps a deliberate constant writable. The other key
positions follow (#673): a grouping element (`Rollup`, `Cube`, `Group`) and a
`PartitionBy(...)` key take an expression or a subquery and reject every value
(`ExpressionResolver.ResolveKey`), an integer included. `PARTITION BY` reads even
a literal integer as a constant (PostgreSQL 16 and SQLite, live); PostgreSQL 16
reads one in a grouping element as a position (live), but how Oracle and SQL
Server read it is not verified (MySQL and SQLite have no grouping element), so
it is not emitted as one there as it is in `GroupBy`. `GroupBy(...)` itself still
rejects a subquery: taking one directly is an addition, left to demand
(ADR 0010). A
subquery stays a grouping element (it runs on PostgreSQL 16, live).
`DistinctOn(...)` renders an integer as the select-list position PostgreSQL, its
one engine, reads there, rejecting one below 1 at the call as `GroupBy` does
(both live), and rejects every other value. Inside `OVER`, `WITHIN GROUP` or an
aggregate a number in an `ORDER BY` is a constant key, as in `PARTITION BY`
(`ROW_NUMBER() OVER (ORDER BY 1)` numbers
the rows in input order on PostgreSQL 16 and SQLite 3.45.1, and
`PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY 1)` returns `1` on PostgreSQL 16,
live): `Sql.OrderBy(...)` and
`PartitionBy(...).OrderBy(...)` reject every number at the call (#693); how
MySQL, Oracle and SQL Server read one there is not verified. The statement
`ORDER BY` keeps its ordinals and its non-integer constants (ADR 0011).

**A function-argument enum is no value (decided — do not re-file):** a
`DateTimePart`, `RegexpOptions` or `SearchModifier` in an `object` slot is a
slip, one passed at an arity with no slot of its type, so
`ExpressionResolver.BindImplicitly` rejects one in every implicit value
position, on every dialect, as a call-site defect (ADR 0011 decides the class
outside its bar; #667). `Bind(value)` keeps a deliberate one writable; why
`Dbms` and `DbTypeCategory` still bind is in `public-api-design.md` §
"Separator/format-string parameters".

**A held stage's out-of-order clause stays unguarded (decided — do not
re-file):** a held builder can still append a clause after the one it must
precede — `Output(...)` after an `INSERT`'s `Values(...)` (#542), or
`Where(...)` after `DeleteWhere(...)` on a held `ThenUpdateSet` stage
(#544). Both render a statement the engine rejects outright
(`OutputAfterValues_IsRejectedByTheEngine` on SQL Server,
`MergeDeleteWhereBeforeUpdateActionWhere_IsRejectedByTheEngine` on Oracle), which is
the walk's clause-order line below (SD23), not an exception to it. That
`MergeBuilder.Validate` already walks the parts in order makes the MERGE check
cheap, not warranted; revisit SD23 as a whole or not at all.

**Whitespace is rejected in a bare-token position, accepted in a quoted one
(decided — do not re-file):** `DbSequence`'s constructor and `DbColumn`'s name
reject a whitespace-only string, while the alias guards
(`StringGuard.ThrowIfNullOrEmpty`) accept one. A sequence name and a column
name are emitted as bare identifiers, invalid as whitespace on every dialect;
an alias is quoted, where whitespace is legal. The stricter check is correct
where it is, and the asymmetry is not a defect.

**An empty `In` / `NotIn` list or quoted name, or an empty or blank
`IntervalLiteral` value, is rejected, though an engine runs it (decided — do not
re-file):** `In(...)` and `NotIn(...)` with no value, an empty `As(...)` alias,
CTE or derived-table name, and an empty or whitespace-only `IntervalLiteral`
value throw at the call on every dialect. SQLite 3.50.4 runs `IN ()`,
`NOT IN ()` and a zero-length alias, CTE name or subquery derived-table name;
MySQL 8.0 reads `INTERVAL '' DAY` and `INTERVAL ' ' DAY` as 0. An empty list or
a blank string reaches these positions only as a value computed empty, which
those engines would run without an error, so ADR 0011 decides the class outside
its bar. These lanes reject them: `IN ()` and `NOT IN ()` on MySQL 8.0, Oracle
XE 21.3.0, PostgreSQL 16.15 and SQL Server 2022, the empty alias on PostgreSQL
16.15, and the empty and blank interval values on Oracle XE 21.3.0 and
PostgreSQL 16.15 (`In_EmptyList_*`, `EmptyQuotedName_*`,
`IntervalLiteral_EmptyOrBlankValue_*`). An empty `Values(...)` name throws with
them, though SQLite 3.50.4 does not run one: it rejects the column list
`Values(...)` always emits, whatever the name
(`SqliteTests.ValuesColumnList_IsRejectedByTheEngine`). The blank interval value
departs from the clause above, which keeps whitespace in a quoted position:
whitespace is a legal name there, while MySQL reads this value as 0. `Array()`
takes no element instead: PostgreSQL, its one engine, runs an empty `ARRAY[]` only under a cast the caller writes,
`CAST(ARRAY[] AS int[])` returning an empty array, and rejects it bare
(`Array_NoElementsUnderACast_IsAcceptedByTheEngine`, PostgreSQL 16.15). A list
computed empty under that cast now reaches the engine, where it used to throw
(#669).

**`Build(ISqlBuilder, IDbConnection)`'s unregistered-provider throw names
`dbms` as its `ParamName` (decided — do not re-file):** its `ParamName` and
`ActualValue` describe the resolved `Dbms`, as those of the exception
`Build(Dbms)` throws for `Dbms.Unknown` do, so code that reads either sees one
fact whichever entry point it called; a caller-facing `cnn` name would split
that. Its message is the extension's own:
the DBMS is often supported and only the connection type unregistered (a
wrapper around a supported provider), so it names the type and both remedies,
`DbmsResolver.RegisterProvider(...)` and `Build(Dbms)`, or, for a null
connection, passing one (#679). `DbmsResolverTests` pins both messages.

"Structurally invisible to the analyzer" is a per-guard fact, not a law: the
correlated-DML guard's provable subset now has an advisory analyzer duplicate
(SQLA0300, ADR 0014) — the `Build()` guard remains the enforcement boundary,
and suppressing the diagnostic never disables the throw. Several ADR 0011
entries (leading `WITH`, bare `OFFSET`, `DELETE ... USING`, the joined shapes)
were admitted as invisible but are visible to ADR 0013's declaring-interface
mode; they stay on a cost ground (ADR 0011, #569). An entry's condition-1
sentence is its admission record, so check it before citing it as precedent.

**Each nested block defers the correlated-DML guard to its end (decided — do
not re-file):** the target instance legitimately appears in a CTE body or a
subquery as that block's own relation, so a target column there throws only if
its own block lists the target nowhere (#253, #607;
`DeleteFrom_CteBodyReferencingTarget_CorrectSql`,
`DeleteFrom_CteBodyNestedSubqueryReferencingTarget_CorrectSql` and
`DeleteFrom_SubqueryListingUnaliasedTarget_CorrectSql` pin the listed shapes).
A listing block reads its own relation, which is what the C# listing says; the
plain subquery was rejected as "one instance for two scopes" until #679, a
reason no engine gave while the CTE body took the same shape. The check waits
for the block's end because its FROM follows its SELECT list. A block that does
not list the target reaches it only by correlation, and the guard cannot see the
schema to tell whether a bare column there binds the target or a same-named
column of the block's own relation, so it rejects it. That
holds for a top-level `With(...)` body too: a bare column there binds the
body's own same-named column, nested or not
(`CorrelatedCteBody_BareTargetColumn_BindsTheBodysRelation`), and SQLite 3.50.4
resolves a body at its use site, so without one it can reach the target
(`CorrelatedCteBody_AliasedTarget_Executes`). A listing in a
sibling or an enclosing block does not count: a relation in between can still
shadow the column (`DeleteFrom_SubqueryListingTargetElsewhere_ThrowsArgumentException`). Each
set-operator branch is a block of its own, so a listing in one branch does not
cover a correlation in another (#611,
`DeleteFrom_CteBodyBranchCorrelatingUnaliasedTarget_ThrowsArgumentException`).
A target column in a compound's trailing `ORDER BY` passes only where some branch
lists the target, checked after the last branch — the acceptance the shared block
had (`DeleteFrom_CteBodyCompoundOrderByTargetColumn_CorrectSql`,
`DeleteFrom_CteBodyOrderByTargetColumnNoListing_ThrowsArgumentException`). The
listing is necessary, not sufficient: an `ORDER BY` name picks a result column,
which SQLite 3.50.4 also finds through a branch's FROM and rejects when no branch
selects it (`CompoundOrderBy_NameMatchingNoResultColumn_ResolvesThroughBranchFrom`),
so on SQLite 3.50.4 it can land on another branch's expression but never on the
outer row; the other engines are not verified. That is the compound choosing
among its own columns, so it stays outside the guard. With no listing the target
cannot be what the name means, so the bare target column is rejected as in any
block that does not list the target. The
guard keys on the target *instance*: a second, unaliased instance of the target
table that the subquery itself lists renders the tautology unguarded (it is that
block's own relation by instance), the same instance-identity fact ADR 0014
records for the analyzer — the harness template uses one instance for that
reason. A second instance the subquery does not list is rejected by the
every-table rule below (#677).

**Every unaliased table a nested block reads without listing it is rejected
(decided — do not re-file):** the correlated-DML guard's reasoning holds for
any table, not just a DML target. A column of an unaliased table renders bare,
and a bare name in a subquery binds that block's own same-named column first, so
`Build()` throws when a nested query block (a subquery, a CTE body, a `LATERAL`
body) renders one whose table its own `FROM` and joins do not list (#677). The
schema is not visible, so a reference no inner column could capture is rejected
too, as for the DML target, which keeps its own message (ADR 0011 decides the
class outside its bar). A compound's branches count together, which can let a
branch's capture pass but rejects nothing a per-branch check would accept. A
top-level column whose table no clause lists stays permissive; an
`INSERT ... SELECT` CTE body is a block like any other. On MySQL,
whose `INSERT` target cannot be aliased (ADR 0011), an `ON DUPLICATE KEY UPDATE`
subquery reaches the inserted row through `Excluded(...)` and has no spelling
for the existing row's other columns; the message's alias remedy is unreachable
there (§ Message grammar, recorded exception). The check costs every build 8 bytes, one
field on the buffer (ADR 0006).

**A bare `SET` target is a column of the statement's target table (decided —
do not re-file):** MERGE's `UPDATE SET`, `DO UPDATE SET`,
`ON DUPLICATE KEY UPDATE`, `InsertInto(...).Set`, `InsertIgnoreInto(...).Set`
and an `UPDATE` that does not qualify its target render a `SET` column by its
name alone, so another relation's column became the target's same-named one: a
swapped assignment (`u.B == t.B`) ran as a no-op under `UPDATE ... FROM`
(PostgreSQL 16, SQLite 3.45.1) and `MERGE` (PostgreSQL 16), and any other value
wrote the target's column (live; #674). The qualified spelling of
what was written, `SET "u".b`, runs on neither, so `AssignmentResolver` rejects
the call as a call-site defect. It keys on the table name, compared ordinally
(`TableReference.NamesSameTable`): with its schema when both handles carry one,
without it when either lacks one, since an unqualified name resolves through a
search path the library cannot see. So a schema-less handle of a qualified target
stays legal; a same-named table in another schema throws when both name their
schema and passes when either does not; a swapped assignment over two handles of
one table (a self-`MERGE`) passes; and a handle spelling the name in another case,
or qualifying it differently (`db.dbo.t` beside `dbo.t`), throws. The arms that
qualify the target (MySQL's direct-join `UPDATE`, SQL Server's re-listed target)
render each column by its owner's alias, and MySQL's sets a joined table's column
that way, so they skip the name check; an owner with no alias renders bare there
too, whichever table it is, so `ThrowIfUnaliasedOwner` rejects it, a second
unaliased handle of the target's own table included (the target instance itself
is always aliased in these arms). `UpdateSetClause` decides at
`Build()`, where that shape is final, the other clauses at the call. A column
list renders its entries bare too, so `ColumnListGuard.IsColumnOf` holds the
`INSERT` and MERGE `ThenInsert` lists and the `ON CONFLICT` target to the
target, and an `OUTPUT ... INTO` list to its own table, at the call. The
pseudo-row accessors `Excluded(...)`, `Inserted(...)` and `Deleted(...)` drop the
owner too, and their pseudo-row holds the target's row alone, so another table's
column read the target's same-named one (`PseudoRowColumn_ReadsTheTargetRowOnly` on
PostgreSQL 16.15, MySQL 8.0 and SQL Server 2022): each checks its owner at `Build()`
against the `INSERT`, `UPDATE` or `DELETE` target, by `ColumnListGuard.IsColumnOf`,
whatever expression it sits in (#675). The remedies the records name run there too: an
upsert reads another table's value through a subquery (the same twins, and
`PseudoRowColumn_ReadsAnotherTableThroughASubquery` on SQLite 3.50.4), and the SQL
Server twin outputs a joined column directly. Where one sits stays the engine's — a `SELECT`
or a `MERGE` has no such target (`SELECT EXCLUDED.a` is rejected on PostgreSQL 16.15,
the same twin) — the complete-construct class the window-function paragraph leaves
unguarded, and `SQLA0102` reports `Inserted` / `Deleted` outside `OUTPUT`.

**A builder is one chain (decided — do not re-file):** a stage on a statement
builder appends to it (`Returning(...)` once the statement builds; `With(...)` is
no statement builder, and each head called on it starts a new one), so a held
builder that another statement already holds would change that statement too (#676, live on
PostgreSQL 16). Two shapes are rejected. A builder composed into another
statement — `In`, `NotIn`, `Exists`, `NotExists`, `Any` / `All` / `Some`, a
scalar subquery, `AsTable`, a CTE's `As`, a lateral or apply join — takes no
further stage call (`This SELECT statement is composed into another statement;
start a new chain to change it.`); building it alone stays legal, since that
changes nothing the other statement renders. A stage that runs the caller's code
before it lands — `Values(IEnumerable<object[]>)` enumerating its rows — runs its
checks again after that code has run: the code can compose the builder into
itself, which overflowed the stack and ended the process, or call a stage on it
that changes what was checked, such as a `VALUES` row of another width (#679). A set operator returns a new
builder holding the chain so far, and the held left branch can no longer be
extended, built or composed (`This SELECT statement was continued by a set
operator; use the builder the operator returned.`): the continuation takes the left
branch's part list over instead of copying it, so a stage call on the left would
land on the right branch and a build or compose of it would render the whole
compound. A set operator costs one builder, measured at +32 B on `SELECT` and
+56 B on `INSERT ... SELECT`, and a non-compound build is unchanged (ADR 0006).
Two statements cut from one held prefix (`var all = q; var page =
q.OrderBy(...)`) still share the instance, which only builders returning a new
instance from every stage would separate; `docs/query-statements.md` §
"Reusing a builder chain" states it. Composing `all` once `page` is derived,
before or after `page.Build()`, renders the page (#676's build-then-compose
count): that is this fork, and rejecting a built builder in `FormatCore` would
also break composing a chain built as written, which renders what was built.

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
The release audit extended #245's family with the once-per-query-block
duplicate-clause guard (`SqlBuilderBase.ThrowIfDuplicateClauseInBlock` — a
stage repeated on a held, not-yet-built builder would append `WHERE ... WHERE`;
a set operator resets the block) and the `ReturningBuilder` single-use freeze
(`Into(...)` then `Build()` on the held stage appended a second `RETURNING`).
Its second pass widened the same guard to MERGE (`USING`/`ON`
once-per-statement; the branch actions once per `WHEN` branch, where a new
`WHEN` clause resets the branch scope), paired join `ON`/`USING` by adjacency
(at most one since the last join clause), and added `MergeBuilder.Validate`'s
structural pairing (a `WHEN` branch must carry an action; `THEN INSERT` must
carry a `VALUES` row). The same pass fixed the freeze ordering the family
depends on: `ReturningBuilder.Build` freezes only after the delegated build
succeeds, and the multi-row `Values` batch overloads validate every row
before mutating the held clause — a failed stage call must leave the builder
exactly as it was (`BuilderReuseTests` pins both). Its fourth pass ran the
same walk from every nested render — an `IN` subquery, a CTE body, a scalar
select item never pass through `BuildCore`, and a duplicate clause or
dangling join is no less wrong one level down — and made the `INSERT` root
open a new `WITH` block, so a leading `With(...).InsertInto(...)` and the
feeding `SELECT`'s own `With(...)` coexist. The correlated-DML guard (#253)
arms for `MergeInto(...)` too. Its fifth pass gave the walk a slot-exclusivity
dimension — `LIMIT`/`FETCH`, a `DELETE`'s `FROM`/`USING`, and an `INSERT`'s
`VALUES`/`SET`/`SELECT` row sources are spellings of one slot a held stage
could stack past the once-per-kind check — and made a condition-free join
(`CROSS JOIN`, `NATURAL ...`, `APPLY`) *consume* the `ON`/`USING` slot rather
than re-admit it. **The walk checks multiplicity and slot exclusivity, not
clause order** (decided, pass 5, SD23): a held earlier stage gets past the
typestate (`var o = q.OrderBy(t.A); q.Where(...)` builds `ORDER BY a WHERE ...`,
which PostgreSQL 16 and SQLite 3.45.1 reject). A pair PostgreSQL 16 runs out of
order (`OFFSET ... LIMIT`, `FOR UPDATE ... LIMIT`) returns the in-order text's
rows and SQLite rejects both (live; the other engines are not verified), so the
walk stays a backstop for a repeated or stacked stage on one held instance
(#676).
Some repeats it rejects run on some engine (for example SQLite's second
`ON CONFLICT`, PostgreSQL's second locking clause); they are rejected on
every dialect by decision (ADR 0011, which lists the recorded cases).
Its sixth pass extended slot exclusivity to the upsert (`ON CONFLICT` /
`ON DUPLICATE KEY UPDATE`), conflict-action (`DO NOTHING` / `DO UPDATE SET`),
and MERGE branch-action slots, paired a set operator with the `SELECT` that
must follow it, and made a `RETURNING` written on a stage *pending* until the
statement is built from that stage (building the stage before it throws
rather than dropping the clause). Outside the walk, the same pass moved every
`SqlExpression` operator's left-operand null check into `OperandGuard` so
`null + x` throws instead of rendering `( + :0)`, made `Values(...)` /
`Unnest(...)` derived-table `Column(...)` references render bare like a CTE's
(the #165 family), and admitted the Oracle leading-`WITH` rejection as an
ADR 0011 bounded exception — a dialect guard, not a walk rule.
Its seventh pass made the `RETURNING` obligation a count of live stages — a
failed build restores it, a rejected `Returning(...)` never marks it, and a
second stage on one statement is refused — and widened two dialect guards:
the leading `WITH` rejection to MySQL's `INSERT` (live: 8.0 `ER_PARSE_ERROR`),
and `TOP` beside `LIMIT`/`OFFSET`/`FETCH` to every dialect, since `TOP` is SQL
Server's alone and SQL Server rejects it beside `OFFSET`/`FETCH`
(`TopWithOffsetFetch_IsRejectedByTheEngine`) in one query block; it runs a
compound's `OFFSET ROWS` / `FETCH` beside a branch's `TOP`
(`TopInBranchWithCompoundOffset_IsAcceptedByTheEngine`, #669). **The #400 pairings stay
Build()-time by decision** (pass 7): narrowing the `Output(...)` continuation
types to withhold `Returning`/`Using`/upsert would split three interface
families for a rejection the guard already states exactly, so ADR 0007's
compile-time-first order yields here; the typestate still withholds `Output`
after `Using`, and that asymmetry is accepted, not a finding. That weighs a type
against a guard; that the guard throws at `Build()` rather than at the call
completing the pair is § "When to throw"'s decision.
Its eighth pass added the bare-`OFFSET` rejection on MySQL and SQLite (ADR
0011) and the SQL Server joined-`UPDATE` guard's second remedy for the
direct-join chain, made the CTE-body exemption cover nested subqueries, and
extended this rule to TableClassGen: a CLI guard sees the whole schema, not
the narrowed run (`GuardClassNames` plus the on-disk table-name check), and
an object-valued `--config` key is rejected at parse (the full kind check is
the JSON-kind clause in § "TableClassGen" below, #645).
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
| Any written condition clause — `.Where(...)` (SELECT/UPDATE/DELETE), `.Having(...)`, aggregate `.Filter(...)`, JOIN/MERGE `.On(...)`, CASE `When(...)`, MERGE `.WhenMatched(cond)` / `.WhenNotMatched(cond)` / `.WhenNotMatchedBySource(cond)` / `.DeleteWhere(...)` / action `.Where(...)` | **throw at Build()** |
| Empty SELECT list (#236); empty `SELECT`/`UPDATE` `.From()`; empty `IN`/`NOT IN`, empty `VALUES` row (#243); empty `SET`/`DO UPDATE SET`/`ON DUPLICATE KEY UPDATE`/MERGE `.ThenUpdateSet()`; empty `Sql.Decode(...)` pairs (#396); INSERT column-list vs. `VALUES` row width mismatch (#397); empty explicit `INSERT` column list (1.0 release review); empty explicit MERGE `.ThenInsert(cols)` column list and its column-list vs. `VALUES` width mismatch — `ThenInsert()` with no arguments stays the positional form, exactly as columnless `InsertInto(table)` does (release audit pass 1) | throw **eagerly** |

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

**An empty `OnConflict(...)` / `OUTPUT ... Into(t, ...)` column array keeps
the column-less form (decided — do not re-file):** an empty conflict target
renders a targetless `ON CONFLICT`, which arbitrates on every unique (or, on
PostgreSQL, exclusion) constraint rather than the named one, and an empty
`INTO` list renders a positional `INTO t` that skips the OUTPUT/INTO width
check (`OutputClauseGuard.ThrowIfIntoWidthMismatch`). The ground recorded above
for the explicit `INSERT` column list — a computed empty array silently
switching to the column-less form and disabling the width check — holds here
too, and so does "written as literals", which was true of the guarded lists
as well; what differs is the cost. `InsertInto(table)` already existed, so
that guard was free, and `ThenInsert()` was undocumented when its guard made
it an overload. Both no-list forms here are documented call shapes —
`OnConflict()` in the docs pages, `Into(t)` in the `*OutputInto` XML docs
("omit to target the table's columns positionally") — so guarding either
would take a no-list overload, and every assembly compiled with the
zero-argument call would throw until rebuilt, a binary break paid by correct
code for a misuse no docs page, cookbook recipe or companion package makes
(none computes either list). A caller shape that computes either list
reopens this (#579).

**An empty `INSERT` row stays rejected on MySQL too (decided — do not
re-file):** `Values()`, `InsertInto(t).Set()` and an empty explicit column list
throw on every dialect, though MySQL 8.0.46 inserts a row of defaults for the
`() VALUES ()` and `VALUES ()` they would emit
(`MySqlTests.DefaultsRow_IsAcceptedByTheEngine`), so #396's "invalid SQL"
premise is false for MySQL. An array computed empty is likelier a caller's slip
than a deliberate defaults row, and on MySQL the text would insert that row
with no error, the silent class this section exists for, so ADR 0011 decides it
outside its bar (#669). A defaults row is a
construct of its own, `DEFAULT VALUES` in `public-api-design.md`'s forms not
yet offered, added when a caller needs it (ADR 0010, #679).

## Null arguments: where the runtime-guard obligation stops

The guard mission targets **silent** wrongness — a build that succeeds with SQL
the caller did not mean. Judge a null argument by which failure it produces:

- **Silent acceptance** (the statement still builds): guard it, whatever the
  parameter's type. Shipped instances: `object`-typed value positions
  (`ExpressionResolver`'s "pass `Sql.Null`" message) — except a comparison or
  predicate operand (`=`, `BETWEEN`, `LIKE`, `IN`, a simple `CASE`,
  `REGEXP_LIKE`, the array and JSONB predicates bar a `?&` / `?|` key, `@@`),
  where `Sql.Null` builds a comparison with NULL rather than a NULL test, so a
  new predicate factory resolves its operands with `ResolveCompared`, which
  names `.IsNull` (#614). `Sql.Null` beside a comparison operator or in a
  simple CASE `When(...)` is `SQLA0303`'s to report, not a guard's: SQL Server
  2022 reads `= NULL` and `<> NULL` as NULL tests under the deprecated
  `SET ANSI_NULLS OFF` (`ComparisonWithNull_UnderAnsiNullsOff_ReadsANullTest`),
  so the text has a meaning there (#742). A grouping, partition or `DISTINCT ON` key is another:
  `Sql.Null` there is a constant key, silent where it runs (`PARTITION BY NULL`
  is one partition on PostgreSQL 16 and SQLite, `GROUP BY NULL` one group on
  SQLite) and rejected by PostgreSQL 16 in `GROUP BY`, a grouping element and
  `DISTINCT ON` (all live), so it is no remedy in any of the three and
  `ExpressionResolver.NullKey` names a column or an expression instead (#673).
  An `ORDER BY` key is a fourth (#693): a statement `ORDER BY NULL` fails on
  PostgreSQL 16 and sorts by a constant on SQLite, and `PERCENTILE_CONT(0.5)
  WITHIN GROUP (ORDER BY NULL)` returns NULL with no error on PostgreSQL 16
  (live). `OrderBy(Null)` inside `OVER` runs on PostgreSQL 16 and SQLite 3.45.1
  (live) and stays the frame spelling with no ordering; the message's "an
  expression" covers it.
  MySQL's `Against` takes search text, not a compared value, so neither remedy
  fits: its own message says a NULL search text
  matches no row (live on 8.0, in every modifier form) and names passing the
  text or dropping the predicate; `AgainstScore`, unmeasured, keeps the
  `Sql.Null` message. The other full-text predicates (`Contains`, `Freetext`,
  SQLite `Match`) keep the `Sql.Null` message, because following it fails
  loudly. FTS5's `MATCH NULL` errors (3.45.1, live); FTS3/4 tables return no
  row, but they lie outside `Match`'s documented FTS5 contract. SQL Server
  rejects a NULL search variable with Msg 7645 (community reports; not run
  here, as the lane images ship without Full-Text Search), and the literal
  `NULL` that `Sql.Null` emits is expected to fail the same way or as a parse
  error. Other shipped instances: string identifiers (`StringGuard`), null
  elements inside arrays/`params` (#403), a null
  subquery in `CteBase.As` (previously emitted `WITH "c" AS ()`),
  `new BindValue(null)` (a never-true `= NULL` predicate the factory already
  rejected), and `default(OutputParameter)` — a struct default no annotation
  can flag, rejected at the `Into(...)` call (#569).
- **Loud failure** (a `NullReferenceException` from dereferencing a single
  non-nullable reference parameter — `Column(DbColumn)`,
  `Exists(subquery)`, the condition operators): the nullable annotation *is*
  the contract — the compiler warns (CS8604), and the throw lands either at
  the factory call (`Column`) or at `Build()` (a stored subquery or
  condition). Either way the statement never builds, so nothing is silently
  wrong. No runtime guard is owed; do not file these in review.
- **Documented null** (the parameter is annotated nullable and its docs give
  `null` a meaning): no guard is owed, because the statement builds exactly
  as documented. `DbTableBase` / `DbTable`'s `tableAlias` is `string?`, where
  `null` and an empty string both mean no alias (#614).

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
convert those NREs to named exceptions. The release audit closed the remaining
typed-element positions the same way: `CollectionGuard.ThrowIfNullElement`
(caller's parameter name, construct-named message) guards the clause
constructors — `FROM`, JOIN/DELETE `USING`, `ON CONFLICT`, `OUTPUT INTO`,
`WITH`, `GROUPING SETS`, MERGE `ThenInsert` — and the multi-row `VALUES` rows;
`FactoryGuardSweepTests` now fails on a bare `NullReferenceException` reached
from an element injection (whole-argument nulls keep the loud-NRE exemption);
and `BuilderElementGuardTests` pins the builder-side sites the factory sweep
cannot reach. A new typed element position lands on this shape, not a bare NRE.
A lone null argument binds the `params` array itself to null (`From(null)`), so
each position gets a named message there too, never the runtime's
`Value cannot be null.`: the element's message where the site's guards give it
one (`CollectionGuard.ThrowIfEmpty`'s `nullMessage`, `ThrowIfNullElement` run
first, or a resolver's own check), and its empty-collection message where they
do not (`IN` and the `SELECT` list among them). An `INSERT`'s
`Values(null)` binds the rows overload, and MERGE's, which has none, the row
itself; both messages name `Sql.Null` (`BuilderElementGuardTests`, #679).

## Compile time or a guard: the copy count (decided — do not re-file)

ADR 0007's compile-time-first order covers a rejected pairing or order in a
fluent chain too: withhold the step through the return type. The one cost
that buys a `Build()` guard instead is the stage copies the withholding
needs. On the withheld route — through a set operator too — every stage that
offers the withheld step, or leads to one that does, needs a copy if a valid
chain also reaches it. A stage only that route reaches is changed in place,
and a copy whose members match an existing stage of the same statement and
position (its `I<Statement>Builder` name fits; the branch end a MERGE action
returns to, say) is that stage; neither counts. A node type counts like a
stage.

- **Four or fewer copies: compile time.** The quantified operands and MySQL's
  `INTERVAL` (0: their three node types, re-based in place, #666),
  `WithRecursive` (1, #521), the
  `WhenNotMatchedBySource` branch (1), `With(...)` after an `INSERT`'s
  `Output(...)` (1: `IInsertBuilderOutputColumns`, #679), `Into(...)` after an
  `ON CONFLICT` action (1: `IInsertBuilderConflictAction`, #679), the
  direct-join `UPDATE`'s `SET`,
  which leaves out `From` and `Output` (1), `StringAgg`'s inline `ORDER BY` (1, #582),
  DISTINCT aggregate windows (2: the node the three DISTINCT factories share
  and its filtered form, #662), `RETURNING` on the joined `DELETE ... FROM`
  and direct-join `UPDATE` (2: their `WHERE` stages, #582), Oracle's MERGE
  action filters on a conditioned branch (3: the `WHEN MATCHED AND` and
  `WHEN NOT MATCHED AND` stages and the latter's `INSERT`, #582) and `InsertIgnoreInto` (4: its Table, Columns, Values and Set
  stages, #275).
- **Five or more: a `Build()` guard**, its count recorded here. A leading
  `WITH` before `ON DUPLICATE KEY UPDATE` (6, #569); a leading `WITH` before
  Oracle's MERGE action filters (11: the target, `USING`, `ON` and branch
  stages a `With(...).MergeInto(...)` chain passes through, #679); a leading
  `WITH` before `RETURNING ... INTO` (18: the `INSERT`, `UPDATE` and `DELETE`
  stages a `With(...)`-led chain passes through to `Returning(...)`, #679); an
  action filter beside a `MERGE` branch form Oracle refuses (10: past its first
  filter the `When`, `WhenMatched`, `ThenUpdateSet`, `WhenNotMatched`,
  `ThenInsert` and `Values` stages, and past a refused branch the `When`,
  `WhenMatched`, `WhenNotMatched` and `ThenInsert` ones, #679); `TOP` beside
  `LIMIT`/`OFFSET`/`FETCH` (9); `TOP ... WITH TIES` without an `ORDER BY` (9:
  the `SELECT` stages before it, the set operator, and a `TopClause` copy,
  since a plain `Top(n)` reaches it too), or in a set-operator branch (the
  same 9, #679); the `OUTPUT` pairings (8: the
  `INSERT` stage after `Into(...)` and its `VALUES`, the `UPDATE` `SET`,
  `WHERE`, `FROM` and join-`ON` stages, and the `DELETE` and its `WHERE`;
  #400, recounted in #679); an `INSERT ... SELECT` embedded as a
  subquery (13; thrown at the embedding call, not at `Build()`, #679).

Each copy doubles later edits to the stage it mirrors (all but one later
commit to an `INSERT IGNORE` stage since #275 also edited its `INSERT` twin);
that is the cost the line prices. How rarely the pairing is written, and
whether the analyzer reports it, are not inputs. Either way, a held stage
that can reach both halves needs a `Build()` backstop, since a type
constrains one chain and not a held instance (the walk, `ReturningGuard`); a
cast does not (ADR 0011, the leading-`WITH`-before-`MERGE` entry). A fact no
type carries — a dialect, a table instance's identity — is a guard outright.

## When to throw: eagerly vs at Build()

- **Eagerly (in the factory / clause method)** only when the fact is fixed at
  the call site: a `params` array length, a collection count. Precedent:
  `PartitionBy` (#69), the empty-`Select()` guard (`SelectItemResolver.ResolveOrThrow`, #236),
  and the `.WithColumnList()` column-name guard (the first query block's
  resolved select items are fixed at the call, #263).
  A **value-domain guard** (an argument value no engine accepts, e.g. a
  percentile fraction outside 0..1) is also eager — its three admission
  conditions are ADR 0012 (#295); never domain-check a bound value.
- **At Build()/format time** when the position's own architecture calls for
  it: conditions (`WhereClause`/`HavingClause`/etc. are shared by every
  statement type that embeds them — SELECT/UPDATE/DELETE and the aggregate
  `FILTER` — so the throw lives in the clause node's own `Format`, letting one
  implementation serve all of them; see "no elision" above) and builder stages
  (`SqlBuilderBase._parts` only reaches its final shape once every stage call
  has run). An eager check here would misfire on legal code. A fact fixed by
  one stage's own arguments is the first bullet's case, not this one
  (`OrderBy(0)`, `Into(...)`'s variable names, #569), and so is one fixed by
  the stages already called (the pairings and repeated stages below excepted):
  the `OUTPUT ... INTO` width at `Into(...)`, the `INSERT ... SELECT` width at
  the first `Select(...)` (#582). The joined
  `DELETE ... FROM`'s re-listed target is not: a later join can re-list it, and
  the dialect decides, so it waits for `Build(Dbms)` (#679).
- **A condition clause stays Build()-time even where its node is one
  statement's own** (the MERGE `ON`, `WHEN ... AND`, `DeleteWhere` and action
  `Where`): an empty condition is rejected one way in every position, so the
  empty-state table above has one row for all of them (#582).

**The walk, the `RETURNING` count and the `Validate(Dbms)` pairings throw at
`Build()`, not at the call that completes them (decided — do not re-file):**
some are fixed at that call and could throw there, leaving a misused held
builder still buildable, which a `Build()`-time throw does not. The walk stays
on cost: rerunning it at each `AddPart` would add an estimated 0.3 to 2.4 times
a build's own time (from its measured share of a build), and holding its state
on the builder instead costs 8–24 bytes each, by how it is packed (ADR 0006).
The rest stay beside the absence and dialect checks in the same walk and
`Validate(Dbms)` overrides, which can only run at `Build()` — a join's missing
`ON`, a target not yet re-listed, every `Dbms`-scoped guard, a build from the
stage before `RETURNING` — since moving them helps only a misused held builder
(#679).

## TableClassGen: a run does what it was asked or fails

The CLI's silent-wrongness class is a run that exits 0 (or reports "in sync")
having acted on a scope, path or file other than the one the caller named.
Its exit code is what a script or CI job reads, so a stderr warning does not
count as failing. Each clause below is a shape A6 found (#645):

- **No mode creates the database.** The tool only reads the catalog, so the
  SQLite `--file` opens without creating in every mode (and `--check` /
  `--dry-run` write no file at all). A mistyped path must fail at connect;
  opened as an empty catalog, it lets a generate run exit 0 having read
  nothing, and `--check` report every class removed.
- **A narrowed run reads the object set a full run reads, and writes what a
  full run writes.** `--tables` applies the same table filter as the full scan
  (no views, no engine-internal tables), and emits the catalog's stored name,
  not the caller's spelling, where the engine folds identifier case (SQLite's
  `--tables ITEM` emitted `"ITEM"` for `item`); otherwise a narrowed run writes
  a class the next full `--check` disowns or reports modified. The SQLite and
  information_schema readers resolve the stored name (SQL Server's default
  collation admits `ORDERS` for `Orders`); Oracle's folds every name to upper
  case, as an unquoted identifier is stored.
- **A full run writes every table it lists, or fails naming it.** A table with
  no columns is generated: PostgreSQL's was skipped while the run exited 0, and
  `--tables` called it absent. One with a column the user cannot see fails the
  run: PostgreSQL lists a table on any privilege but each column only on a
  privilege over it, so writing the visible ones stripped the rest from a
  committed class; `pg_attribute` counts them whatever the grants. MySQL and
  SQL Server have no such count, so only an empty list is caught there; a
  partial column grant is rare, and whether it narrows their listing is not
  probed (decided in #645). An Oracle table whose quoted name is not upper case
  fails the run, by `--tables` too: every Oracle read folds the name, so it was
  skipped (decided in #645).
- **Every check runs before the first write.** A guard, a catalog read, and
  the orphan scan all run before any file is written, so a failure leaves the
  directory as it was; only a write itself can fail after another, and its
  message names the files already written. The orphan scan ran after the
  writes, so a subdirectory it could not enter aborted a `--fix` that had
  already rewritten every file, with an empty report.
- **A guard that cannot read its fact fails closed.** A file at a path the
  run would write whose table literal (`: base("<table>", tableAlias)`) the
  tool cannot read is refused, never passed: skipping the check let a run
  overwrite another table's file and exit 0. The literal is the fact, not the
  header, which 0.7 and earlier never wrote. The orphan scan fails the run on a
  `.cs` file it cannot read, hand-written or not: skipped, an unreadable orphan
  of the tool's own read as in sync (decided in #645 over the earlier skip). It
  counts a file as its own by the generated header among the leading comments
  and the run's `--namespace`, so another generation nested under `--output` is
  not claimed. The readers (`CommittedFile`) accept the table literal of every
  version, the column literal of 0.6 on, the header of 0.8 on, and both
  core-name spellings; `FixtureCorpusTests` holds them to the 0.8 and 1.0
  shapes.
- **A blank value is an absent value**, for every option and config key. It
  is dropped where the value is stored (`ParseArguments`, `ReadConfigFile`), so
  a blank flag leaves its key to the `--config` file and no reader downstream
  can tell `""` from the default it stands for: `--output ""` used to skip the
  orphan scan through `Directory.Exists("")`.
- **A repeated option is decided, not last-wins.** A repeated flag or
  `--config` key is rejected at parse: keeping the last `--tables` silently
  narrowed `--check` (#643).
- **A `--config` value's JSON kind matches its option**, checked after `null`
  and a blank string have read as absent: a switch takes a boolean or the
  string `true`/`false`, as `Flag` reads it; `tables` an array or string; a
  value option a string or number. `"output": false` must not become the
  directory `false`.

## Message grammar

One sentence; name the construct by its **SQL spelling**; state the
requirement. Unit tests assert the message verbatim (see the unit-tests rule),
so the wording is part of the contract.

- ✓ `PARTITION BY requires at least one expression.`
- ✓ `The target of a correlated UPDATE, DELETE, or MERGE must be aliased.`
- ✗ `Invalid input.` — names nothing, states nothing.
- The remedy a message names is reachable from the chain that threw: the
  SQL Server joined-`UPDATE` guard says "re-list the target in FROM" on the
  `From(...)` chain and "join through `From(target, ...)`" on the direct-join
  chain, which has no FROM (the `DELETE ... USING` guard was the first such
  split, pass 5; the UPDATE twin, pass 8). The empty join `ON` names
  `CROSS JOIN` only on the `SELECT` chain, the one with `CrossJoin`; a joined
  `UPDATE` or `DELETE` states the requirement alone (`DmlOnClause`, #582).
  Recorded exception (#677): the every-table correlated guard's "must be
  aliased" also fires for a MySQL `ON DUPLICATE KEY UPDATE` subquery reading
  the `INSERT` target, which MySQL cannot alias. The buffer that throws does
  not know which table is the `INSERT` target, and a message split for one
  dialect's one clause was weighed against the docs note and left out; the
  CHANGELOG and `docs/query-statements.md` name `Excluded(...)` there.
- A guard shared by several clauses names the clause the caller wrote, never
  one sibling's spelling: `AssignmentResolver` takes each caller's clause,
  since `ON DUPLICATE KEY UPDATE` has no `SET` token to name (#582).
- **`ParamName` is a parameter of the member the caller invoked**, or absent;
  the `dbms` entry above is the one recorded exception. A shared resolver or
  guard takes the name from its caller, or names its own parameter as every
  caller names theirs; `ExpressionResolver`'s `item`, `items` and `pairs`
  reached the caller until #669. `FactoryGuardSweepTests` holds it for the
  arguments it injects into the factories; the public operators and instance
  members #669 changed are pinned by their own tests.
- **TableClassGen's construct is the flag.** A CLI message names the flag or
  `--config` key that fixes it by its spelling (`--schema`, `"schema"` in the
  `--config` file), the file for a malformed `--config`, or the schema object
  when none can (`rename the column`). It prints as one `error: <message>`
  line, so the tool's own sentence takes the parse errors' shape, with no
  terminal period; an appended driver or file-system cause keeps its own text.
  That cause is appended, never substituted: an exception reaching `CliRunner`
  unwrapped prints a message that names no flag, and a driver's names no path
  either (#645). A flag is named only when changing it can fix the cause: a
  failure no flag fixes (a server session failing after connect) names the
  target being read instead, since advice that cannot help sends the caller
  after an option that was never wrong (#645).

The `Invalid type for <X>: <type>` family is built by one helper,
`ExpressionResolver.UnresolvableValue`, and `<X>` names **the position the
value reached** — `SelectItem`, `ReturningItem`, `OutputItem`, `OrderByItem`,
`GroupByItem`, `PartitionByItem`, `DistinctOnItem`, `InsertValue`, `Assignment`,
and `nameof(Bind)` where the factory
resolves its own argument, and `ValuesItem` for a `Sql.Values(...)` row.
`RETURNING`, `OUTPUT` and `Sql.Values(...)` borrow the `SELECT`-list or `INSERT`
resolver but pass their own names (#569).
Naming instead the type that position requires reads as a tautology against
the offending type, never saying what the caller did wrong, and leaks an
internal name into the public failure surface: that is how three `SET`-list
guards said `Invalid type for EqualityCondition` until #497. The one type name
in the family is `SqlExpression`, for the generic value position whose
requirement *is* that — not a precedent for a narrower position. Four values a
caller writes in a reachable slip leave the family for a remedy of their own:
a `SELECT` prefix (`Distinct`, `DistinctOn(...)`, `Top(n)`, `Hints(...)`)
written out of the order the `Select(...)` overloads take, which lands in an
item slot; a `Group(...)` grouping set outside `GroupingSets`, `Rollup` or
`Cube`; `DBNull.Value` passed to `Bind(...)`, which names `Sql.BindNull()`
(#669); and an array among the values an `INSERT` or MERGE `Values(...)` call
passes, which `params object[]` makes of several rows, of a row of another
element type, or of an array value, which PostgreSQL binds through
`BindArray(...)`, named before the
row's width (#679).
