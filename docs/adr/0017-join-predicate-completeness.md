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
this per member and per dialect, live, against real engines: `sqlite3` CLI
3.45.1, PostgreSQL 16, and MySQL 8.0.46, installed and run directly in the
review environment; then Oracle 21c and SQL Server 2022, run via the project's
own Testcontainers-backed integration suite in CI (`OracleTests.
OmittedJoinPredicate_Rejected`, `SqlServerTests.OmittedJoinPredicate_Rejected`,
alongside a `CrossJoin_ProducesCartesianProduct` control in each — both suites
passed in full, 78/78 and 77/77 non-skipped). The live run **corrected the
first draft of this ADR**: an initial pass, sourced to web-search summaries of
SQLite's documentation, claimed `LeftJoin`/`RightJoin`/`FullJoin` were syntax
errors without a predicate on every dialect. Running the actual engine
falsified that — SQLite accepts the omission on all three, not just
`InnerJoin`. The live results, now covering all five supported dialects:

| Engine | `INNER`/bare `JOIN` omitted | `LEFT`/`RIGHT`/`FULL JOIN` omitted | `CROSS JOIN` (control) |
|---|---|---|---|
| SQLite 3.45.1 | Accepted — cartesian product | **Accepted — cartesian product**, identical row-for-row to the `INNER`/`CROSS` case; no implicit same-name-column matching is attempted even though both test tables shared a column name | Accepted |
| PostgreSQL 16 | `ERROR: syntax error at or near ";"` | `ERROR: syntax error at or near ";"` | Accepted |
| MySQL 8.0.46 | Accepted — cartesian product | `ERROR 1064 (42000)` (`FullJoin` isn't a MySQL construct at all — separate, pre-existing dialect-availability fact) | Accepted |
| Oracle 21c (Testcontainers) | Rejected (`ORA` syntax error) | Rejected | Accepted |
| SQL Server 2022 (Testcontainers) | Rejected (T-SQL syntax error) | Rejected | Accepted |

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
- **A `Build()`-time backstop joined the compile-time mechanism** (release
  audit, pass 3): a caller holding a *pre-join* stage reference can call a
  join member and then build from the held reference, bypassing the pending
  type entirely — the chain compiles and the dangling join renders as the
  silent cartesian product this ADR rejects. The duplicate-clause walk in
  `SqlBuilderBase` therefore also throws when a conditioned join
  (`InnerJoin`/`LeftJoin`/`RightJoin`/`FullJoin`/`JoinLateral`, and the DML
  builders' joined forms) is not followed by its `ON`/`USING`. The
  compile-time pending type stays primary; the backstop is dialect-blind,
  matching the uniform scope above, and runs from every nested render as
  well (release audit, pass 4): a subquery, CTE body, or scalar item never
  passes through `BuildCore`, and a dangling join is no less a cartesian
  product one level down.
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
- **No engine in this ADR's table is grammar-unverified anymore.** Oracle and
  SQL Server were not reachable in the local review environment (no Docker
  daemon there), but the same claim is now anchored by
  `OracleTests.OmittedJoinPredicate_Rejected` and
  `SqlServerTests.OmittedJoinPredicate_Rejected` running against the project's
  own Testcontainers-backed CI, executed on demand for this ADR and passing in
  full — closing the one item this cluster still owed the #414 epic's
  "run the integration matrix" backlog for this construct.
- Complements ADR 0007 and ADR 0011; supersedes neither. See #400, #420.
