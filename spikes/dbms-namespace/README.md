# Spike: Per-DBMS namespaces (`SqlArtisan.Oracle` / `SqlArtisan.SqlServer` / …)

> **Status:** exploratory prototype. Nothing here is wired into the solution or
> compiled by any `.csproj`. It exists to make the design discussion concrete
> before committing to (or rejecting) the approach.
>
> **Scope of this spike:** the **numeric function** category only, across all
> five supported DBMS. Enough to feel the real cost/benefit; small enough to
> verify by hand.

## Goal being prototyped

A SqlArtisan user who targets, say, Oracle should see **only Oracle's syntax**
in IntelliSense:

```csharp
using SqlArtisan.Oracle;          // pick your DBMS once, here

Select(Sql.Ceil(t.Price))         // ✅ offered
Select(Sql.Ceiling(t.Price))      // ❌ does not exist in SqlArtisan.Oracle
```

This is the breaking-change-allowed destination we converged on:

1. a **single source of truth** (the applicability *matrix* / catalog),
2. **code generation** of one `Sql` facade per DBMS from that catalog,
3. **DBMS-tagged nodes** validated at `Build()` time, to close the "mixing"
   hole that pure namespace filtering leaves open,
4. the universal `SqlArtisan.Sql` facade and the `Build(Dbms)` overload are
   **removed** (breaking).

The artifacts below correspond to (1), (2), (3):

| File | Role |
|------|------|
| `catalog/NumericFunctionCatalog.cs` | (1) single source of truth |
| `generated/Oracle.Sql.Numeric.g.cs` | (2) "generated image" — Oracle facade |
| `generated/SqlServer.Sql.Numeric.g.cs` | (2) "generated image" — SQL Server facade |
| `prototype/DbmsAffinity.cs` | (3) tag + Build-time validation, riding on the existing `Format` traversal |
| **`proof/`** | **a compiling, test-backed vertical slice that runs all of the above end to end** |

### `proof/` — does ③ actually work? (run it)

`catalog/`, `generated/`, `prototype/` are illustrative (not compiled). `proof/`
is the opposite: a self-contained xUnit project with a faithful miniature of the
real pipeline (`SqlExpression.Format(buffer)` → `SqlBuildingBuffer` →
`IDbmsDialect`), proving the mechanism rather than describing it.

```bash
dotnet test spikes/dbms-namespace/proof      # 7 passed
```

It is **not** in `SqlArtisan.sln` and does **not** reference the real library,
so it cannot destabilise production code. The miniature maps 1:1 onto
`src/SqlArtisan`; the entire production-side footprint of ③'s tag mechanism is
**two new members** (`SqlExpression.AuthoredFor`, `IDbmsDialect.Dbms`) and **one
new line** (the affinity check in `SqlBuildingBuffer.Append(SqlPart)`).

What the 7 passing tests establish:

| Claim | Test |
|-------|------|
| DBMS folded into namespace; `Build()` takes no arg and emits that DBMS's SQL | `Oracle_Ceil_BuildsCeil`, `SqlServer_Ceiling_BuildsCeiling` |
| IntelliSense filtering is real — the invalid function does not exist (proven by reflection; uncommenting it fails to compile) | `Oracle_Facade_Has_Ceil_But_Not_Ceiling`, `SqlServer_Facade_Has_Ceiling_But_Not_Ceil` |
| Tag validation catches the cross-namespace **mixing** hole at build time | `Mixing_OracleNode_Into_SqlServerBuild_Throws` |
| Portable nodes (columns) and universal functions are **not** over-restricted (no false positives) | `PortableColumn_BuildsOnAnyDbms`, `Universal_Abs_NoFalsePositive` |

**Verdict: ③ is viable.** The C# namespace filtering, the DBMS-folded entry
point, and the near-zero-cost mixing guard all work as designed against a
faithful pipeline. No blocker found that would argue against committing to ③.
The remaining work is *engineering scale* (the generator + the verified matrix
across all categories), not *feasibility*.

---

## 1. The applicability matrix (numeric functions)

