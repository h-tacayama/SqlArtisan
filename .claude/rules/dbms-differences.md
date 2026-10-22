---
description: Classifying a DBMS syntax difference — dialect-layer token vs separate per-dialect construct
paths:
  - "src/SqlArtisan/Internal/SqlBuilder/DbmsDialect/**/*.cs"
  - "src/SqlArtisan.Analyzers/DialectMatrix.cs"
  - "src/SqlArtisan.Analyzers/ContextRules.cs"
  - "src/SqlArtisan.Analyzers/ArgumentValueValidity.cs"
  - "src/SqlArtisan.Analyzers/DatepartValidity.cs"
  - "tests/SqlArtisan.IntegrationTests/Infrastructure/MatrixSweepCatalog.cs"
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

**A context-conditional swap stays token-level only when its
meaning-changing contexts are enumerable and excluded.** SQLite 3.50.4, the
lane's engine, resolves an aliased target in `RETURNING` by its table name
alone, so
`ReturningIgnoresTargetAlias` swaps `"u".id` for `users.id` (#595). The swap
changes meaning in a subquery or CTE body whose FROM or JOIN exposes either
name, which is the `WITH ROLLUP` trap. Under the table name, `users.id` binds
the inner relation. Under the alias, the `"u".id` the author wrote already
binds the inner relation, and the swap would send it to the target. The swap
stays token-level only because those scopes can be found mechanically, by a
relation's exposed name, and the swap is withheld there, so the author's
spelling stands. A swap whose meaning-changing contexts cannot be enumerated
is construct-level.

A construct that simply does not exist on a DBMS needs no flag at all: emit
faithfully and leave availability to the database and the analyzer (ADR 0003) —
do not gate it at `Build` time.

**A token the engine reads as something else is unavailable too, and its
cell is `false`** — `||` (logical OR on MySQL), `<=>` (MySQL's NULL-safe
equality, not pgvector's cosine distance), the foreign `FORMAT()` of MySQL and
SQLite, T-SQL's value-first `LOG(x, b)`, and `MINUS`, which SQL Server reads as
a table alias before a second statement. The engine runs the text, so nothing
fails; the report is the only layer that sees the meaning change, at the price
of a report on a deliberate use of the engine's own reading, which a
suppression at the site removes. Such a cell needs a sweep `NegativeSkip`
naming the other meaning, a per-engine twin that asserts that meaning, and, on
its own, a `[!NOTE]` docs callout, since the analyzer catches it (`docs-style.md`
§ "Hazard callouts"); #681 tracks the cells that lack them. The same function
with engine-defined semantics stays `true` and is a docs matter (ADR 0003):
`LOG(x)`'s base, `STDDEV`'s population or sample. `GroupConcat(expr, sep)` on
MySQL, which reads `sep` as another value, belongs to the class but stays
`true`: its arity-2 key is a union with the inline-`ORDER BY` and `SEPARATOR`
overloads MySQL runs (the `DialectMatrix` key-collision caveat), so its
`[!WARNING]` in `docs/functions.md` is the record.

**A sweep negative is rejected by its key's own construct.** `MatrixSweepTests`
counts any `DbException` as the rejection, so a case whose `false` engine stops
first at something else the statement needs — an argument
(`Interval(30, DateTimePart.Day)` under `DateAdd`), a host clause (`LIMIT` under
`Offset`, `MERGE` under `Using`) — tests no cell. A case that would still fail
once the engine accepts the key, such as `WithRecursive` with no column list on
Oracle, tests the cell until then and leaves it `false` unseen once a release
does. Where the API can emit a statement that isolates
the key, the case uses it, per engine if need be (`Build` receives the engine);
where the key cannot be emitted without a construct that is itself `false` on
that engine (a `MERGE` branch step, `DoNothing` after `OnConflict`, `Listagg`
before its `WITHIN GROUP`), the case says so. #681 tracks the cases that do not
yet.

Three further classes look like dialect differences but belong in **neither**
the dialect layer nor a plain matrix entry (the first two identified in the
#225 triage, the third by #528/#529):

- **Version-bounded availability → docs note + a #232 interval seed.** The
  matrix asserts against one pinned engine version (`VerifiedAgainstVersion`),
  so a fact that flips at a version boundary — `EXCEPT`/`INTERSECT` on
  Oracle 21c, `CONCAT`/`||` on SQLite 3.44, MySQL 8.0.16 / 8.0.19 / 8.0.20,
  `DATETRUNC` on SQL Server 2022 — is recorded as a docs version note and
  registered as an interval-annotation seed on #232, never as an
  `IDbmsDialect` member.
- **Context-bounded validity → an analyzer context rule (SQLA0102, ADR
  0013).** A construct valid in one syntactic context and rejected in another
  on the *same* engine — MySQL's `LIMIT` inside `IN`/`ANY`/`ALL`/`SOME`
  subqueries, or a joined `UPDATE`/`DELETE` spelling written for the wrong
  engine — cannot be expressed by the construct-level matrix at all. Add a
  rule to `src/SqlArtisan.Analyzers/ContextRules.cs` with a primary source and a
  live rejection proof in the per-engine integration tests, plus the docs
  note with the workaround. A dialect that accepts the position from some
  version on gets a floor beside the rule's dialect list (ADR 0015, "Context
  rules take the same floor"), never a second rule. A restriction with no API
  surface to anchor on (Oracle's `PRIOR` outside `CONNECT BY` — CONNECT BY is
  wontfix per ADR 0010) stays a docs/ADR note only.
- **Value-bounded validity → an analyzer value-domain rule (SQLA0104, ADR
  0022).** A construct the engine has, with one *argument value* it rejects —
  a `DateTimePart` outside a function's grammar, MySQL's match-parameter
  alphabet having no `'x'`, a negative row count — which the matrix cannot key
  on either. Add the cell to `DatepartValidity.cs` (dateparts) or
  `ArgumentValueValidity.cs` (the rest), with a primary source, a live
  rejection proof, and the acceptance twin that proves the probe was
  well-formed. A value the engine runs with another meaning than the member's
  stays out of the accepted list, so it reports, as a token read as something
  else does; a twin asserting that meaning stands in for the rejection proof,
  and no sweep `NegativeSkip` applies, since the sweep reaches no value cell
  (PostgreSQL's `EXTRACT(MICROSECOND)`, #684). A value no engine accepts is
  ADR 0012's eager guard instead only when its domain is closed (condition 3):
  a vendor's own accepted-value
  list keeps the cell even where no engine accepts the value today, as
  `DATE_TRUNC('day_hour', ts)`, which PostgreSQL 16.15, the one engine with
  `DATE_TRUNC`, rejects (#661). An engine nobody has measured is left out,
  never assumed to reject.

No `SqlPart` names `Dbms` at all — `DialectBranchSweepTests` sweeps the two
`SqlPart/` trees for the type name, so a node reads a difference through
`IDbmsDialect` or not at all. The builders' `Validate(Dbms)` hook is where a
resolved target is legitimately read, and `SqlBuildingBuffer.Dbms` exists to
carry it there.

Before adding anything to `IDbmsDialect` or `DialectMatrix`, walk the classes
above in order: token-level → construct-level → plain unavailability (a token
the engine reads as something else included) → version-bounded →
context-bounded → value-bounded.

An engine-rejection claim written in prose — a comment, an XML doc, a docs
page — names the engine version it was observed on and has a live twin, or is
not made: the "MySQL rejects `ESCAPE ?`" rationale stood in three places for
months after 8.0.46 accepted it (release audit pass 8). State the grammar
reason the code relies on instead (a position that takes a constant), which
holds whether or not any engine still rejects the alternative.
