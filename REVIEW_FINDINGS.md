# SqlArtisan Code Review: Full Codebase

## Verdict

**Not mergeable** — on workflow grounds, before any code finding is considered: **69 of 135 chunks (51%) never returned a result and were never reviewed**, which this workflow treats the same as a failing gate. Entire subsystems have zero review coverage: all of `SqlArtisan.Analyzers`, all of `TableClassGen`, both companion packages (ArrayBind, Dapper), all public surface types (`SqlBuilder/`, `SqlPart/`, `Metadata/`), the entire test tree, and the benchmarks. All build/test/format gates passed (build clean; 841 unit, 620 analyzer, 145 TableClassGen tests green; format clean), and the reviewed half of the codebase produced a well-verified set of findings — but a "mergeable" verdict cannot be issued over a silent 51% coverage hole. Additionally, 18 of the 66 completed chunks (all Public API and Function-implementation parts 1–13) were never adversarially verified; their findings are treated as unconfirmed below.

## Summary

The 66 completed chunks — 48 of them adversarially verified against primary sources and live harnesses — converge on one dominant defect class: **silent emission of invalid SQL for permitted inputs**, concentrated in empty-collection guard gaps (`SET`/`VALUES`/`DO UPDATE SET` with zero elements), typestate holes that let grammatically incomplete or dialect-impossible chains build (`JOIN` without `ON`, `OUTPUT`+`RETURNING`, `UPDATE ... FROM` on MySQL/Oracle), and a culture-dependence bug in `ORDER BY` numeric literals. The function-node and clause layers are otherwise in strong shape — dozens of nodes verified clean against all five dialects with faithful emission, correct escaping, and no dialect branching. Verification also caught and refuted several review claims (including one fabricated/mislabeled probe transcript), which are listed explicitly rather than silently dropped.

## Findings by Severity

### MUST FIX

**MF-0. Workflow: 69 chunks silently unreviewed** (blocker). The failed chunks cover Internal SQL parts 22, 24–30, all 7 Public-surface-types parts, both Companion-packages parts, all 5 Analyzers parts, all 6 TableClassGen parts, all 36 Tests parts, and all 5 Benchmarks parts. These files were in scope and were never looked at. Re-run before any merge decision on the audited state.

**MF-1. Cross-chunk pattern — empty-assignment/params guard gaps emit invalid SQL** (verified across 6 chunks: Builders 5, 6, 7, 14, 16, 17; Internal parts 2, 5, 6, 11). The same defect shape, all CONFIRMED with live probes, at every one of these entry points — none guarded, all silently building broken SQL:
- `UpdateBuilder.Set()` (both plain and joined) → `UPDATE t SET ` (`UpdateBuilder.cs:115-125`, `UpdateSetClause.cs:14-30`)
- `InsertBuilder.Set()` → `INSERT INTO users () VALUES ()` (`InsertBuilder.cs:66-70`) — confirmed rejected by live SQLite
- `InsertBuilder.Values()` single-row zero-width → `VALUES ()` on all 5 dialects (`InsertBuilder.cs:78-140`)
- `DoUpdateSet()` → `ON CONFLICT (id) DO UPDATE SET ` (`InsertBuilder.cs:31-35`, `DoUpdateSetClause.cs`)
- `OnDuplicateKeyUpdate()` → trailing `ON DUPLICATE KEY UPDATE ` (`InsertBuilder.cs:49-54`)
- `MergeBuilder.ThenUpdateSet()` (both branches) → `WHEN MATCHED THEN UPDATE SET ` (`MergeUpdateSetClause.cs:14-19`)
- `SelectBuilder.From()` → bare `SELECT name FROM ` — while `UpdateBuilder`/`DeleteBuilder.From()` correctly guard the identical shape (`SelectBuilder.cs:156-160` vs. `DeleteBuilder.cs:24-26`)