> ⚠️ **VERIFICATION REQUIRED.** This matrix is AI-drafted from general SQL
> knowledge. A wrong matrix is *worse than none* (it gives false confidence),
> so every cell must be checked against each vendor's current documentation
> before it is trusted. Cells I am least sure about are marked 🔶.

Canonical name = the name SqlArtisan would expose. "—" = not available under
any name (an operator or a different construct is used instead).

| Canonical | MySQL | Oracle | PostgreSQL | SQLite¹ | SQL Server | Divergence |
|-----------|-------|--------|------------|---------|------------|------------|
| `Abs(x)`        | `ABS`            | `ABS`  | `ABS`              | `ABS`            | `ABS`            | none — **universal** |
| `Sign(x)`       | `SIGN`           | `SIGN` | `SIGN`             | `SIGN`           | `SIGN`           | none — **universal** |
| `Floor(x)`      | `FLOOR`          | `FLOOR`| `FLOOR`            | `FLOOR`¹         | `FLOOR`          | none |
| `Sqrt(x)`       | `SQRT`           | `SQRT` | `SQRT`             | `SQRT`¹          | `SQRT`           | none |
| `Power(x,y)`    | `POWER` (also `POW`) | `POWER` | `POWER`        | `POW`/`POWER`¹   | `POWER`          | spelling only |
| `Ceil(x)`       | `CEIL`           | `CEIL` | `CEIL`            | `CEIL`¹          | **—** 🔶        | **existence**: no `CEIL` on SQL Server |
| `Ceiling(x)`    | `CEILING`        | **—**  | `CEILING`         | `CEILING`¹       | `CEILING`        | **existence**: no `CEILING` on Oracle |
| `Mod(x,y)`      | `MOD` (also `%`) | `MOD`  | `MOD`             | **—** (use `%`)  | **—** (use `%`)  | **existence**: operator-only on SQLite/SQL Server |
| `Round(x)`      | `ROUND`          | `ROUND`| `ROUND`           | `ROUND`          | **—** ²         | **arity**: 2nd arg required on SQL Server |
| `Round(x,n)`    | `ROUND`          | `ROUND`| `ROUND` (numeric only) ³ | `ROUND`   | `ROUND`          | **semantics** (see ³ and rounding-mode note) |
| `Trunc(x[,n])`  | **—** ⁴          | `TRUNC`| `TRUNC`           | `TRUNC`¹         | **—** ⁵         | **name/existence**: `TRUNCATE` on MySQL, absent on SQL Server |

**Footnotes**

1. SQLite's built-in math functions (`CEIL`, `FLOOR`, `POW`, `SQRT`, `TRUNC`, …)
   require the library to be compiled with `SQLITE_ENABLE_MATH_FUNCTIONS`. This
   is on by default in the amalgamation since 3.35.0 (2021-03) but is **not
   guaranteed** on every distro/driver build. `ABS`, `SIGN`, `ROUND` are core
   and always present. 🔶
2. SQL Server `ROUND` **requires** the length argument: `ROUND(x)` is a syntax
   error; you must write `ROUND(x, 0)`. So the single-arg overload must not be
   generated for `SqlArtisan.SqlServer`.
3. PostgreSQL defines two-arg `ROUND(v, s)` only for `numeric`, **not** for
   `double precision`. `ROUND(2.5::double precision, 0)` is an error. 🔶
4. MySQL spells truncation `TRUNCATE(x, d)` and **requires both arguments** — a
   different name *and* a different arity from `TRUNC`.
5. SQL Server has no `TRUNC`; truncation is `ROUND(x, n, 1)` (non-zero third
   arg). This is a *construct* difference, not a renamed function.

### What the matrix already proves

- **The value is concentrated.** 5 of 11 functions (`Abs`, `Sign`, `Floor`,
  `Sqrt`, `Power`) are universal — filtering them per DBMS yields *nothing*; it
  is pure duplication. The entire benefit lives in the other 6 (`Ceil`,
  `Ceiling`, `Mod`, `Round`, `Trunc`). This is the cost/value asymmetry from the
  analysis, now visible in one category.
