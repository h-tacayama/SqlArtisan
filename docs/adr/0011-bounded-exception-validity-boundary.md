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

> `SQL Server does not support aliasing the target of an INSERT, UPDATE, or DELETE statement; use an unaliased target table.`

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
  narrow or point at the joined form instead.
- **The premise is empirically anchored,** not asserted: an integration test
  (`SqlServerTests.AliasedDmlTarget_Rejected`) executes the raw emitted
  form against a live SQL Server and confirms the rejection, alongside the
  unaliased form succeeding — so the guard rests on a verified engine fact, not
  on grammar folklore.
- **Complements, does not overlap, #239.** #239 guards the *unaliased correlated*
  target (a silent tautology) via the owning-table model; this guards the
  *aliased* target on the one dialect where aliasing it is invalid. Together they
  make the correlated-DML surface fail loudly on every dialect.
