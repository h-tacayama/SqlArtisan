# ADR 0011 — Bounded exceptions to the validity-enforcement boundary: when a construct valid on some dialect may still be rejected

**Status:** Accepted

## Context

ADR 0007 draws the line between what the library **rejects** (a construct that
is *incomplete* — ungrammatical in every dialect because a mandatory element is
missing) and what it **emits faithfully** (a *complete* construct that some
engine happens not to support — dialect availability, surfaced by the opt-in
analyzer and ultimately the database). Its dividing test is literal:

> Is there any supported dialect, in any configuration, where this exact text is
> valid SQL? If no → incomplete → reject. If yes-somewhere → dialect
> availability → permissive.

Aliasing a DML target — `Update(new UsersTable("cu"))`, emitted as
`UPDATE users AS "cu" SET …` — is valid on PostgreSQL and MySQL (and an aliased
`INSERT` target is how PostgreSQL spells `ON CONFLICT`). By the literal test it
is therefore *dialect availability*, which ADR 0007 says the library must not
throw for. Yet on **SQL Server** the emitted text is a hard syntax error: T-SQL
cannot alias the target of an `INSERT`/`UPDATE`/`DELETE` directly (the alias must
be introduced through a `FROM` clause — the joined-DML form, not yet built —
#237). This surfaced in the #225 audit (GAP-10 / ERG-09) as part of the
correlated-DML family (#239).

Two facts make this case unlike ordinary dialect availability (e.g. `CUBE` on
MySQL, which ADR 0007 correctly leaves permissive):

1. **The analyzer cannot see it.** The dialect matrix (ADR 0003) keys on
   *construct usage* — a method/property/field reference. The target's alias is
   a **value-level constructor argument** to the table class, invisible to a
   construct-level matrix entry. So the opt-in analyzer — ADR 0007's designated
   safety net for dialect availability — structurally cannot warn here. (This is
   the same reason #239's correlated-target guard is a Build()-time check, not a
   matrix entry.)
2. **There is no valid spelling on the target at all.** For an ordinary
   dialect-availability case the user can choose a supported construct instead.
   Here, until joined DML lands (#237), SQL Server has *no* way to spell an
   aliased/correlated DML target — the alias can only ever be a mistake there.

So the two mechanisms ADR 0007 relies on for dialect availability — the analyzer
and "the database is the final arbiter" — leave this case with no early,
deterministic signal, only a runtime syntax error the analyzer can't pre-empt.

## Decision

The library **throws at `Build(SqlServer)`** (an `ArgumentException`, per the #69
/ #190 guard precedent) when the target of an `INSERT`, `UPDATE`, or `DELETE`
carries an alias. The message names the construct and states the requirement:

> `SQL Server does not support aliasing the target of an INSERT, UPDATE, or DELETE statement; use an unaliased target table — a correlated UPDATE or DELETE joins through From(...) instead.`

This is a **bounded exception to ADR 0007**, not a repeal of it. It is confined
to exactly the case where both of ADR 0007's dialect-availability safety nets are
unavailable *and* no valid spelling exists on the resolved target:

- **Dialect-scoped:** it fires only for `Dbms.SqlServer`; the same aliased target
  emits faithfully on PostgreSQL, MySQL, SQLite, and Oracle (their bare/`AS`
  alias forms — the aliased `UPDATE`/`DELETE` target is live-verified on Oracle
  and MySQL, #255; the aliased `INSERT` target is native on PostgreSQL, where it
  introduces the `ON CONFLICT` correlation name).
- **Position-scoped:** only the DML *target* alias. Aliases everywhere else
  (`FROM`, joins, derived tables, CTEs) are untouched.
- **Not a portability opinion.** The library is not judging that SQL Server
  "should" support this; it is refusing to emit text that cannot be correct on
  the target the caller explicitly named, in the same fail-loudly spirit as the
  empty-state and correlated-DML guards (#236 / #239).

The mechanism is a `Validate(Dbms)` hook on `SqlBuilderBase`, invoked at the top
of `BuildCore`, that `InsertBuilder`, `UpdateBuilder`, and `DeleteBuilder`
override — so every build path (including `Returning()`, which funnels through
`BuildCore`) is covered.

## Consequences

- **ADR 0007's dividing test gains one explicit, enumerated exception** rather
  than being silently bent. Any future "throw for a valid-somewhere construct"
  must clear the same bar this one did: the analyzer structurally can't see it,
  *and* the resolved target has no valid spelling. Absent both, the answer stays
  ADR 0007's default — emit faithfully.
- **Revisit when joined DML lands (#237).** Once `UPDATE … FROM` / `DELETE … FROM`
  give SQL Server a real spelling for the aliased/correlated shape, this guard's
  premise ("no valid spelling exists") weakens; re-evaluate whether it should
  narrow or point at the joined form instead. *Closed (release audit pass 4):*
  joined DML landed (#237/#258) with its own alias requirement, so the joined
  shapes never reach this guard; it stays for the un-joined target, where
  T-SQL still has no aliased spelling.
- **The premise is empirically anchored,** not asserted: an integration test
  (`SqlServerTests.AliasedDmlTarget_Rejected`) executes the raw emitted
  form against a live SQL Server and confirms the rejection, alongside the
  unaliased form succeeding — so the guard rests on a verified engine fact, not
  on grammar folklore.
- **Complements, does not overlap, #239.** #239 guards the *unaliased correlated*
  target (a silent tautology) via the owning-table model (shipped as #253); this
  guards the *aliased* target on the one dialect where aliasing it is invalid.
  Together they make the correlated-DML surface fail loudly on every dialect.

- **`Validate(Dbms)` runs once per query block.** The outermost builder runs
  it from `Build(Dbms)`; a subquery, CTE body, or derived table runs it from
  `FormatCore`, against the target that build resolved. A nested block
  resolves its own ordinals and holds its own clauses, so a shape with no
  valid spelling there is no less rejected one level down — the earlier
  outermost-only scope let a subquery's `ORDER BY 0` through, and its review
  corrected that. The dialect-blind structural walk (a duplicate clause, a
  dangling join) runs on the same two paths. Nested *availability* stays
  permissive, as ADR 0007 has it: these guards reject only what the resolved
  target cannot spell at all — live-confirmed on PostgreSQL 16.13 and SQLite
  3.50.4 (the engine the lane pins), where a subquery's and a CTE body's own
  `ORDER BY 0` / `ORDER BY -1` are rejected exactly as the outermost one is.

## Later instances admitted under the same bar (release audit, pass 1)

Each guard below cleared this ADR's two-condition test — the analyzer
structurally cannot see the fact, and the resolved target has no valid
spelling — and joined the `Validate(Dbms)` hook. Every entry ends with a
`Live twins:` sentence naming the integration test that executes the raw
statement on each dialect the entry claims rejects it (and the accepted form
on a dialect the guard leaves alone, where one exists), or `Live twin owed:`
naming the lane not yet run; `DialectGuardTwinTests` gates both.

- **Aliased `INSERT` target on MySQL.** MySQL's `INSERT` grammar has no
  target-alias slot at all — the 8.0.19+ `AS row_alias` is a separate,
  post-`VALUES` construct — so `InsertInto(new UsersTable("u"))` has no valid
  MySQL spelling (the alias is the same analyzer-invisible constructor
  argument as the SQL Server case). Scoped to `INSERT` alone: MySQL's aliased
  `UPDATE`/`DELETE` targets are valid (live-verified, #255). Anchored by
  `MySqlTests.AliasedInsertTarget_Rejected` executing the raw aliased form
  against a live engine, per the empirical-anchor precedent above.
  Live twins: `AliasedInsertTarget_Rejected` on the MySQL lane.
- **Joined `UPDATE`/`DELETE` on SQL Server without the target re-listed in
  `FROM`.** T-SQL's joined form takes the target's alias from `FROM`, so a
  joined shape that never re-lists the target (a `USING`-only `DELETE`, a
  PostgreSQL-style `UPDATE ... FROM aux`, a direct-join `UPDATE`) has no valid
  T-SQL spelling; the shape lives in value-level builder state the analyzer
  cannot read. PostgreSQL's forms legally omit the re-list, so the guard is
  `Dbms.SqlServer`-scoped. Live twin owed: the SQL Server lane pins only the
  accepted re-listed form (`JoinedUpdateFrom_Executes`).
- **Joined `UPDATE` with the target re-listed in `FROM`, off SQL Server**
  (release audit, pass 2) — the mirror of the guard above. The re-listed form
  makes the lead render as the bare alias, which only T-SQL resolves
  (live-verified rejection on SQLite); the re-listing is instance identity —
  `ReferenceEquals` between the target and a `FROM` element — which the
  analyzer cannot see. A joined `DELETE` stays permissive: its repeated-`FROM`
  form is also MySQL's. Live twins: `JoinedUpdateRelistedTarget_IsRejectedByTheEngine`
  on the SQLite lane.
- **A non-integer constant `ORDER BY` sort key on PostgreSQL and SQL Server**
  (release audit, pass 4; SQL Server added by #523). `OrderBy(2.5)` renders
  `ORDER BY 2.5`, a no-op ordering MySQL 8.0, SQLite 3.50.4 and Oracle XE
  21.3.0 accept (each pinned by `OrderByNonIntegerConstant_IsAcceptedByTheEngine`
  on its lane) while PostgreSQL 16 and SQL Server 2022 reject it outright
  (`non-integer constant in ORDER BY`; `A constant expression was encountered
  in the ORDER BY list, position 1.` — both live-verified); the value is a
  literal the construct-level matrix cannot see, so `SelectBuilder.Validate`
  throws for those two dialects. Oracle's acceptance is why the pass-4 entry's
  SQL Server question stayed open until its lane ran. Live twins:
  `OrderByNonIntegerConstant_IsRejectedByTheEngine` on the PostgreSQL and SQL
  Server lanes, and `OrderByNonIntegerConstant_IsAcceptedByTheEngine` on the
  Oracle, MySQL and SQLite lanes.
- **A negative constant `ORDER BY` ordinal on PostgreSQL, SQLite, and SQL
  Server** (SQL Server added by #523). `OrderBy(-1)` renders `ORDER BY -1`,
  which those three read as a column position and reject (`ORDER BY position -1
  is not in select list` on 16.13; `1st ORDER BY term out of range` on 3.50.4;
  `The ORDER BY position number -1 is out of range of the number of items in
  the select list.` on 2022 — all live-verified), while MySQL reads it as a
  constant expression and orders by nothing (measured on 8.0.46 on its lane)
  and Oracle XE 21.3.0 does the same. Oracle is therefore not claimed, though
  it still rejects the zero ordinal (ORA-01785), as the dialect-blind guard
  has it. The literal is invisible to the construct-level matrix, so
  `SelectBuilder.Validate` throws for `Dbms.PostgreSql`, `Dbms.Sqlite`, and
  `Dbms.SqlServer`. The zero ordinal is a separate,
  dialect-blind guard: no engine resolves position 0, so it is ADR 0007's
  incomplete construct and owes no entry here. Both arms are statement-scoped
  through `FindPart<OrderByClause>()`, in each query block the build renders,
  leaving `OVER (...)`, `WITHIN GROUP` and `GROUP_CONCAT` orderings — where the
  same literal is an expression — untouched.
  Live twins: `OrderByNegativeOrdinal_IsRejectedByTheEngine` on the PostgreSQL,
  SQLite and SQL Server lanes, `OrderByNegativeOrdinalInSubquery_IsRejectedByTheEngine`
  and `OrderByZeroOrdinalInCteBody_IsRejectedByTheEngine` for the nested blocks,
  and `OrderByNegativeOrdinal_IsAcceptedByTheEngine` on the MySQL and Oracle lanes.
- **`DELETE ... USING` on SQL Server** (release audit, pass 5). T-SQL has no
  `USING` form for `DELETE` at all, so the shape has no valid spelling on the
  target; it is builder state (a `DeleteUsingClause` part) the analyzer's
  context-free `Using` key unions with MERGE's support and cannot see. The
  guard exists mainly for its message: the joined-target guard's re-list
  remedy is unreachable from a `Using(...)` chain, so this one names
  `From(...)` first. Live twin owed: the SQL Server lane has no raw
  `DELETE ... USING` probe.
- **A leading `WITH` before `INSERT`, `UPDATE`, or `DELETE` on Oracle**
  (release audit, pass 6). Oracle's DML grammar has no leading
  `subquery_factoring_clause`: the CTE belongs inside the feeding `SELECT`
  (`InsertInto(...).With(...).Select(...)`), so the leading shape has no
  valid spelling on the target and `Validate` throws for `Dbms.Oracle`
  alone, naming the mid-chain form. Anchored on the SQL Language
  Reference's grammar until the lane ran. Live twins:
  `LeadingWithBeforeInsert_IsRejectedByTheEngine` on the Oracle lane.
- **A leading `WITH` before `INSERT` on MySQL** (release audit pass 7). MySQL
  8.0 takes a leading `WITH` before `SELECT`, `UPDATE`, and `DELETE` but not
  before `INSERT` (or `INSERT IGNORE`/`REPLACE`): `WITH c AS (...) INSERT INTO
  t ...` is `ER_PARSE_ERROR` on 8.0.46, live-verified twice in the pass, while
  the mid-chain `INSERT INTO t (...) WITH c AS (...) SELECT ...` runs. The
  Oracle guard above therefore keys on `(dialect, statement)`: Oracle for
  every DML, MySQL for `INSERT`; the message names the mid-chain form. The
  pass-6 entry's "Oracle alone" premise was the under-scoping. Live twins:
  `LeadingWithBeforeInsert_IsRejectedByTheEngine` on the MySQL lane.
- **A bare `OFFSET` on MySQL and SQLite** (release audit pass 8). Both engines
  take `OFFSET` only after `LIMIT` (`SELECT ... OFFSET 1` is `ER_PARSE_ERROR`
  on 8.0.46 and a syntax error on SQLite 3.45, live-verified); PostgreSQL takes
  it alone. The analyzer's `Offset` key is the union of two interfaces, so the
  standalone shape is invisible to it, and `SelectBuilder.Validate` throws at
  `Build(MySql)`/`Build(Sqlite)` when an `OffsetClause` has no `LimitClause`.
  Live twins: `Pagination_OffsetWithoutLimit_IsRejectedByTheEngine` on the
  SQLite and MySQL lanes.
- **A targetless `ON CONFLICT DO UPDATE` on PostgreSQL** (release audit, after
  pass 8). PostgreSQL requires an inference specification or constraint name
  for `DO UPDATE` (`ON CONFLICT DO UPDATE requires inference specification or
  constraint name`, live-verified on 16); SQLite takes the targetless form on
  its last `ON CONFLICT` clause (live-verified on 3.45). The pass-5 guard
  rejected both, on the premise that both engines require the target — the
  regression review of the audit branch caught SQLite's valid statement
  throwing — so `InsertBuilder.Validate` now throws for `Dbms.PostgreSql`
  alone. The pairing is builder state (an `OnConflictClause` with no target
  beside a `DoUpdateSetClause`) the analyzer's `OnConflict` key cannot see.
  Live twins: `Upsert_OnConflictDoUpdateWithoutTarget_IsRejectedByTheEngine`
  on the PostgreSQL lane and
  `Upsert_OnConflictDoUpdateWithoutTarget_IsAcceptedByTheEngine` on the
  SQLite lane.
- **A leading `WITH` before `MERGE` on Oracle** (#521 item 3). Oracle's
  `merge_statement` carries no `subquery_factoring_clause` either
  (live-verified on XE 21.3.0 and Free 23ai), so the leading shape has no
  valid spelling there — but the remedy the two entries above name is not
  available to it: `MERGE` has no feeding `SELECT` to carry the CTE, and its
  own source slot takes a subquery instead, so the CTE goes inside the one
  `Using(...)` names. That is why this is a guard of its own rather than a
  fourth statement in the Oracle message: a shared message would misname the
  way out. `MergeBuilder.Validate` throws for `Dbms.Oracle` alone —
  PostgreSQL and SQL Server take the leading form, and MySQL and SQLite have
  no `MERGE` for one to lead, which leaves those two to SQLA0100 rather than
  to any guard. The recursive pairing needs no guard at all: `WithRecursive`
  hands back a state that declares no `MergeInto`, so the chain no engine
  accepts does not compile. Live twins:
  `LeadingWithBeforeMerge_IsRejectedByTheEngine` on the Oracle and Oracle23ai
  lanes, `CteInsideMergeUsingSubquery_IsAcceptedByTheEngine` for the remedy on
  the Oracle lane, and `LeadingWithBeforeMerge_IsAcceptedByTheEngine` on the
  PostgreSQL and SQL Server lanes.
