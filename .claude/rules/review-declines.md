---
description: The review-decline ledger — in-tree records of below-bar declined findings, and the precision-gated protocol for matching new candidates against them
paths:
  - ".claude/skills/sa-diff-review/**"
  - ".claude/skills/sa-diff-review-refinement/**"
  - ".claude/skills/sa-panel-audit/**"
  - ".claude/skills/sa-panel-diff-review/**"
  - ".claude/skills/sa-release-audit/**"
  - ".claude/workflows/sa-audit-sweep.js"
---

# Review-decline ledger

A triaged review finding that is **declined** needs a durable in-tree record,
or every fresh-context pass re-raises it (ADR 0022). The record's home is
tiered by weight:

1. **Design-level** decline → an ADR (the #491 → ADR 0021 precedent).
2. **Convention-level** decline → a clause in the relevant `.claude/rules/`
   file (the loud-NRE clause in `guards-and-empty-states.md` is the
   template; `docs-style.md`'s #228 paragraph is another).
3. **Below both bars** → a line in the ledger at the bottom of this file.

A `not_planned` issue closure is tracking, never the binding record — the
executors of fresh review passes are `sa-reviewer` instances with no GitHub
access, and per `docs/adr/README.md` an issue is for discussion while the
durable record lives in the tree.

Unlike this directory's other rules, path auto-loading is not how this file
reaches its readers: it binds review conduct, not source editing, so the
review skills and the `sa-audit-sweep` workflow cite it explicitly (the
`paths` above only surface it when that review machinery is itself edited).

## Matching protocol — routed by record precision

When a review candidate resembles a decided record, who gets to drop it
depends on how precisely the record's class boundary is written:

- **Precisely worded clause** (a rules clause, an ADR clause, or a ledger
  entry marked `[precise]`): matching is mechanical, so the *reviewer*
  suppresses the candidate — cite the record and drop it, at every tier
  including SHOULD DISCUSS / NITS. This is the
  `guards-and-empty-states.md` "do not file these in review" mechanism.
- **Terse ledger entry** (no `[precise]` marker): the reviewer must NOT
  suppress — a wrong match would silently discard a real defect,
  invisibly. Report the candidate normally, tagged
  **`possibly decided by RD-NNN`**, and let triage adjudicate the match.

Triage on a tagged candidate has two outcomes, and both are ratchets:

- **Match confirmed** → promote the record: rewrite the ledger line
  precisely enough that matching becomes mechanical (add `[precise]`), or
  move it up a tier into a rules clause or ADR. Each gray-zone
  adjudication is paid once; the mechanical tier only grows.
- **Match rejected** → the candidate is a live finding; triage it on its
  merits.

## Ledger

One line per declined finding-class. Format:

```
- RD-NNN [precise|terse] <finding-class: the shape declined, at class
  granularity — never file:line> — declined because <reason>. (source:
  #NNN or review round)
```

IDs are assigned sequentially and never reused; a superseded line is
rewritten in place (same ID), not re-added. Keep entries grep-able: name
the class by the terms a reviewer would search for.

- RD-003 [precise] Missing `sql.Parameters` assertions or type-only/`Contains`
  exception assertions in tests written before the guard-assertion convention
  (the `unit-tests.md` forward clause) — declined to retro-fix wholesale
  (~90 pre-convention tests): the rule binds new and edited tests, and a file
  upgrades when it is next touched (CastTests was, in the same pass). Not a
  finding for any pre-convention test at any tier.
  (source: release audit pass 1, F44)
- RD-004 [precise] `DbSequence`'s constructor rejecting whitespace-only names
  while alias guards (`StringGuard.ThrowIfNullOrEmpty`) accept whitespace —
  declined to unify: a sequence name is emitted as a bare identifier, invalid
  as whitespace on every dialect, while an alias is quoted, where whitespace
  is legal. The stricter check is correct where it is; the asymmetry is not a
  finding. (source: release audit pass 1, F17)
- RD-005 [precise] Pre-existing comment blocks — in `src/` and `tests/`
  alike (the rule's own `paths` cover both) — exceeding the
  `code-comments.md` length caps and predating the rule — declined
  to mass-trim: the caps bind new and edited comments at review time; an
  existing block trims when its file is next edited for substance. A bulk
  reflow would churn blame for no behavioral gain. Not a finding for any
  pre-rule comment at any tier, in either tree. A comment written *after*
  the rule (an audit-pass addition) gets no such grace: it is trimmed on
  sight, never declined.
  (source: release audit pass 1; tests/ scope settled pass 3, SD20; post-rule
  clause pass 4, SD15)
- RD-006 [precise] `RegexpOptions` flag-value validation of any kind — the
  per-dialect flag alphabet, and a contradictory combination (e.g. case-
  sensitive + case-insensitive together) rendering as written — deferred: an
  ADR 0012 value-domain guard needs each engine's accepted alphabet and
  combination semantics verified against vendor grammar and live probes,
  which the audit freeze does not admit; revisit with integration coverage.
  Not a finding for any RegexpOptions value shape until then.
  (source: release audit pass 1 F18; pass 2 ADJ1 match confirmed)
- RD-007 [precise] Pagination (`Limit`/`Offset`/`FetchFirst`) and
  `ForUpdate(...)` unreachable in combination (CS1061 in either call order)
  though MySQL/Oracle/PostgreSQL accept the combined SQL — recorded as a
  deliberate deferral, not wired during the release audit: widening the
  fluent-state interfaces is feature work, frozen until after the audit
  converges. Re-raise as a feature issue, not a review finding.
  (source: release audit pass 1, F32)
- RD-008 [terse] Coverage-asymmetry additions from the audit test sweeps
  (pass 1: ContainsScore/Score with no unit coverage, CrossJoinLateral Oracle
  unit case, JsonArrowText sibling shapes, `Range(bound)` frame overload,
  TableClassGen ReportJson/ResolveSchema/--fix return-code batch, ArrayBind
  rollback sibling, DocsIndexTests slug derivation vs check_links.py;
  pass 2: WithRollup SqlServer unit, JsonbExistsAll SingleKey, FullJoin
  OnClause guard test, DbmsResolver Unknown fallback, SQLA0205 Binary
  end-to-end, SQLA0104 mixed-dialect and SQLA0202 multi-missing-column
  analyzer cases, TypeCategoryMismatch duplicate EditorConfig, integration
  window tests asserting only Count(), MatrixSweep/Oracle23aiBound label
  duplication and missing skip-check, TCG Indexed end-to-end emission,
  ConsoleUI ReadDatabaseConnectionInfo, MySQL legacy index-catalog fallback
  catch; pass 3: ConditionIf two-way-AND excluded operands,
  `With(...).InsertIgnoreInto(table)` columnless overload, JoinLateral
  excluded-On, JsonValue MySQL and If/Ifnull/Instr dialect unit variants,
  CrossJoinLateral StatementCatalog integration case, Oracle23aiBound
  skip-check, MERGE ThenInsert value assertions, ordered-set-aggregate
  PostgreSQL `.Over()` converse context rule with the PercentileCont
  PostgreSQL-claim verification; pass 4: CaseTests WHEN legal twin,
  ConditionIf AND mirror, DeleteTests two-way AND, If/Iif condition twins,
  CrossJoinLateral Oracle, FullJoin/JoinLateral guard twins, JsonArrowText
  MySQL/SQLite and JsonHashArrow sibling shapes, MERGE partly-excluded
  branches, DbmsResolver legacy providers, vector/RangeBetween asymmetry,
  StatementCatalog CrossJoinLateral Only(), Oracle23aiBound label
  duplication, CommandLineTests required-schema arm, ConsoleUI header and
  ReadDatabaseConnectionInfo, TableClassEmitter Indexed emission,
  ToTsquery/ToTsvector null twins, RegexpOptions combinations, `NotIn<T>`
  collection overload, PercentileDisc silent-case siblings, join/MERGE guard
  coverage twins) — deferred as a tracked batch: coverage widening, not
  defects; scheduled after the audit converges.
  (source: release audit passes 1–4)
- RD-010 [precise] Refinement-scope idiom findings — style, wrapping, naming,
  doc phrasing, measurement idioms, and comment length in files predating the
  caps — in any tree, none of which changes emitted SQL, a guard, or a
  documented fact; the audit sweeps' instances (pass 1: the OverClause
  parenthesis idiom (pass 4 match confirmed), `AppendSpaceIfNotNull` on a
  never-null part, `IsNull` property style divergence, test-file style
  outliers — CaseTests' StringBuilder shape, hanging first arguments, naming
  and filler-comment nits; pass 2: the CASE `WHEN (cond)` wrapping doubling
  parens around an already-grouped condition, `<Expectation>`-less MergeTests
  names, duplicated join-guard test comments, builder-interface doc-phrasing
  asymmetries, WithBuilder's exploded-constructor wrap shape, residual
  test-comment length and 100-column outliers; pass 3: ConditionIfTests
  argument packing, PublicSurfaceBoundaryTests over-length doc blocks,
  AnalyzerConfigResolverTests/ClaudeMdTests/ConstructKeyNaming comment
  lengths, MSBuildPropertyParityTests comment shape; pass 4: Sql.V.cs and
  OracleCatalogReader wrap shapes, the three Dapper benchmark entrants'
  `ParameterNames.Count()` LINQ allocation (equal across entrants, so the
  comparison holds), `DateTimePart`'s XML parentheticals, DbTable/Cte
  `Column` doc-vs-inheritdoc shapes, the `StringAggFunction` mutable-field
  outlier, `ISortable`'s vestigial marker, `AssignmentResolver`'s DRY caveat,
  the single-column `FOR UPDATE OF` doc shape, `SchemaRuleParityTests`
  `ExcludedMembers` phrasing, test naming/type-only-assert/comment residue,
  `TestSettings.createSubFolders`) — deferred: non-defect refinements outside
  the defect-bar review scope, batched for an `sa-diff-review-refinement`
  pass after the audit converges. Not a finding at any tier.
  (source: release audit passes 1–4)
- RD-009 [terse] DML-context dialect gaps the matrix's context-free keys
  cannot express — a joined `DELETE` on SQLite (`InnerJoin`'s entry is All),
  Oracle `DELETE ... USING` (the `Using` key unions the MERGE context's
  support), and the wrong-dialect joined-`UPDATE` spellings that emit
  faithfully (the MySQL JOIN form on Oracle/PostgreSQL/SQLite; the
  un-re-listed FROM form on MySQL/Oracle), the joined-`DELETE`
  repeated-FROM form on Oracle and PostgreSQL (MySQL's and SQL Server's
  spelling, emitted faithfully where those two reject it), and PostgreSQL's
  rejection of `FOR UPDATE` after `GROUP BY` (release audit pass 4, SD12) — all
  SQLA0102-class context rules needing live rejection proofs; deferred under
  the audit freeze. The repeated-target `UPDATE ... FROM` form is
  instance-identity-visible and is guarded at Build() instead (release
  audit pass 2). (source: release audit pass 1 F26; pass 2 F7/F28/ADJ3;
  pass 3 panel 1)
- RD-011 [precise] `InsertInto(t).Set(...)` exposing no `.Output(...)` while
  `Update(t).Set(...)` does — declined as correct typestate: the SET-form
  INSERT is MySQL's construct and `OUTPUT` is SQL Server's, so no dialect
  accepts the pairing; the narrowed interface is the compile-time guard
  working as designed. Not a finding at any tier.
  (source: release audit pass 2, SD9)
- RD-012 [precise] The expressibility gaps recorded in issue #521 — value
  analytics' `Over(PartitionByClause)`, `GroupBy` ordinal literals,
  `With(...)` into `MergeInto(...)`, and `.Output(...)` from the columnless
  `InsertInto(t).Values(...)` chain (T-SQL accepts the column-list-free
  OUTPUT insert) — deferred as feature work under the audit freeze;
  re-raise on the issue, not as a review finding.
  (source: release audit pass 2, ADJ2/SD7/SD13; pass 3, SD9)
- RD-002 [precise] `Validate(Dbms)` running only on the outermost statement
  builder — a subquery, CTE body, or derived table renders through `Format`,
  so its *dialect* guards (e.g. the SQL Server TOP pairing rules) do not
  re-run one level down — declined to extend into the render path: ADR 0007's
  permissive default governs nested dialect availability (the engine rejects
  them loudly), and `docs/query-statements.md` scopes the throw claim to the
  statement's own clauses. The dialect-blind structural walk (duplicate
  clause, dangling join) is outside this decline and does run nested since
  pass 4. Not a finding for any nested dialect-guard shape.
  (source: release audit pass 1; scope narrowed pass 4, F24)
- RD-001 [precise] TableClassGen requiring `--schema` on the SQL Server CLI
  while the interactive prompt defaults it to `dbo` — declined to default the
  CLI: a scripted run states its schema explicitly, and the interactive path
  is where defaults belong (both schema prompts now carry one). The same
  asymmetry between any other required CLI option and its interactive
  prompt's default is this class. Not a finding at any tier.
  (source: #506 smoke-test sweep triage; pass 4 SD16)
- RD-013 [precise] The mid-chain `INSERT INTO t (cols) WITH ... SELECT`
  position (`IWithBuilder` on the column-list stage, `InsertIgnoreInto`
  included) flagged as an invalid WITH placement — refuted: the position is
  the deliberate feeding-SELECT feature the member's own doc describes,
  MySQL documents `INSERT ... WITH ... SELECT` explicitly, and SQLite
  accepts it live. Not a finding; only the leading `With(...).InsertInto`
  form is separately documented. (source: release audit pass 3, F15)
- RD-014 [precise] `BindValue`'s `direction`/`size` accepting any value —
  declined to validate: `Size = -1` is SqlClient's own varchar(max)/
  nvarchar(max) spelling (a meaningful value, so ADR 0012's
  universally-invalid test fails), and an undefined `ParameterDirection`
  is rejected loudly by every ADO.NET provider at bind time. Not a finding
  for either parameter. (source: release audit pass 3, F3)
- RD-015 [precise] A pseudo-column reference building outside its context —
  `Sql.Excluded(...)` outside an upsert's DO UPDATE SET, `Inserted`/
  `Deleted` under the wrong OUTPUT verb — declined as ADR 0007's permissive
  default: every engine rejects the misplaced reference loudly by name, and
  a Build()-time context guard would need cross-clause analysis for a
  failure that is never silent. An SQLA0102-class context rule for these
  belongs with RD-009's family when the freeze lifts. Not a finding for any
  pseudo-column placement. (source: release audit pass 3, F8/PD3)
- RD-016 [precise] Negative or zero pagination and TOP counts rendering as
  written (`LIMIT -1`, `OFFSET -1`, `TOP (-1)`, `FETCH FIRST 0 ROWS`) —
  deferred like RD-006: zero is legal everywhere, SQLite's `LIMIT -1` is a
  meaningful "no limit" (so the LIMIT/OFFSET families fail ADR 0012's
  universally-invalid test outright), and the TOP/FETCH negative cases need
  per-engine live rejection proofs the audit freeze does not admit;
  revisit with integration coverage. Not a finding for any pagination or
  TOP count value until then.
  (source: release audit pass 3, SD3; pass 4 `Top(-1)` match confirmed)
- RD-017 [precise] `SqlBuildingBuffer.AddParameter`'s reference-identity
  linear scan making a build with n distinct bind instances O(n²) — declined
  as ADR 0006's best-effort speed: measured in Release at 37 ms for 2,100
  binds (SQL Server's parameter cap), 101 ms at 5,000, and ~150 ms at 20,000,
  with driver caps bounding the shape; the remedy if it ever matters is a
  lazy reference-equality dictionary past a threshold. Not a finding for any
  bind-count scaling shape. (source: release audit pass 4, panel 1)
