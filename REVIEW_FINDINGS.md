# SqlArtisan Code Review: Full Codebase

## Verdict
**Mergeable after must-fix.**

All gates pass (build clean; 841 + 620 + 145 tests green; format clean), coverage is complete (135/135 chunks reviewed, 135/135 adversarially verified, no chunk failures or file gaps), so nothing blocks structurally. But verification confirmed a substantial set of real defects — most prominently a systemic family of unguarded empty-collection positions that silently emit invalid SQL, two in-place-mutation aliasing bugs that produce silently wrong SQL, two analyzer soundness bugs, and several test/benchmark files that assert nothing or assert wrong-dialect SQL. Five chunk findings were refuted in verification and are excluded from the verdict (listed below).

## Summary
The codebase is in strong shape architecturally — dialect isolation, faithful-emission policy, guard conventions, and the analyzer matrix all held up under adversarial re-derivation, and the large majority of chunks closed with "no findings, confirmed." The confirmed defects cluster into a small number of cross-cutting patterns rather than scattered one-offs: the SET/assignment/collection guard convention is applied inconsistently (roughly ten public entry points leak invalid SQL like `UPDATE t SET ` and `IN ()`), condition/sort nodes mutate in place when the API invites holding and reusing them, and a handful of documented dialect restrictions (Percentile on SQL Server, MERGE SET on PostgreSQL) are enforced nowhere. Every High finding below was reproduced live by at least two independent sessions.

## Findings by Severity

### MUST FIX