Fix: `CollectionGuard.ThrowIfEmpty` at each entry point (the established pattern already used by `Output()`/`Returning()` in the same files) plus exact-message tests. Note: one verifier flagged the Builders-5 framing ("every other params clause guarded") as OVERREACH — the guard is actually the *minority* case in `InsertBuilder`, so fix all sites together, not `DoUpdateSet` alone.

**MF-2. Cross-chunk pattern — typestate/inheritance holes let invalid chains build** (all CONFIRMED):
- `ISelectBuilderJoin : ISqlBuilder, IForUpdate` (`ISelectBuilderJoin.cs:6`) lets `InnerJoin(...)` reach `Build()`/`ForUpdate()` without `.On(...)`/`.Using(...)` — executed against live SQLite as a silent cartesian product (Builders 10, High). Precedent fix: `IMergeBuilderUsing` has no base interfaces.
- `.Output(...).Returning(...)` type-checks via `IReturning` inheritance and builds `DELETE ... OUTPUT DELETED.id RETURNING id` — valid on no dialect (verifier-added DEFECT, Builders 3, Medium; same architecture gap exists in `IUpdateBuilderOutputInto`).
- Direct `UPDATE target JOIN aux ON ... SET` builds on PostgreSQL/Oracle/SQLite/SQL Server, invisible to the analyzer because `MatrixKey("InnerJoin")` conflates SELECT and UPDATE joins (Builders 14, High); and verifier-added: `IUpdateBuilderSet.From(...)` builds `UPDATE ... FROM` on MySQL and Oracle, which have no such clause, contradicting its own docstring (DEFECT, High). Both need `Validate(Dbms)` branches.
- Held pre-`Build()` builder references silently cross-contaminate: calling `.Where(...)` twice on a held `ISelectBuilderFrom` emits `WHERE ... WHERE ...`, and branching a held reference into `.Where(...)` and `.GroupBy(...)` leaks both clauses into the first branch's SQL (Builders 13, CONFIRMED and *broadened* by the verifier — a per-clause-type "once only" guard alone will not close the cross-branch leak; fix needs per-instance isolation).

**MF-3. INSERT VALUES width never checked against the declared column list** — `INSERT INTO users (id, name) VALUES (:0)` and `INSERT INTO users (id) VALUES (:0, :1)` both build silently, in both mismatch directions (Builders 7, CONFIRMED High). Complete fix requires exposing the column count from `InsertIntoClause`.

**MF-4. Culture-dependent `ORDER BY` numeric literals** — `OrderByItemResolver.cs:41` uses `ToString()` instead of the established `ToInvariantString()`; under `de-DE`, `OrderBy(2.5)` emits `ORDER BY 2,5` — silent wrong SQL (Internal 12, CONFIRMED High).

**MF-5. Cross-chunk pattern — null-element NREs in resolvers** (Internal 12, 17, CONFIRMED): `OrderByItemResolver.Resolve(object)` (`OrderBy(col, null)`), `UpdateSetClause.Parse` (`Set(null)` and `Set(cond, null)`), `SelectItemResolver.Resolve(object)` (null select item, reaching all 10 call sites incl. `Output()`/`Returning()`), and the duplicated `UpsertAssignmentResolver` all crash with raw `NullReferenceException` instead of the sibling resolvers' `ArgumentNullException: "...Use Sql.Null to represent SQL NULL."` One verifier noted OVERREACH in the "every sibling guards" framing (the convention is not exceptionless), but the gaps are real; fix them as a family.

**MF-6. `FrameBound.Preceding/Following` accepts negative offsets** — literal-embedded, call-site-fixed, rejected by every engine (`frame starting offset must be a non-negative integer` on live SQLite); exactly ADR 0012's eager-guard shape, matching the existing `PercentileFractionGuard` precedent (Internal 13, CONFIRMED High).

**MF-7. `StringAggFunction` null/empty separator → NRE at `Build()`** (`StringAggFunction.cs:17-25`) — the only mandatory-string node in its shape with no `StringGuard.ThrowIfNullOrEmpty` (Function 20, CONFIRMED Medium).

