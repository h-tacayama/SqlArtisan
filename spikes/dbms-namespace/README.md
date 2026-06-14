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

## Step 4 — clause-level decision experiment (#85 UPSERT): feasible, but a different cost curve

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

Concretely: ON CONFLICT for **PostgreSQL alone** needed **3** wrapper types
(`…InsertColumns/InsertValues/OnConflict`). SQLite (also ON CONFLICT) would need
3 more — identical shape, different return types, **zero reuse**. MySQL's variant
needed 2. So **one** UPSERT feature across 3 DBMS ≈ **8 hand-written wrapper
types** on top of the shared core — and #86/#88/#89 are all clause-shaped too.

Two smaller costs also showed up:
- `IDbmsDialect` now carries `OnConflictExcludedAlias`, **unused by 3 of 5**
  dialects. Each divergent construct tends to add such partial-coverage members.
- The neutral API (Approach A) is unavoidable regardless — it's just #85. The
  namespace layer is **additive cost on top**, not a replacement.

### What this means for the Q3 decision

- **For scalars, namespaces are nearly free** (generated) and genuinely polished.
- **For clauses, namespaces cost ~N× hand-written state machines** for a *modest*
  marginal benefit over the roadmap's already-chosen "per-dialect methods in one
  namespace": the difference is *"wrong verb absent"* vs *"wrong verb present but
  named for another dialect."* Both reach the same shared core.

Three honest options, in increasing investment:

1. **Per-dialect methods only (roadmap #91 as-is).** Ship `OnConflict` /
   `OnDuplicateKeyUpdate` on one builder (Approach A). No namespace layer for
   clauses. Lowest cost; the wrong verb is discoverable-but-present.
2. **Hybrid.** Namespaces for scalar functions (generated, cheap); per-dialect
   methods for clause features. Best cost/benefit, but two mental models.
3. **Namespaces everywhere.** Requires extending the generator to emit fluent
   state machines (modeling transitions, not flat lists) — real engineering — or
   accepting the per-DBMS wrapper boilerplate forever.

**Recommendation:** the scalar PoC's "③ is great" does **not** transfer
unconditionally to clauses. Lean **option 2 (hybrid)** — or **option 1** if a
single mental model is valued over the compile-time guarantee. Reserve full
option 3 for after a fluent-builder generator exists. Decide before 1.0.

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