**1. Empty-assignment / empty-collection guard family — silent invalid SQL (High; cross-chunk pattern, ~10 entry points).**
Confirmed live in multiple independent chunks and verifications; several confirmed invalid against real PostgreSQL 16 and SQLite:
- `UpdateBuilder.Set()` (both overloads) → `UPDATE t SET ` — Builders 14/16, Infra 17, Tests 7
- `InsertBuilder.Set()` → `INSERT INTO t () VALUES ()` (plain and INSERT IGNORE) — Builders 6/7, Infra 5
- `InsertBuilder.DoUpdateSet()` / `OnDuplicateKeyUpdate()` → trailing `DO UPDATE SET ` / `ON DUPLICATE KEY UPDATE ` — Builders 5/7, Infra 6, Tests 8
- `MergeBuilder.ThenUpdateSet()` (WHEN MATCHED and WHEN NOT MATCHED BY SOURCE) → bare `UPDATE SET ` — Infra 11
- Single-row `Values()` with zero args → `INSERT INTO t VALUES ()` — Builders 6/7 (live-invalid on PG + SQLite)
- `SelectBuilder.From()` empty → bare `FROM` (Update/Delete already guard; Select does not) — Infra 2
- `In()`/`NotIn()` `params object[]` overloads → `IN ()` / `NOT IN ()` (collection overloads guard; params overloads don't) — Infra 23
- `Sql.Decode(expr, pairs[], default)` with empty pairs → `DECODE(expr, :0)` — Function impl 10, Tests 11
Fix at the shared chokepoints (`UpdateSetClause`, `InsertSetClause`, `UpsertAssignmentResolver`/`MergeUpdateSetClause`, `InsertValuesClause`, the Select `From` path, `SqlExpression.In/NotIn`, `DecodeFunction`) with the established `CollectionGuard.ThrowIfEmpty` pattern and exact-message tests.

**2. INSERT column-list width never cross-checked against row width (High).** `InsertInto(t, a, b).Values(x)` builds `INSERT INTO t (a, b) VALUES (:0)`; live-invalid on PG ("more target columns than expressions") and SQLite. — Builders 7.

**3. Culture-dependent numeric literal in ORDER BY (High).** `OrderByItemResolver.cs:41` uses plain `ToString()`; under de-DE, `OrderBy(2.5)` emits `ORDER BY 2,5` — one sort key becomes two. Repo convention (`ToInvariantString`) exists at 10+ sibling sites. — Infra 12.

**4. In-place mutation / aliasing of held nodes — silently wrong SQL (High).**
- `SqlCondition.operator &`/`|` mutate the left `AndCondition`/`OrCondition` in place and return the same instance; a held base condition extended along two branches cross-contaminates both (reproduced: branch1 silently gains branch2's operand). The type's own docs invite the accumulator pattern. — Infra 25 (verifier DEFECT).
- Held builder prefix accepts a second pre-`Build()` stage call: `WHERE x WHERE y`, `GROUP BY a GROUP BY b` build silently — the canonical #225 hazard shape, untested. — Builders 13.
- `SortOrder.NullsFirst/NullsLast` mutate the receiver; two derivations alias to whichever was last. — Public surface 5 (verifier DEFECT).

**5. Bounded-exception / type-state gaps (High–Medium).**
- `PercentileCont/Disc` bare `WITHIN GROUP` builds for `Dbms.SqlServer` though the library's own docs and matrix say only the `.Over()` form is valid there; analyzer cannot see it (matrix's own comment) — needs a `Validate(Dbms)` guard. — Public API 3, Function impl 19 (grammar-unverified live, internally documented).
- `ISelectBuilderJoin : ISqlBuilder, IForUpdate` lets `InnerJoin(...)` (etc.) reach `Build()`/`ForUpdate()` with no `On`/`Using`, contradicting the interface's own docs. — Builders 10.
- `Sql.Inserted()` usable outside `Output(...)` emits invalid `INSERTED.col` in WHERE; no ContextRules entry despite the Grouping/WITH ROLLUP precedent. — Public API 2.
- `Output(...)` combines silently with `Returning(...)`/`Using(...)` on Update/Delete — no valid spelling on any dialect. — Builders 3 (verifier DEFECT).
- Aliased `OUTPUT … INTO` destination table on SQL Server unguarded (primary-target twin guard exists). — Infra 14 (verifier DEFECT, grammar-unverified).

**6. MERGE `UPDATE SET` alias-qualified target column breaks on PostgreSQL 15+ (High).** `MergeUpdateSetClause` renders `SET "t".name = ...`; the repo's own sweep catalog documents PG rejecting a qualified SET target and deliberately dodges it, while `MergeTests.Merge_PostgreSql_OmitsTerminatingSemicolon` certifies the broken shape and the docs teach the idiom. Switch to the `AppendAssignmentsCsv`/`FormatUnqualified` path used by every sibling assignment clause. — Infra 11 (verifier DEFECT; grammar corroborated in-repo, no live PG MERGE run).

**7. Value-domain guards missing where ADR 0012's three conditions hold (Medium–High).** `Ntile(buckets ≤ 0)`, negative window-frame offsets (`Preceding(-5)`), a `FOLLOWING`-kind bound as a sole frame extent, and inverted `ROWS BETWEEN` bounds all build silently and were live-rejected by SQLite. `PercentileFractionGuard` is the shipped precedent. (`NthValue(n≤0)` is the same class but already tracked — see SHOULD DISCUSS.) — Function impl 3, Infra 13 (+verifier extras).

**8. Null-argument NREs instead of guard exceptions (Medium; pattern).** `UpdateSetClause.Parse` (null array/element), `SelectItemResolver.Resolve(null)`, `OrderByItemResolver` null element, `InsertValueResolver`/`InsertSetClause` null paths, `StringAggFunction` null separator (NRE at `Build()`), and `OracleArrayBindCommandFactory` null statement element. Sibling resolvers already guard with the "Use Sql.Null" `ArgumentNullException`. — Infra 5/12/17, Function impl 20, Companion 1.

**9. `UnnestFunction.AsTable` never validates array/column-count correspondence (High).** Multi-array + no columns, fewer columns than arrays (silently drops arrays), more columns than arrays — all build; live PG 16 errors or silently changes result shape. — Function impl 5.

**10. Derived-table alias empty/null renders `""` (Medium).** `DerivedTable(string)` and `ISubquery.AsTable(alias)` skip the guard the `Unnest.AsTable`/`Sql.Values` siblings have. — Public surface 6.

**11. Analyzer soundness defects (High).**
- `FluentChain.HasVisibleStatementHead` treats the first SqlArtisan invocation it meets — including an argument-position `ConditionIf(...)`/`Not(...)` — as the chain, silently suppressing SQLA0007/SQLA0010 on directly-built statements. Reproduced with a live Roslyn harness. — Analyzers 3.
- `NotInNullableSubqueryRule.FiltersOutNulls` false-positives (SQLA0008) when the `IsNotNull` remediation is held in a local — violating the documented no-false-positive charter. Reproduced live. — Analyzers 4.

**12. Test-suite defects (High–Medium).**
- `CaseTests.cs:389-766`: 12 Simple-CASE tests build `expected` and never assert it — they pass unconditionally. — Tests 2.
- Wrong-dialect default builds asserting SQL that live PG rejects: `PercentileTests` `.Over()` forms, `PublicSurfaceNamingTests` `code_seq.NEXTVAL` (both live-rejected on PG 16), `ToNumber` 1-arg form (Oracle-only per matrix). Fix with explicit `Build(Dbms.Oracle/SqlServer)`. — Tests 6, Tests 14.
- `Resolve_SqlConnection_ReturnsSqlite` asserts `Dbms.SqlServer` (plus stray `;;`). — Tests 18.
- 54 Dapper-mapper tests dead behind an undefined `#if SQL_MAPPER_TEST` with a placeholder Oracle connection string — remove or wire up (surface is live-covered by integration tests). — Tests 19.
- `Oracle23aiBoundSweepTests.TryExecute` lacks the sibling's `catch (Exception)` fallback (latent divergence from `MatrixSweepTestBase`). — Tests 32.

**13. Benchmark project defects (High–Medium).** `SqlBuildingBufferBenchmark` reuses a frozen builder — throws "already built" on every invocation, all results NA (reproduced twice). `SqlArtisanBenchmark.cs:29` comment ("Parameters is Dictionary") wrong since introduction. `BenchmarkValidation` has a demonstrated false-negative blind spot for keyword-gluing in the Dapper SqlBuilder template. — Benchmarks 5, 3, 1.

**14. TableClassGen defects (High–Low).** Culture-sensitive `.ToLower()/.ToUpper()` in `InformationSchemaCatalogReader`/`SqliteCatalogReader`/`OracleCatalogReader` — under tr-TR the unconditional Oracle schema uppercase (`BİLLİNG`) makes every catalog query return zero rows; invariant precedent exists in `ColumnCategory.cs`. Reporter's Fix-mode "Regenerated (N tables)" counts Removed entries that are never written. `ColumnPattern` regex silently drops quote-containing column names from `--check` diffs. `ConsoleUI` unguarded `int.Parse` on the Port prompt; `nameof(connInfo.Dbms)` yields the wrong ParamName. — TableClassGen 2, 5, 6, 1.

**15. Public-surface accuracy defects (Medium–Low; doc inaccuracies are must-fix by project policy).**
- `ISubquery.As` XML doc claims `(SELECT ...) AS "alias"`; no `AS` is ever emitted. — Public surface 1.
- `IPagination.Limit` tells SQL Server users to use `FetchFirst`, which the matrix marks SQL-Server-unsupported standalone and whose own doc says the opposite. — Builders 12.
- `ISelectBuilderGroupBy.WithRollup` doc implies SQLite validity (both rollup forms are `sqlite: false`); class summary also omits SQL Server. — Builders 11.
- `ReturningBuilder`'s aliased-expression exception recommends `.Into("var1","var2")`, which does not compile (no string overload); plus two message-template deviations and member-ordering. — Builders 10.
- `AsteriskMarker` doc claims `UPPER(*)` "does not compile" — it compiles and fails at runtime. `DeleteClause` needlessly `public` vs. its `internal` twin `UpdateClause`. `EqualityCondition`'s public constructor is the sole unjustified public in its family (independently confirmed three times). `SqlParameters.Get<T>` has an undocumented NRE path for non-nullable `T` on a null direct-constructed `BindValue`. — Infra 1, Infra 20, Public surface 2.
- Dialect-remarks gaps contradicted by `DialectMatrix.cs`: `Concat` (SQLite 3.44 floor), `Excluded` (MySQL 8.0.19 floor; Oracle/SQL Server unsupported), `Greatest`/`Least`, `SkipLocked`, `Round(expr)` 1-arg (invalid on SQL Server, zero doc signal), `RegexpReplace` missing "(15+)" its three siblings carry, `JsonValueFunction` class doc omitting MySQL, `StringAggFunction` class doc omitting SQLite, `Ltrim/Rtrim(2)` SQL Server 2022 floors, `DateTimePart` doc omitting the `Datetrunc` consumer, `CountNullableColumnRule`'s factually wrong "boxing conversion" comment, `WithRollupClause` "MySQL's" comment contradicting the interface doc. — Public API 1/2/4, Function impl 16/20, Analyzers 2, Infra 3, Public surface 4.

### SHOULD DISCUSS
- **`DeleteBuilder`'s blanket joined-target-alias guard** forecloses valid unaliased PostgreSQL/MySQL `DELETE ... USING` — deliberate and tested, but only the author can weigh uniform simplicity vs. rejecting valid SQL. — Builders 4.
- **`NthValue(n≤0)`** guard decision — same class as the Ntile fix above; already tracked in `REVIEW_FINDINGS.md`; decide together. — Function impl 4.
- **`EngineVersion` `Equals`/`GetHashCode` contract violation** (`"1"` vs `"1.0"`) — no exploited call site today. — Analyzers 3.
- **Test-coverage gaps (verified, non-blocking):** empty-`Set()`/`DoUpdateSet()`/`OnDuplicateKeyUpdate()` negative tests (land with the guards from MUST FIX 1); `Values(object[][])` empty-overload test; DELETE-side joined-unaliased-target guard test; `OnDuplicateKeyUpdate` silence-regression test for SQLA0012; split shared silent bodies in `InsertColumnsAnalyzerTests`; at-limit (128) identifier tests for Oracle/SQL Server; `OuterApply` Sqlite faithful-emission test; `LastValue`/`NthValue` partition variants; MERGE upsert test's self-join asserting nothing about branch correctness; malformed-JSON `--config` test; stderr capture in `CliRunnerTests`; `InnerJoin_ComplexConditionWithWhere` missing `sql.Parameters` assert; `ContextRuleAnalyzerTests`' `GroupingInWhere...` comment misdescribes the mechanism it claims to regression-test; `SchemaRuleParityTests`' HiddenChains catalog should gain the ConditionIf/Not wrap shapes (pairs with MUST FIX 11).
- **Minor allocation pattern:** `Coalesce`/`Concat`/`Grouping`/`GroupingId` spread-merge a second array per construction (~24-32 B/op); fix as a small sweep if at all. — Function impl 16/21.
- **`TempSqliteDatabase.Create`** leaks its seed file if caller DDL throws (inert today). — Tests 36.
- **SmokeCatalog stale scope note** claims date/time/conversion/regexp are "deferred" while the file contains them; only sequences are actually missing. — Tests 30.

### NITS
- **Member-ordering (rule 5):** `IDbmsDialect` + MySql/Oracle/PostgreSql dialect impls; `IMergeBuilderWhenMatched` and `IMergeBuilderWhenNotMatchedBySource` (fix together); `ReturningBuilder` (`Into` before `Build`); `InsertBuilder.AddValuesRow` placement; `IPagination` (`FetchFirst` last).
- **Comment length/duplication/accuracy (code-comments rule):** `DmlTargetGuard` 14-line header; `IDbmsDialect` over-length enumerating summaries; `OracleDialect`/`SqlServerDialect` duplicated `ExcludedName` rationale; `ExpressionAlias` 7-line comment; `ExpressionResolver` duplicated rationale + wrong "16 B/call" figure (measured ~40 B); duplicated `Lag`/`Lead` remarks; `LikeCondition`/`NotLikeCondition` duplicated escape rationale; `OrCondition._rest`; `GroupByItemResolver`; `GroupingSet`; `WithRecursiveClause` header; `SelectClauseWithOptions`/`WithDistinct` duplicated line; `StringAggFunction` 4-line comment; `CaseConverter` header; `ConstructKeyNaming` examples duplicated by adjacent tests; `SqliteCatalogReaderTests` "the one engine" (Oracle also bypasses information_schema); `TestSchema` self-contradicting engine list; `CookbookTables` "CTE classes" clause; `TestEnum.None` dead member; `ReplaceFunction` stray blank line; `OrderBy` summary over-enumeration; `With` summary length (established pattern — optional); `subquey` typo (below bar, noted only).
- Test-name style: `On<Dbms>` prefix in two names; `Into()` mismatch-message pluralization.

## Refuted in verification (excluded from verdict)
1. **`BindValue` public constructor as a guard-bypass MUST FIX** (Public surface 3) — refuted: it is the documented, integration-tested pgvector escape hatch (`docs/expressions.md:467`); the proposed `internal` fix would break shipped API.
2. **`JsonExtract` bound-path "open design question"** (Function impl 16) — refuted: ADR 0016 already classifies JSON paths as grammar-forced literals by position; not open.
3. **`MultiRowInsertTests` row-width exact-message MUST FIX** (Tests 5) — refuted: the guard predates the #236/#245 convention, which is explicitly forward-only.
4. **`ColumnIndexInfo.IsIndexed` precedence "logic defect"** (TableClassGen 3) — refuted: deliberate ADR 0010 "noticed, never interpreted" design; the proposed reorder would reintroduce false positives.
5. **`DbTableBase.cs:40-41` comment-trim nit** (Public surface 5) — refuted as inconsistent with the pervasive accepted pattern.
Additionally, verifiers flagged evidence-quality errors *inside* several reviews (a fabricated "verbatim" RegexpLike Oracle probe line, a nonexistent grep exception, a `CurrentTime` matrix misstatement, several miscounts, condensed-not-verbatim transcripts). These do not implicate the reviewed code and are excluded from findings, but they mean chunk-review prose should not be reused verbatim without the verifier's corrections.

## Coverage
- Branch point: n/a
- Chunks reviewed: 135/135
- Chunks adversarially verified: 135/135 (none carried the "verification unavailable" marker)
- Files in scope: 649
- Gates: build=true test=true (841 unit / 620 analyzer / 145 TableClassGen) format=true
- Empirical probes actually run (from chunk reviews and verification): hundreds of throwaway-harness `Build(Dbms.X)` probes across all five dialects (emission shape, parameter markers, quoting, guard messages, all four #225 hazard shapes); live-engine executions — PostgreSQL 16 via psql (UNNEST arity, empty VALUES/SET/column-width, Percentile OVER rejection, `code_seq.NEXTVAL` rejection, `boolean = integer`), in-process SQLite (NTILE/NTH_VALUE domain rejections, frame-bound shapes, RIGHT/FULL JOIN, JSON bound path, catalog/pragma matrices, MATCH), live Dapper+Sqlite round trips; allocation probes (`GC.GetAllocatedBytesForCurrentThread`, reproduced to the byte in most cases); culture probes (tr-TR, de-DE); Roslyn analyzer harnesses (ConditionIf/Not suppression, SQLA0008 false positive, SQLA0009 silence, ContextRules anchor probes); reflection sweep of `MatrixSweepCatalog` (263 cases × 5 dialects = 1315 builds, 0 failures); BenchmarkDotNet runs (`validate` + short jobs, reproduced the NA failure and the 2.22/2.84/60.94 KB figures); TableClassGen CLI/reflection probes. No Docker daemon was available: live MySQL/Oracle/SQL Server executions were not possible, so those grammar claims rest on in-repo documentation/matrix comments and are tagged grammar-unverified where load-bearing (Percentile/SQL Server, MERGE alias forms, OUTPUT INTO alias, ODKU empty).

## Recommendations (ranked)
1. **Close the empty-assignment/collection guard family in one sweep** at the shared chokepoints (MUST FIX 1-2), with exact-message tests per the #236/#245 convention — this is the largest cluster of silent-invalid-SQL risk and one patch shape fixes ~10 entry points.
2. **Fix the mutation/aliasing bugs** (`operator &`/`|`, `SortOrder`, held-prefix double stage) by returning fresh nodes / adding per-clause-type pre-Build dedup — silent wrong SQL is the project's stated worst case.
3. **Fix the culture bug and the null-element NRE family** in the resolvers (`ToInvariantString`/`Convert.ToString(inv)`, null guards matching siblings).
4. **Add the bounded-exception guards** (Percentile/SQL Server, MERGE SET qualification for PG, ISelectBuilderJoin type-state, OUTPUT×RETURNING/USING, aliased OUTPUT INTO, Inserted ContextRules entry) and run the integration matrix in an environment with Docker to discharge the grammar-unverified tags.
5. **Fix the two analyzer soundness bugs** (FluentChain argument-position wrap; FiltersOutNulls local-variable false positive) and extend `SchemaRuleParityTests.HiddenChains` accordingly.
6. **Repair the test and benchmark harness defects** (CaseTests assertions, wrong-dialect builds, misnamed test, dead `#if` suite, frozen-builder benchmark, validation blind spot) — these currently give false confidence.
7. **TableClassGen invariant-culture sweep** plus the Reporter/regex/ConsoleUI fixes.
8. **Docs/XML-remarks accuracy sweep** driven by `DialectMatrix.cs` as source of truth (all items in MUST FIX 15), including the `ISubquery.As`/`IPagination` contradictions.
9. Resolve the SHOULD DISCUSS design questions (DELETE USING alias guard scope; NthValue/Ntile domain-guard decision — decide alongside item 4's guards).
10. Batch the NITS (ordering + comment cleanups) as a single low-risk chore PR.