**MF-8. Subquery as INSERT value unresolvable** — `InsertValueResolver` lacks the `ISubquery → ScalarSubquery` branch its siblings (`ExpressionResolver`, `SelectItemResolver`) have, so `VALUES (1, (SELECT ...))` throws `Invalid type for InsertValue` (Internal 5, CONFIRMED Medium; also reached by MERGE via `MergeBuilder`).

**MF-9. Doc/dialect-matrix contradictions** (all CONFIRMED against `DialectMatrix.cs`):
- `IPagination.Limit` doc directs SQL Server users to `FetchFirst`, which the matrix marks `sqlServer: false`; the same file's `FetchFirst` doc says the opposite (Builders 12 — verifier reclassified as a straight Medium inaccuracy, fix: point to `OffsetRows`).
- `ISelectBuilderGroupBy.WithRollup` doc claims a SQLite path via `Sql.Rollup(...)`; both forms are `sqlite: false` — no SQLite spelling exists (Builders 11).
- `JsonValueFunction.cs:4-7` omits MySQL, contradicting the factory doc, matrix (`8.0.21+`), and `docs/functions.md` (Function 16).
- `DmlTargetGuard.cs:13` header comment still describes joined DML (#237) as unbuilt future work while the same file implements and the builders consume it (verifier-added DEFECT, Builders 17 — comment-only fix).
- `IUpdateBuilderJoinedSet.cs:5-6` third sentence violates the sa-write-xml-docs "no mechanism rationale" convention, sole outlier among its siblings (verifier-added DEFECT Low, Builders 15).

**MF-10. `ReturningBuilder` guard message suggests non-compiling code** — `.Into("var1", "var2")` fails CS1503 (`Into` takes `OutputParameter[]`) (Builders 10, CONFIRMED Medium).

**MF-11. Unjustified public accessibility** (cross-chunk pattern, both verifier/CONFIRMED): `EqualityCondition` is the only comparison-family node that is `public` with a public constructor despite no public factory returning the concrete type (Internal 20); `DeleteClause` is `public` while its structural twin `UpdateClause` is `internal`, widening the shipped package surface for no consumer (Internal 1, verifier DEFECT). Fix both to match their siblings.

**Unverified MUST-FIX candidates** — from chunks with no adversarial verification; each carries its own probe evidence but stands unchallenged and should be re-verified before fixing:
- `PercentileCont`/`PercentileDisc` bare `WITHIN GROUP` builds for SQL Server despite the documented `.Over(...)`-only restriction; no `Validate(Dbms)` guard (Public API 3, High).
- `NTILE(0)`/`NTILE(-1)` unguarded, literal-embedded; rejected by live SQLite (Function 3, same ADR 0012 shape as MF-6).
- `UnnestFunction.AsTable` never validates array/column-count correspondence — mismatches silently drop data (live PostgreSQL 16 demonstration) (Function 5).
- `DecodeFunction` accepts an empty search/result-pair array, emitting a two-argument `DECODE(expr, default)` (Function 10; Oracle grammar claim grammar-unverified).

### SHOULD DISCUSS

1. **Held-builder branch semantics** (relates to MF-2): decide the intended contract for pre-`Build()` reuse — freeze-per-clause, fork-on-branch, or documented single-chain-only — before choosing a fix (Builders 13/4).
2. **`ThrowIfJoinedTargetUnaliased` fires for PostgreSQL's plain, unambiguous `DELETE ... USING` with an unaliased target** — rejecting valid SQL; documented policy in `docs/query-statements.md`, but the guard is not dialect-scoped the way ADR 0011's dividing test wants (Builders 4, CONFIRMED as an open question).
3. **Rule 5 ("alphabetical is mechanical") vs. deliberate narrative ordering** in `IMergeBuilderWhenMatched`/`IMergeBuilderWhenNotMatchedBySource` (`ThenUpdateSet` before `ThenDelete`, consistently in both files) — verifier flagged the dismissal as an INCONSISTENCY: either reorder or amend the rule (Builders 8/9).
4. **`IDeleteBuilder.DeleteFrom` `<returns>` omits `FROM`/`USING`**, which are reachable without `WHERE` (Builders 2, CONFIRMED).
5. **`Wait(seconds)` accepts negative/zero literal-embedded values** — same shape as MF-6 but Oracle grammar behavior unverified; guard or document (Internal 2 verifier question).
6. Unverified doc/enforcement gaps from Public API / Function chunks 1–13: `Sql.Inserted` usable anywhere with no SQLA0004 context rule (invalid SQL Server SQL demonstrated); `Concat` doc omits SQLite 3.44 floor; `Greatest`/`Least` docs omit dialect restrictions and min-arity throw; `SkipLocked` missing the dialect `<remarks>` its twin `Nowait` has; `Ltrim(source, trimChars)` omits its SQL Server 2022 floor; `NthValue(n<=0)` guard question (live SQLite rejects; other engines unverified).

### NITS

- Alphabetical-ordering violations: `IDbmsDialect` + MySql/Oracle/PostgreSql dialect impls (Builders 1); `IPagination` member order (Builders 12); `ReturningBuilder` (`Into` before `Build`) (Builders 10); `InsertBuilder.AddValuesRow` placement vs. the trailing-private-helper convention in `UpdateBuilder`/`DeleteBuilder` (Builders 7).
- Guard-message grammar outliers in `ReturningBuilder`: `"At least one expression is required for Returning()."` vs. the 25-site `"<CONSTRUCT> requires at least one <thing>."` template, plus the `"1 were provided"` singular/plural slip (Builders 10, CONFIRMED).
- Comment-length/duplication over `code-comments.md` caps (cross-chunk pattern, CONFIRMED): `SqlBuildingBuffer.cs:18-20, 267-269, 425-428`; `WithRecursiveClause.cs:5-9`; `RollupGrouping`/`CubeGrouping`/`GroupingSetsGrouping` doc summaries (verifier-added, Internal 4); `SqlServerDialect`/`OracleDialect` duplicated 3-line `ExcludedName` comment; `SelectClauseWithOptions`/`SelectClauseWithDistinct` duplicated field rationale; `GroupByItemResolver`/`GroupingSet` trims; `StringAggFunction.cs:41-44`.
- `ReplaceFunction.cs:11` stray blank line (unverified chunk).
- `IMergeBuilderWhenMatched.cs` ordering (see SHOULD DISCUSS 3).

**Refuted in verification** (excluded from the verdict, listed so the exclusion is visible):
- Function 15: the sql-style "matches `TrimFunction`/`FilteredAggregateFunction` precedent" citation and the ADR 0007 attribution for `GroupingFunction`'s overload split — both refuted; the underlying code conclusions stand.
- Function 16: the `JsonExtract` inline-literal "open design question" — refuted as OVERREACH; ADR 0016 already settles JSON paths as grammar-forced literals. The review also miscited ADR 0004 (`LIKE ... ESCAPE` belongs to ADR 0016).
- Function 21: "no extra array allocation at any of the 10 `VariadicFunctionCore` call sites" — refuted; 4 call sites do a measurable (+24 B/op) spread-merge second allocation.
- Internal 9: `.AppendSpace(SqlPart)` interchangeability claim and the "18 files" count — refuted; per-op byte figures flagged as OVERREACH (relative conclusion held).
- Internal 14: "primary constructors dominant (29 files)" — refuted (33 traditional vs. 29); and the review's `LIMIT`/`OFFSET` probe transcript showed MySQL markers while claiming consistency with the PostgreSQL-default `PaginationTests` — a High-severity evidence-integrity defect in the *review*, not the code (the code was independently re-verified correct on every dialect).
- Builders 18: "Select(×9)" — actual count is 8.
- Internal 15 (ReturningClause chunk): the "eager guard" characterization of the duplicate-`Into`-name check — it fires at `Build()`, not at the call site.

## Coverage

- Branch point: n/a
- Chunks reviewed: **66/135** — 69 chunks failed and their files were never reviewed: Internal SQL parts 22, 24–30; Public surface types 1–7; Companion packages 1–2; Analyzers 1–5; TableClassGen 1–6; Tests 1–36; Benchmarks 1–5. **All of `SqlArtisan.Analyzers`, `TableClassGen`, `ArrayBind`, `Dapper`, the public `SqlBuilder/`/`SqlPart/`/`Metadata/` types, every test project, and the benchmarks have zero review coverage.**
- Chunks adversarially verified: 48/66. Unverified (findings stand as drafted, unchallenged): Public API (Sql.*.cs) parts 1–5; Function implementations parts 1–13.
- Files in scope: 649
- Gates: build=true test=true format=true (unit 841, analyzer 620, TableClassGen 145; several verifiers independently re-ran build/unit/format and confirmed clean)
- Empirical probes actually run: 40+ throwaway `dotnet run` harnesses referencing `SqlArtisan.csproj`, each exercising emitted SQL across all five `Dbms` values; live in-process SQLite executions (JOIN-without-ON cartesian product, empty `INSERT`, `NTILE`/`NTH_VALUE`/frame-offset rejections, `JSON_EXTRACT` bound-path); one live PostgreSQL 16 session (`UNNEST` multi-array shapes); guard/exception-message probes matched verbatim against test assertions; `GC.GetAllocatedBytesForCurrentThread` allocation micro-probes (with two verifiers noting absolute figures are environment-sensitive); external-assembly accessibility compiles (CS0122/CS1503 checks); reflection probes (`ParameterNameCache` boundary, typestate interface hierarchy); `DialectMatrix.cs`/ADR/rule cross-checks by direct read in nearly every verified chunk. No live MySQL/Oracle/PostgreSQL-server/SQL Server engines were reachable (no Docker); those grammar claims are marked grammar-unverified where relied upon.

## Recommendations (ranked)

1. **Re-run the 69 failed chunks** before treating this audit as complete — the analyzers, TableClassGen, companion packages, public surface types, and the entire test tree are unreviewed, and several confirmed findings (e.g. the `MatrixKey` name/arity conflation behind MF-2) implicate exactly those unreviewed areas.
2. **Land one guard sweep for MF-1** (all seven empty-assignment/params sites plus exact-message tests) — same fix pattern (`CollectionGuard.ThrowIfEmpty`), same rule citation, one PR.
3. **Close the typestate holes (MF-2)**: strip base interfaces from `ISelectBuilderJoin`, break the `IReturning` path out of `Output` states (Delete and Update), add `Validate(Dbms)` branches for `UPDATE ... FROM`/`JOIN` on unsupported dialects, and decide the held-builder branching contract (SHOULD DISCUSS 1) before implementing its fix.
4. **Fix the resolver family (MF-4/MF-5)**: `ToInvariantString()` in `OrderByItemResolver`, plus null-element guards across `OrderByItemResolver`/`UpdateSetClause`/`UpsertAssignmentResolver`/`SelectItemResolver` in one pass.
5. **Add the ADR 0012 value-domain guards** (`FrameBound` now; `NTILE`, and decide `NthValue`/`Wait` after re-verification) and the `StringAgg` separator guard.
6. **Re-verify then fix the four unverified must-fix candidates** (Percentile/SQL Server, NTILE, UNNEST AsTable, DECODE) — each already has probe evidence but no adversarial pass.
7. **Batch the doc corrections (MF-9) and accessibility tightening (MF-11)**, then sweep the NITs (ordering, comment lengths, message grammar) in a single mechanical cleanup.