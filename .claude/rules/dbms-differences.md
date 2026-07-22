---
description: Classifying a DBMS syntax difference — dialect-layer token vs separate per-dialect construct
paths:
  - "src/SqlArtisan/Internal/SqlBuilder/DbmsDialect/**/*.cs"
---

# Handling a DBMS syntax difference

Before adding a capability flag or per-dialect spelling to `IDbmsDialect`,
classify the difference (ADR 0001/0002). Getting this wrong ships a **silent
semantic rewrite** — the failure the `WITH ROLLUP` example below caused.

- **Token-level → dialect layer (belongs here).** A *substitution that does not
  change the statement's shape or how the construct composes*: identifier
  quoting, the parameter marker, alias / excluded-row casing, a clause's own
  spelling, the pagination keyword in its fixed slot. The author writes one call;
  `Build(Dbms)` swaps the token in place.
- **Construct-level → separate per-dialect public API (NOT a dialect flag).** A
  genuinely different grammar for the same intent — a different *syntactic
  position* or *composition rule*, even when the result set is identical. Expose
  it as its own method (like UPSERT `OnConflict` / `OnDuplicateKeyUpdate`), never
  as one shared method rewritten per dialect.

**The test that catches the trap:** does the per-dialect form **compose
differently**? If swapping it in changes meaning when combined with other items,
or it only renders in one position, it is construct-level — a dialect flag would
make `Build` silently rewrite the author's SQL, violating ADR 0001.

> Worked example — do not repeat. `ROLLUP(a, b)` is one grouping element among
> many; MySQL's `WITH ROLLUP` is a suffix over the whole `GROUP BY` list. A
> `UsesWithRollupSuffix` flag that rewrote `Sql.Rollup(...)` per dialect turned
> `GroupBy(x, Rollup(a, b))` into a *different grouping* on MySQL (rolled up
> `x, a, b` instead of `a, b` within each `x`), and produced invalid SQL with a
> trailing item. Fix: `Rollup(...)` always emits the standard function form;
> MySQL's suffix is the separate `.GroupBy(...).WithRollup()` step.

A construct that simply does not exist on a DBMS needs no flag at all: emit
faithfully and leave availability to the database and the analyzer (ADR 0003) —
do not gate it at `Build` time.

Two further classes look like dialect differences but belong in **neither**
the dialect layer nor a plain matrix entry (both identified in the #225
triage):

- **Version-bounded availability → docs note + a #232 interval seed.** The
  matrix asserts against one pinned engine version (`VerifiedAgainstVersion`),
  so a fact that flips at a version boundary — `EXCEPT`/`INTERSECT` on
  Oracle 21c, `CONCAT`/`||` on SQLite 3.44, MySQL 8.0.16 / 8.0.19 / 8.0.20,
  `DATETRUNC` on SQL Server 2022 — is recorded as a docs version note and
  registered as an interval-annotation seed on #232, never as an
  `IDbmsDialect` member.
- **Context-bounded validity → an analyzer context rule (SQLA0004, ADR
  0013).** A construct valid in one syntactic context and rejected in another
  on the *same* engine — MySQL's `LIMIT` inside `IN`/`ANY`/`ALL`/`SOME`
  subqueries, MySQL's `GROUPING()` outside a `WITH ROLLUP` query — cannot be
  expressed by the construct-level matrix at all. Add a rule to
  `src/SqlArtisan.Analyzers/ContextRules.cs` with a primary source and a
  live rejection proof in the per-engine integration tests, plus the docs
  note with the workaround. A restriction with no API surface to anchor on
  (Oracle's `PRIOR` outside `CONNECT BY` — CONNECT BY is wontfix per ADR
  0010) stays a docs/ADR note only.

Before adding anything to `IDbmsDialect` or `DialectMatrix`, walk the classes
above in order: token-level → construct-level → plain unavailability →
version-bounded → context-bounded.
