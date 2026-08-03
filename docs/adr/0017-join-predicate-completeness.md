# ADR 0017 — Join predicate completeness: rejecting an omitted `ON`/`USING` that some dialects silently reinterpret as `CROSS JOIN`

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
this per member and per dialect, live, against real engines (`sqlite3` CLI
3.45.1, PostgreSQL 16, MySQL 8.0.46 — installed and run directly in the review
environment; Oracle and SQL Server were not reachable there and remain sourced
to each vendor's own ANSI-join documentation, unchanged from prior belief). The
live run **corrected the first draft of this ADR**: an initial pass, sourced to
web-search summaries of SQLite's documentation, claimed `LeftJoin`/`RightJoin`/
`FullJoin` were syntax errors without a predicate on every dialect. Running the
actual engine falsified that — SQLite accepts the omission on all three, not
just `InnerJoin`. The live results:

| Engine | `INNER`/bare `JOIN` omitted | `LEFT`/`RIGHT`/`FULL JOIN` omitted | `CROSS JOIN` (control) |
|---|---|---|---|
| SQLite 3.45.1 | Accepted — cartesian product | **Accepted — cartesian product**, identical row-for-row to the `INNER`/`CROSS` case; no implicit same-name-column matching is attempted even though both test tables shared a column name | Accepted |
| PostgreSQL 16 | `ERROR: syntax error at or near ";"` | `ERROR: syntax error at or near ";"` | Accepted |
| MySQL 8.0.46 | Accepted — cartesian product | `ERROR 1064 (42000)` (`FullJoin` isn't a MySQL construct at all — separate, pre-existing dialect-availability fact) | Accepted |
| Oracle, SQL Server | Not run live; each vendor's ANSI-join reference documents `ON`/`USING` as mandatory for every listed join type, `CROSS JOIN` excepted | (same, not run live) | — |

So the omission is accepted **somewhere** for every one of the five members —
just not the same "somewhere" for each: `InnerJoin`/`JoinLateral` (which emits
a bare `JOIN` keyword) are lenient on MySQL and SQLite; `LeftJoin`/`RightJoin`/
`FullJoin` are lenient on SQLite alone. By the literal test, every member is
therefore *dialect availability*, which ADR 0007 says the library must never
throw for.

Yet nobody who writes `.InnerJoin(x)`/`.LeftJoin(x)`/`.JoinLateral(x, alias)`
with no following predicate is choosing SQLite's (or MySQL's) cartesian-product
reading on purpose: the interface's own doc says "supply its `ON` predicate,"
and the construct that *does* mean "I want the unfiltered cartesian product"
already has its own explicit, faithfully-emitted name — `CrossJoin` — which
every dialect accepts unconditionally (`IJoinOperator.CrossJoin`, "the
unfiltered Cartesian product, so no `ON` follows"; confirmed live above,
identical output to the lenient dialects' omitted-predicate reading).
PostgreSQL's own documentation states `CROSS JOIN` "is equivalent to
`INNER JOIN ON (TRUE)`" — and the SQLite live run goes further than any
documentation claim: the omitted-predicate `LEFT`/`RIGHT`/`FULL JOIN` output is
not merely *equivalent to* `CROSS JOIN`, it is byte-for-byte identical to it,
row for row, with no outer-join NULL-padding attempted at all.

## Decision

The library **rejects a missing join predicate on `InnerJoin`, `LeftJoin`,
`RightJoin`, `FullJoin`, and `JoinLateral`**, on every dialect, via the same
compile-time mechanism as any other incomplete construct (`ISelectBuilderJoin`
carrying no `ISqlBuilder`/`IForUpdate`) — not a `Validate(Dbms)` runtime guard,
and not left to the opt-in analyzer.

This is a **bounded exception to ADR 0007**, admitted only because all three
hold together:

- **The accepting dialects don't treat the omission as a distinct construct.**
  Confirmed live: SQLite's output for every omitted-predicate join type is
  identical to its `CROSS JOIN` output; MySQL's manual states `JOIN`,
  `CROSS JOIN`, and `INNER JOIN` are "syntactic equivalents" and that
  `INNER JOIN` with no join condition is "semantically equivalent" to a
  comma-join, i.e. a cartesian product. This is unlike ordinary dialect
  availability (`CUBE` on MySQL), where the two dialects genuinely disagree
  about a real, independent feature.
- **No caller intentionally targets that reading.** `CrossJoin` is the
  construct's real, explicit, faithfully-emitted name on every dialect,
  including the lenient ones. A caller who wants the cartesian product writes
  `CrossJoin`; one who reaches `Build()`/`ForUpdate()` from any of the five
  members with no predicate has an unfinished chain, not a deliberate choice —
  mirroring exactly the "no valid spelling was actually intended here" logic
  ADR 0011 uses for its own bounded exception.
- **Fits the deterministic-guard mission (ADR 0010).** A missing `.On(...)`/
  `.Using(...)` is the class of mistake an AI-assisted or hastily-written
  chain produces silently, and the lenient dialects mean the database will not
  catch it either — exactly where a deterministic guard matters most.

**Scope, precisely:** all five members of `ISelectBuilderJoin`, uniformly —
not a subset. The distinction the first draft of this ADR drew (some members
"already incomplete everywhere," others needing the exception) does not
survive live testing: SQLite is lenient across all five. One mechanism, one
justification, no per-member split.

## Consequences

- **`ISelectBuilderJoin`'s interface-hierarchy fix needed no dialect split.**
  One type change (dropping the two base interfaces) covers every member
  uniformly, with no `Validate(Dbms)` branch — the classification argument
  lives entirely in this ADR, not in the mechanism.
- **Narrow, by construction.** This is not a license to block any construct
  that has a "better" alternative spelling — see `public-api-design.md`'s
  `COUNT(*)` lesson: knowledge encoded as an API hole is invisible and
  unexplained. The bar here is the same three-part test above, not "a nicer
  spelling exists"; a future proposal must clear all three, not just the last
  one.
- **A web-search summary of vendor documentation was wrong; the live engine
  was the correction.** The first draft of this ADR trusted a search-engine
  synthesis of SQLite's own docs over running SQLite — and shipped a false
  claim as a result. Treat this as the standing argument for why a grammar
  claim backing a design decision gets run against the real engine before it's
  written down, not sourced to a summary of a summary.
- **Oracle and SQL Server remain grammar-unverified against a live engine** —
  neither was reachable in the review environment (no Docker daemon, no
  feasible from-scratch install). Both vendors' own ANSI-join reference
  documentation states `ON`/`USING` is mandatory for every listed join type
  except `CROSS JOIN`, and nothing in this review contradicts that, but given
  what just happened with SQLite, treat it as unconfirmed until the live
  integration matrix — including these two engines specifically — runs, per
  the #414 epic's outstanding "run the integration matrix" item.
- Complements ADR 0007 and ADR 0011; supersedes neither. See #400, #420.