- **Existence-filtering is necessary but not sufficient.** Even a perfect
  show/hide split does **not** capture:
  - rounding mode — Oracle/MySQL/SQL Server round half *away from zero*;
    PostgreSQL rounds half *to even* for `double precision`. Same `Round`
    signature, different result. 🔶
  - the PG `double precision` vs `numeric` restriction on two-arg `Round`.
  - `Mod` vs `%`, `Trunc` vs `ROUND(...,1)` — "absent" really means "spell it a
    different way", which the namespace can guide but not auto-translate
    (translating would violate the project's core philosophy).

  → The per-DBMS namespace is a **discoverability guardrail, not a correctness
  guarantee.** That conclusion survives contact with real data.

---

## 2. Generation (the "5× duplication" answer)

Hand-maintaining five facades is the trap that defeated this idea before. The
prototype shows the alternative: **one catalog, N generated facades.**

`catalog/NumericFunctionCatalog.cs` lists each function once with its per-DBMS
availability and spelling. A generator (source generator or T4 — out of scope
for this spike) emits, for each `Dbms`, only the methods whose catalog entry
includes that `Dbms`. The two `*.g.cs` files are what the generator *would*
produce — written by hand here so we can read them.

Diff the two generated facades to see the filtering working:

- `Oracle.Sql.Numeric.g.cs` has `Ceil`, `Mod`, `Round(x)`, `Round(x,n)`,
  `Trunc` — and **no** `Ceiling`.
- `SqlServer.Sql.Numeric.g.cs` has `Ceiling` — and **no** `Ceil`, **no** `Mod`
  (operator only), **no** single-arg `Round`, **no** `Trunc`.

The universal five appear in both, generated identically. That is the
duplication; generation makes its marginal cost ~zero, which is the whole
premise.

---

## 3. DBMS-tagged nodes + Build-time validation

Namespace filtering only governs what you can *type*. It cannot stop you from
reusing an Oracle-authored subexpression inside a SQL Server build (e.g. a
shared helper that returns a `SqlExpression`). `prototype/DbmsAffinity.cs`
sketches the cheap closing of that hole:

- every function node records the `Dbms` it was authored for (universal nodes
  record `null`);
- validation **rides on the traversal that already happens** during `Format`:
  `SqlBuildingBuffer` already visits every `SqlPart`, and it already knows its
  dialect's `Dbms`. Adding one check in `Append(SqlPart)` catches a foreign
  node with **no extra tree walk** and near-zero overhead.

This upgrades "authoring guardrail" → "build-time guarantee against mixing",
without the viral generics of a phantom-type (`SqlExpression<TDbms>`) design.
It still does **not** catch the semantic divergences in §1 — those remain the
user's responsibility, by design.

---

## Step 2 — thin integration into the REAL library (validated)

The `proof/` slice ran against a miniature. The next validation wired the same
mechanism into `src/SqlArtisan` itself, scoped to `Abs`/`Ceil`/`Ceiling`, to
answer: *does ③ drop into the real codebase without breaking the existing
suite?* Result: **yes — 353/353 tests green, `dotnet format` clean.**

Production-side footprint (all additive):

| Change | File(s) |
|--------|---------|
| `Dbms? AuthoredFor` on the node base (defaults null via the implicit `base()` call, so every existing node is unchanged/portable) | `Internal/.../SqlExpression.cs` |
| `Dbms Dbms { get; }` on the dialect + 5 impls | `Internal/.../DbmsDialect/*` |
| Affinity guard centralised in one `Write(SqlPart)` chokepoint, with every in-buffer `Format(this)` routed through it | `Internal/.../SqlBuildingBuffer.cs` |
| Tagged `CeilFunction`/`CeilingFunction` + tagged `AbsFunction` ctor | `Internal/.../NumericFunction/*` |
| `SqlArtisan.Databases.Oracle` / `SqlArtisan.Databases.SqlServer` facades (numeric slice, DBMS-folded `Build()`) | `PerDbms/**` |
| New tests | `tests/.../PerDbmsNamespaceTests.cs` |

New tests prove against the **real** pipeline: `SqlArtisan.Databases.Oracle.Sql.Select(Sql.Ceil(1)).Build()`
→ `SELECT CEIL(:0)`; the SQL Server mirror → `SELECT CEILING(@0)`; reflection
confirms `Ceil`/`Ceiling` mutual absence; and reusing an Oracle node in the
default (PostgreSql) build throws.

### ⚠️ Key finding (RESOLVED): namespace naming collides with other namespaces

Two distinct collisions surfaced while integrating, both from naming a leaf after
a DBMS or a common word:

1. **`SqlArtisan.Oracle` shadowed the `Oracle.ManagedDataAccess` driver
   namespace** in `DbmsResolverTests` (which has `using SqlArtisan;`). The build
   broke until qualified `global::Oracle.ManagedDataAccess`. Same hazard for
   `MySql.*` and any user file referencing both SqlArtisan and a vendor driver.
2. The first-choice prefix word **`For`** (`SqlArtisan.For.Oracle`) **shadowed
   the `Keywords.For` constant** used internally for `NEXT VALUE FOR` — `'For' is
   a namespace but is used like a variable`. Short/common prefix words are unsafe.

**Decision (maintainer): distinct prefix.** Adopted **`SqlArtisan.Databases.<Dbms>`**
(`Databases` is collision-free against both driver namespaces and library
identifiers). No driver shadowing, no `global::` workaround needed; the
`DbmsResolverTests` change was reverted. The cost is a slightly longer `using`.

Lesson for ③: the prefix segment must be vetted against (a) vendor driver
namespaces and (b) existing `Keywords`/type identifiers before adoption.

## Step 3 — generator PoC: one catalog → N facades (validated)

The open risk after Step 2 was *scale*: hand-maintaining a facade per DBMS is the
trap that defeated this idea before. Step 3 replaces the hand-written facades
with **generated** ones and proves the duplication cost collapses to a single
data file.

Wired into the real build:

| Piece | Location |
|-------|----------|
| Incremental source generator | `src/SqlArtisan.Generator/PerDbmsFacadeGenerator.cs` |
| Single-source catalog (TSV, `AdditionalFiles`) | `src/SqlArtisan/PerDbms/NumericFunctions.catalog.tsv` |
| Analyzer reference + `AdditionalFiles` wiring | `src/SqlArtisan/SqlArtisan.csproj` |
| Hand-written `PerDbms/Oracle|SqlServer/Sql.cs` | **deleted** — now generated |

The catalog is three lines:

```
Abs      AbsFunction      MySql,Oracle,PostgreSql,Sqlite,SqlServer
Ceil     CeilFunction     MySql,Oracle,PostgreSql,Sqlite
Ceiling  CeilingFunction  MySql,PostgreSql,Sqlite,SqlServer
```

From it the generator emits **five** `SqlArtisan.Databases.<Dbms>.Sql` facades
(snapshot in `generator/generated-output-snapshot/`):

- `Oracle` → `Ceil`, no `Ceiling`
- `SqlServer` → `Ceiling`, no `Ceil`
- `MySql` / `PostgreSql` / `Sqlite` → both

**The decisive result:** the hand-written facades were deleted and the suite —
which already exercises `SqlArtisan.Databases.Oracle/SqlServer` through the real
pipeline — **still passes 353/353, `dotnet format` clean.** The generated code is
provably correct because the same tests that validated the hand-written slice now
validate the generated one, unchanged.

What this establishes about *scale*:

- Adding a DBMS = adding it to a catalog row (and a dialect). No new facade file.
- A universal function = one catalog row → emitted into all five facades for
  free; the 80% common surface stops being a duplication burden.
- A diverging function (`Ceil`/`Ceiling`) = list only the DBMS where it is valid;
  filtering falls out of the catalog automatically.

Remaining engineering (not feasibility) for production: multi-arity functions and
overloads (only arity-1 is in this PoC), generating the full catalog across all
categories, and deciding whether the catalog stays TSV or moves to a typed
source (attributes/JSON). The mechanism itself is proven.

## Step 4 — clause-level decision experiments (#85 UPSERT, #89 MERGE, #88 string-agg): cost is driven by fluent depth, not divergence

Steps 1–3 validated namespace separation on **scalar functions** (the easy case).
The real prize and the real risk are **clause-level** features (UPSERT, MERGE,
string-agg, date arithmetic). #85 (UPSERT) was implemented as the decisive test,
in two layers, both wired into the real library and the suite (now **360/360**).

### What was built

**Shared core (Approach A — neutral API, also real #85 progress, ~96 LOC, paid once):**

- `OnConflictClause` / `OnDuplicateKeyUpdateClause` (`Internal/.../Insert/`)
- `IDbmsDialect.OnConflictExcludedAlias` (`EXCLUDED` / `excluded`) + buffer
  `AppendExcludedReference` — the dialect owns the spelling, no node branches on `Dbms`
- `IInsertBuilderValues.OnConflict/OnDuplicateKeyUpdate` + `IOnConflictBuilder`
- six keywords

This serves **all** DBMS via `Build(Dbms)`; one builder exposes both verbs.
Exact-SQL tests pin PostgreSQL (`EXCLUDED`), SQLite (`excluded`), MySQL
(`AS new ... new.col`) — `tests/.../UpsertTests.cs`.

**Namespace layer (Approach B — per-DBMS filtering, ~92 LOC for *2* DBMS, *not* generated):**

- `SqlArtisan.Databases.PostgreSql` exposes `InsertInto…Values…OnConflict…DoUpdateSet/DoNothing`
- `SqlArtisan.Databases.MySql` exposes `…Values…OnDuplicateKeyUpdate`
- each builds with **no DBMS argument**; the wrong dialect's verb is **absent at
  compile time** (proven by reflection + commented CS0117 in `PerDbmsNamespaceTests`)

### Feasibility verdict: ✅ yes, it works at the clause level too

End-to-end through the real pipeline: PG `ON CONFLICT` and MySQL `ON DUPLICATE KEY
UPDATE` build with no `Dbms` arg, the `EXCLUDED`/`excluded` divergence is handled
in the dialect layer, MySQL's structural `AS new` insertion is absorbed by the
clause (no mutation of the VALUES clause), and each namespace omits the other's
verb. No blocker.

### But the maintenance cost is a *different curve* from scalars

| | Scalar functions (Step 3) | Clause features (this step) |
|---|---|---|
| Unit of divergence | one method name | a fluent **state machine** (Values → OnConflict → DoUpdate…) |
| Per-DBMS facade | **generated** from a 1-line catalog row | **hand-written**: each state is its own type, return types are DBMS-specific → no reuse across namespaces |
| Marginal cost of one DBMS | +1 CSV token | +N wrapper types (one per fluent state) |
| Covered by the Step-3 generator? | yes | **no** (it emits flat method lists, not state machines) |

Concretely (this was the *first* implementation; see the re-measurement below for
a much cheaper one): ON CONFLICT for **PostgreSQL alone** needed **3** wrapper
types (`…InsertColumns/InsertValues/OnConflict`). SQLite (also ON CONFLICT) would
need 3 more — identical shape, different return types, **zero reuse**. MySQL's
variant needed 2. So **one** UPSERT feature across 3 DBMS ≈ **8 hand-written
wrapper types** on top of the shared core. **⚠️ This wrapper count was later shown
to be an artifact of the implementation, not inherent to ③ — see "Re-measurement"
below, where extension methods collapse it to 1 shared type + 7 small methods.**

Two smaller costs also showed up:
- `IDbmsDialect` now carries `OnConflictExcludedAlias`, **unused by 3 of 5**
  dialects. Each divergent construct tends to add such partial-coverage members.
- The neutral API (Approach A) is unavoidable regardless — it's just #85. The
  namespace layer is **additive cost on top**, not a replacement.

### Second sample — #89 MERGE (a full statement, deeper chain)

To check the UPSERT numbers generalise rather than being a one-off, #89 (MERGE,
Oracle / SQL Server) was built the same way (suite now **365/365**).

- **Shared core** (~148 LOC, paid once): `MergeBuilder` + `MergeClauses` +
  `IMergeBuilder` + the `Sql.MergeInto` factory. New dialect divergence: the
  **trailing semicolon** (SQL Server requires it, Oracle forbids it), captured as
  `IDbmsDialect.StatementTerminator` — a **second** partial-coverage member
  (meaningful for 1 of 5). It also forced MERGE to bypass the shared
  `BuildCore` so the `;` abuts the final clause: clause features can perturb even
  the build path, not just add parts.
- **Namespace layer** (~110 LOC for 2 DBMS, not generated): `Oracle.Merge.cs` and
  `SqlServer.Merge.cs`, **4 wrapper types each** (Using → On → When → Insert; the
  terminal reuses the generated `<Dbms>Query`). MERGE's chain is *deeper* than
  UPSERT's, so it costs *more* wrapper types per DBMS — confirming the cost
  scales with **fluent depth**, exactly as predicted.

Pleasant symmetry it also demonstrates: UPSERT lives only in PG/MySql/SQLite
namespaces, MERGE only in Oracle/SqlServer — the five DBMS are **cleanly
partitioned by upsert mechanism**, with no namespace exposing a verb it can't run
(test `Namespaces_Partition_Upsert_And_Merge_By_Dialect`).

### Third sample — #88 string aggregation (the most syntactically divergent feature)

#88 was picked as the stress test: per its own issue it is "the most syntactically
divergent feature in scope." Crucially it is a **function** (a SELECT-list
expression), not a fluent statement — a third shape. Built the same way (suite now
**370/370**):

