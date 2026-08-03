# ADR 0017 — Join predicate completeness: rejecting an omitted `ON`/`USING` two dialects silently reinterpret as `CROSS JOIN`

**Status:** Accepted

## Context

`ISelectBuilderJoin` — the state after `InnerJoin`/`LeftJoin`/`RightJoin`/
`FullJoin`/`JoinLateral` — used to extend `ISqlBuilder` and `IForUpdate`, so a
join with no following `.On(...)`/`.Using(...)` could still reach `Build()` or
`ForUpdate()` (#400). Dropping both base interfaces (#420) makes the omission a
compile error, the same "pending type" mechanism ADR 0007 already prescribes
for an incomplete construct.

ADR 0007's dividing test is literal: *is there any supported dialect, in any
configuration, where this exact text is valid SQL? If no → incomplete → reject.
If yes-somewhere → dialect availability → permissive.* The #420 review checked
this construct-by-construct across all five dialects and found the test does
**not** cleanly say "no" for two of the five members:

| Member | `ON`/`USING` omitted |
|---|---|
| `LeftJoin`, `RightJoin`, `FullJoin` | Syntax error on every dialect it is valid on at all — MySQL, Oracle, PostgreSQL, SQLite, and SQL Server all require an explicit predicate for an outer join, because "which left row is unmatched" is undefined without one. Already correctly "incomplete" under ADR 0007 as written; no change of category needed. |
| `InnerJoin` | Syntax error on Oracle, PostgreSQL, and SQL Server (`ON`/`USING` required by their ANSI join grammar). **Silently accepted on MySQL and SQLite**, both of which document treating a bare `INNER JOIN`/`JOIN`/comma-join identically: SQLite's own grammar notes list `CROSS JOIN`, `INNER JOIN`, `JOIN`, and the comma form as the omission-tolerant set, evaluating to "simply the cartesian product"; MySQL's manual states `JOIN`, `CROSS JOIN`, and `INNER JOIN` are "syntactic equivalents" and that `INNER JOIN` with no join condition is "semantically equivalent" to a comma-join, i.e. a cartesian product. |
| `JoinLateral` | Valid only on MySQL, Oracle, and PostgreSQL (`DialectMatrix.cs`; SQLite and SQL Server don't support it at all). Oracle and PostgreSQL require the predicate; MySQL silently accepts the omission for the same reason as plain `JOIN` above. |

So for `InnerJoin` and `JoinLateral`, the omission is — by the literal test —
*dialect availability*, which ADR 0007 says the library must never throw for.
Yet nobody who writes `.InnerJoin(x)`/`.JoinLateral(x, alias)` with no
following predicate is choosing MySQL/SQLite's cartesian-product reading on
purpose: the interface's own doc says "supply its `ON` predicate," and the
construct that *does* mean "I want the unfiltered cartesian product" already
has its own explicit, faithfully-emitted name — `CrossJoin` — which every
dialect accepts (`IJoinOperator.CrossJoin`, "the unfiltered Cartesian product,
so no `ON` follows"). PostgreSQL's own documentation states `CROSS JOIN` "is
equivalent to `INNER JOIN ON (TRUE)`" — confirming that MySQL/SQLite's lenient
reading of a bare `INNER JOIN`/`JOIN` is not a distinct construct at all, only
an unlabeled spelling of one the library already exposes under its own name.

## Decision

The library **rejects a missing join predicate on `InnerJoin` and
`JoinLateral`**, on every dialect, via the same compile-time mechanism as any
other incomplete construct (`ISelectBuilderJoin` carrying no `ISqlBuilder`/
`IForUpdate`) — not a `Validate(Dbms)` runtime guard, and not left to the
opt-in analyzer.

This is a **bounded exception to ADR 0007**, admitted only because all three
hold together:

- **The accepting dialects don't treat the omission as a distinct construct.**
  MySQL and SQLite's own documentation describes the omitted-predicate form as
  *the same thing as* `CROSS JOIN` under a different spelling, not a construct
  with independent meaning — unlike ordinary dialect availability (`CUBE` on
  MySQL), where the two dialects genuinely disagree about a real feature.
- **No caller intentionally targets that reading.** `CrossJoin` is the
  construct's real, explicit, faithfully-emitted name on every dialect,
  including MySQL and SQLite. A caller who wants the cartesian product writes
  `CrossJoin`; one who reaches `Build()`/`ForUpdate()` from `InnerJoin`/
  `JoinLateral` with no predicate has an unfinished chain, not a deliberate
  choice — mirroring exactly the "no valid spelling was actually intended
  here" logic ADR 0011 uses for its own bounded exception.
- **Fits the deterministic-guard mission (ADR 0010).** A missing `.On(...)` is
  the class of mistake an AI-assisted or hastily-written chain produces
  silently, and MySQL/SQLite's leniency means the database will not catch it
  either — the two dialects where the guard matters most are exactly the two
  where nothing else would.

**Scope, precisely:** only `InnerJoin` and `JoinLateral`. `LeftJoin`,
`RightJoin`, and `FullJoin` were never in question — no supported dialect
accepts their omitted-predicate form at all, so they remain plain "incomplete"
under ADR 0007 as originally written, unamended. This ADR does not introduce a
new mechanism; it extends the existing "incomplete construct" category's
membership by two, with the justification above standing in for "no dialect
accepts the bare token" wherever that literal test would otherwise say
"permissive."

## Consequences

- **`ISelectBuilderJoin`'s interface-hierarchy fix needed no dialect split.**
  Because the same compile-time mechanism now covers both the originally
  "incomplete" members (`LeftJoin`/`RightJoin`/`FullJoin`) and the two carved
  out here (`InnerJoin`/`JoinLateral`), one type change serves both without a
  `Validate(Dbms)` branch — the classification argument lives in this ADR: the
  mechanism itself doesn't need to know which members are outer joins and
  which are dialect-lenient.
- **Narrow, by construction.** This is not a license to block any construct
  that has a "better" alternative spelling — see `public-api-design.md`'s
  `COUNT(*)` lesson: knowledge encoded as an API hole is invisible and
  unexplained. The bar here is the same three-part test above, not "a nicer
  spelling exists"; a future proposal must clear all three, not just the last
  one.
- **Grammar-unverified against a live engine.** The MySQL/SQLite leniency and
  the PostgreSQL/Oracle/SQL Server rejection are sourced to each vendor's own
  documentation (cited above), not to a live probe in this environment (no
  Docker daemon available at decision time). Confirm against the live
  integration matrix — MySQL and SQLite specifically — per the #414 epic's
  outstanding "run the integration matrix" item, alongside its other
  grammar-unverified tags.
- Complements ADR 0007 and ADR 0011; supersedes neither. See #400, #420.