- **Shared core** (~106 LOC, paid once): three fixed-form nodes —
  `StringAggFunction` (PostgreSQL inline `ORDER BY`), `WithinGroupAggFunction`
  (Oracle `LISTAGG` / SQL Server `STRING_AGG`, `WITHIN GROUP` — *one* node serving
  both, name passed in), `GroupConcatFunction` (MySQL inline `ORDER BY` +
  `SEPARATOR`). No node branches on `Dbms`.
- **Namespace layer**: `PgSql.StringAgg`, `SqlServerSql.StringAgg`,
  `OracleSql.Listagg`, `MySqlSql.GroupConcat` — **one factory method each,
  ZERO wrapper types** (`grep` confirms 0 classes). ~13 LOC per DBMS, mostly
  comments.

Two sharp findings:
- **`STRING_AGG` is the same name for PG and SQL Server yet needs two different
  nodes** (inline `ORDER BY` vs `WITHIN GROUP`). Sharing by name alone is
  impossible — the structure, not the name, is the unit of divergence.
- **MySQL's `SEPARATOR` must be a string literal, not a bind parameter.** The
  `GroupConcat` factory takes `string` (not `object`) and emits it inline. This is
  a real *correctness* divergence the namespace surfaces but cannot fix — more
  evidence that namespaces buy *discoverability*, not *correctness*.

And the decisive contrast: **the most syntactically divergent feature in scope is
*cheap* under namespaces** (one method per DBMS, no wrappers) because it is
**depth 0**. Raw syntactic divergence is NOT the cost driver — *fluent depth* is.

### The cost, generalised (three samples)

The namespace layer's marginal cost for a feature ≈

> **Σ over supporting DBMS of (number of fluent states)** hand-written wrapper
> types — none reusable across namespaces, none covered by the Step-3 generator.
> A flat function (depth 0) costs **one factory method per DBMS and no wrappers**,
> regardless of how divergent its *syntax* is.

| Feature | Shape | Fluent depth | DBMS | Namespace wrapper types | Generatable? |
|---------|-------|-------------|------|-------------------------|------------|
| Scalar fn (Step 3) | function, name-only divergence | 0 | 5 | 0 | **yes** (catalog) |
| String agg (#88) | function, structural divergence | 0 | 4 | **0** (4 factory methods) | no (hand node) |
| UPSERT (#85), wrappers | INSERT-tail clause | 2–3 | 3 | ~8 | no |
| UPSERT (#85), **extensions** | INSERT-tail clause | 2–3 | 3 | **1 shared** + 7 ext methods | no |
| MERGE (#89) | full statement | 4 | 2 | 8 | no |

### Re-measurement (correction): extension-method ③ collapses the wrapper cost

The wrapper counts above measured *one implementation* of ③. A reviewer asked
whether gating the **entry** per namespace and using **namespace-scoped extension
methods** for the mid-chain verbs would absorb the divergence far more cheaply.
It does — the UPSERT namespace layer was re-implemented that way and remeasured:

- The shared builder (`IExtUpsertValues`/`IExtConflictAction`) carries **no
  verbs** (an instance method would shadow a same-named extension). `OnConflict`
  /`DoUpdateSet`/`DoNothing` live as extensions in `…PostgreSql` and `…Sqlite`;
  `OnDuplicateKeyUpdate` in `…MySql`. Terminals return `<Dbms>Query`, so `Build()`
  still folds the DBMS with no argument.
- **Wrapper types: 8 → 1** (a single shared `ExtUpsertBuilder`). Per-DBMS cost is
  now just **7 tiny extension methods** (PG 3 + SQLite 3 + MySQL 1), not bespoke
  state-machine types.

Two claims were verified by actually compiling, not asserted:

- **Filtering is real.** Under `using …PostgreSql;` only, calling the MySQL verb
  fails with **`CS1061` … no accessible extension method 'OnDuplicateKeyUpdate'
  … are you missing a using directive**. (Proof: `UpsertNamespacePostgreSqlTests`.)
- **Multi-import is a real cost.** Importing both `…PostgreSql` and `…Sqlite`
  (which share an `OnConflict`) makes the call **`CS0121` ambiguous** — so a file
  that targets two ON-CONFLICT dialects at once cannot use the unqualified verb.

Residual costs of extension-③ that keep it *above* the neutral baseline:
- **No neutral API.** The verbs exist only as per-namespace extensions; you must
  import a DBMS namespace to write an upsert at all.
- **No-arg `Build()` couples the terminal per DBMS.** `DoUpdateSet`/`DoNothing`
  must return `PostgreSqlQuery` vs `SqliteQuery`, so the ON CONFLICT family is
  **duplicated** across PG and SQLite (hence the CS0121 ambiguity above). Drop
  no-arg `Build()` (use `Build(Dbms)`) and the family collapses to one shared set
  — trading ergonomics for less duplication.
- The shared interfaces must stay **verb-less**, i.e. a parallel surface to the
  neutral builder.

So the earlier "③ ≈ 8 wrapper types" overstated ③'s clause cost; **extension-③ is
~`1 shared type + one small method per (verb × dialect)`** — close to, but still
above, ②'s ~4 *shared* methods (which reuse one builder and fold the DBMS at
`Build(Dbms)`, dialect handling `EXCLUDED`/`excluded`). For fluent clauses the
ranking is now **② < ③-extensions ≪ ③-wrappers**, a much narrower gap than before.

So there are **three** cost tiers, set by *fluent depth*, not by syntactic
divergence:
1. **Depth 0, name-only** (scalars): generated, ~free.
2. **Depth 0, structural** (string agg): hand-written node + one factory per DBMS,
   **no wrappers** — and here the namespace approach is actually *cleaner than
   neutral*: each namespace uses a fixed-form node, whereas a single neutral
   `Sql.StringAgg` would have to push the structural variance into the dialect
   layer (a richer `IDbmsDialect` contract, brushing against "don't rewrite the
   user's SQL"). This **inverts** the clause finding.
3. **Depth ≥ 2** (UPSERT/MERGE): with extension methods the per-DBMS cost is a
   handful of small methods (not state machines), but neutral is still slightly
   cheaper and keeps a portable API.

### What this means for the Q3 decision

The relative cost of namespaces-vs-neutral is **feature-shape-dependent**, which
is exactly the case for a hybrid:

- **Functions (depth 0), whether name-only or structural:** namespaces are cheap
  and often *cleaner* than neutral. Worth doing.
- **Fluent clauses (depth ≥ 2):** ② (per-dialect methods) is the cheapest and
  keeps a neutral API, but **③ via extension methods is now a close, legitimate
  alternative** if the *compile-time* guarantee (wrong verb absent, not merely
  present-but-mis-named) is valued — its cost is small methods, not wrapper
  machines. ③-via-wrappers is the only clearly-dominated option.

Three honest options, in increasing investment:

1. **Per-dialect methods only (roadmap #91 as-is).** All divergence (functions and
   clauses) lives as differently-named methods on one `Sql` (Approach A). No
   namespaces. Lowest cost; the wrong call is discoverable-but-present.
2. **Hybrid, split by shape (now the sharpest line).** Namespaces for **all
   functions** — depth 0, cheap, often *cleaner* than neutral (scalars generated;
   divergent functions like string-agg = one factory each). Per-dialect **methods**
   for **fluent clauses** (UPSERT/MERGE) where namespaces cost ~N× wrapper
   machines. The two mental models map cleanly onto "function vs statement".
3. **Namespaces everywhere — via extension methods (re-measured), not wrappers.**
   Entry gated per namespace + mid-chain verbs as namespace-scoped extension
   methods. Cost is small methods, not state machines (UPSERT: 1 shared type + 7
   methods). Gives a *compile-time* wrong-verb guarantee. Costs: no neutral API,
   per-DBMS terminal duplication for no-arg `Build()`, and multi-import ambiguity
   (CS0121) when two same-verb dialects are imported together. The old
   "needs a fluent-builder generator" objection was specific to the *wrapper*
   implementation and no longer applies.

**Recommendation:** four samples show the cost is driven by **fluent depth**, not
syntactic divergence, and that namespaces *win for functions* outright. For fluent
clauses the gap narrowed once ③ was re-measured with extension methods:
**② (hybrid) remains the lowest-cost and keeps a neutral API**, but **③-extensions
is a close, defensible alternative** when the compile-time guarantee is prized.
Net: lean **option 2 (hybrid)** — namespaces for the whole function surface,
per-dialect methods for statement-level clauses — adopting **③-extensions for a
clause only where the compile-time guard is worth the lost neutral API**. Reserve
the wrapper form (the dominated option) for never. Decide before 1.0.

## Findings & recommendation (after the spike)

1. **Feasible, and the philosophy fits.** Per-DBMS namespaces are the logical
   endpoint of "you write for one DBMS." Generation removes the maintenance
   blocker; tagging closes the mixing hole cheaply.
2. **The matrix is the real deliverable and the real risk.** It is the
   foundation for *both* this approach and a cheaper Roslyn-analyzer approach.
   Build it first; verify every cell against vendor docs; cover it with tests.
3. **Sequence the bet, don't leap.** Recommended path:
   - **(a)** Finish the matrix for all categories (this spike = 1 of ~6).
   - **(b)** Ship a **Roslyn analyzer** over the *existing* single `Sql` first
     — non-breaking, validates the matrix against real code, ~90% of the
     correctness benefit at a fraction of the cost.
   - **(c)** Only if the filtered-IntelliSense UX is still wanted, add the
     generator + per-DBMS namespaces + tag validation, and remove `SqlArtisan.Sql`
     / `Build(Dbms)` (breaking).
4. **Set expectations honestly in docs:** this is *discoverability*, not
   *portability* and not full *correctness*. Semantic divergences (rounding
   mode, type restrictions, operator-vs-function) are out of scope by design.

## Open questions for the maintainer

- Where is the per-project DBMS declared for the analyzer in (b)? (`.csproj`
  property? assembly-level attribute? `SqlArtisanConfig`?)
- Do we keep a `SqlArtisan.Common` facade for the universal functions to cut
  duplication, or is a flat per-DBMS list more discoverable even at the cost of
  repetition?
- Is build-time (throwing) mixing-validation acceptable, or must it be
  compile-time (→ phantom types, with the ergonomic cost)?
- ~~**Namespace naming**~~ — **RESOLVED**: adopted the `SqlArtisan.Databases.<Dbms>`
  prefix (see the Step 2 finding above).
