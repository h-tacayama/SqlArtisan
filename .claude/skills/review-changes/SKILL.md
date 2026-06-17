---
name: review-changes
description: Review a SqlArtisan change, PR, or diff for correctness, ADR conformance, convention consistency, and doc alignment. Use when the user asks to review changes/a PR/a diff in this repo, or to check a feature before pushing. Adds SqlArtisan-specific checks (ADRs, dialect grammar, allocation budget) on top of a generic code review, and verifies behavior empirically by building and running a throwaway harness rather than reasoning from memory.
---

# Review SqlArtisan changes

This complements the generic `code-review` skill with the checks that catch the
*SqlArtisan-specific* problems: ADR violations, dialect-grammar bugs, and
allocation regressions. The highest-value move is **verifying empirically** —
build a throwaway console that references the project and observe the real SQL,
the real failure modes, and the real allocations. Do not reason about emitted
SQL or DBMS grammar from memory.

## 1. Scope the diff to the actual change

The local `main` is often stale, so `git diff main...HEAD` shows unrelated
already-merged work. Diff against the branch point instead:

```bash
git log --oneline <base>..HEAD          # the PR's own commits
git diff <base>..HEAD --stat            # files the PR actually touches
```

Find `<base>` from the first commit of the branch (or the PR base). Review only
those files.

## 2. Run the gates first

A finding the tools already catch is wasted review budget. Run them up front:

```bash
dotnet build src/SqlArtisan/SqlArtisan.csproj -c Release 2>&1 | grep -iE "warning|error|succeeded"
dotnet test tests/SqlArtisan.Tests
dotnet format SqlArtisan.sln --verify-no-changes ; echo "format exit=$?"
```

- The core lib builds with `AnalysisMode=Recommended` (see `Directory.Build.props`):
  **0 warnings is the bar**, including XML-doc `cref` resolution (CS1574).
- Tests assert exact SQL strings — any output drift fails here.
- `format` exit 0 means `.editorconfig` is satisfied (indent, `var` policy,
  Allman braces, file encoding). `[*.cs]` has no `charset` rule, but match the
  repo's BOM convention (existing `.cs` files are UTF-8 **with** BOM).

## 3. Check conformance to the ADRs

`docs/adr/` is the source of truth for design intent. Read the relevant ones and
check the diff against each:

- **0001 — faithful output.** No abstraction that rewrites the user's SQL for
  portability. Each construct emits what the author wrote.
- **0002 — DBMS differences.** *Token* differences (quoting, parameter marker,
  excluded-row casing) live behind `IDbmsDialect`; nodes **never branch on
  `Dbms`**. *Construct* differences (UPSERT, string aggregation) are separate
  per-dialect public methods, not one "portable" method.
- **0004 — auto-parameterization.** Values bind as parameters. The exception is
  a position whose **grammar requires a literal** (e.g. MySQL
  `GROUP_CONCAT ... SEPARATOR`) — those emit an escaped inline literal *by
  design*. Verify any new literal-emitting path is actually such a case, and
  that it escapes (`'`→`''`, and for MySQL `\`→`\\`).
- **0005 — public API location.** Public surface only in `src/SqlArtisan/Sql/Sql.*.cs`
  and `src/SqlArtisan/SqlBuilder/`. Types under `Internal/` are implementation
  detail even when `public`.
- **0006 — allocation-light.** Build-path allocation is a first-class constraint.
  Complexity in the hot path (`Format`) is justified **only by a measured**
  allocation win; otherwise prefer simplicity. Back any perf claim with a number
  (§5), and note that the official benchmark is `tests/SqlArtisan.Benchmark`.

## 4. Check consistency with existing conventions

- **Naming** follows the CLAUDE.md token rule (underscores are the only word
  boundary): `STRING_AGG`→`StringAgg`, `GROUP_CONCAT`→`GroupConcat`,
  `LISTAGG`→`Listagg`.
- **`Keywords.cs`** stays alphabetical; reuse an existing constant if present.
- **Mandatory trailing clause?** Enforce it with the two-type "pending" pattern,
  like `PercentileContFunction` → `.WithinGroup(...)` → `PercentileFunction`
  (and `ListaggFunction` → `ListaggWithinGroupFunction`). The pending type is
  **not** a `SqlExpression`, so omitting the clause throws at `Select(...)`
  instead of silently emitting invalid SQL. Use this when a DBMS *requires* the
  clause (Oracle `LISTAGG` needs `WITHIN GROUP`); keep it optional when the
  clause is genuinely optional (SQL Server `STRING_AGG`, MySQL `GROUP_CONCAT`
  ordering).
- **Mutable builder fields returning `this`** are acceptable — there is
  precedent (`SortOrder.SetNullOrdering`). Not a finding on its own.

## 5. Verify empirically with a throwaway harness

Build a console app in `/tmp` that references the project and **run it**. This is
what catches grammar bugs, missing enforcement, and allocation regressions that
static reading misses.

```bash
mkdir -p /tmp/sa-review && cd /tmp/sa-review
cat > Demo.csproj <<'XML'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>
  <ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup>
  <ItemGroup><ProjectReference Include="/home/user/SqlArtisan/src/SqlArtisan/SqlArtisan.csproj" /></ItemGroup>
</Project>
XML
# write Program.cs (see patterns below), then:
dotnet run -c Release
```

Define a table inline:
```csharp
internal sealed class T : DbTableBase
{
    public T() : base("users", "") { Name = new DbColumn("", "name"); }
    public DbColumn Name { get; }
}
```

Three checks worth running:

- **SQL per dialect** — `Build(Dbms.X)` for every DBMS the construct targets;
  print `sql.Text` and confirm it is valid for *that* DBMS's grammar. Print
  parameters via `sql.Parameters.ForEach((n,v) => ...)`.
- **Negative / enforcement** — a misuse should fail loudly, not emit invalid SQL.
  e.g. a mandatory clause omitted should throw `ArgumentException` from
  `Select(...)`:
  ```csharp
  try { var s = Select(Listagg(u.Name, ", ")).From(u).Build(Dbms.Oracle);
        Console.WriteLine("LEAK: " + s.Text); }
  catch (Exception ex) { Console.WriteLine($"GUARD: {ex.GetType().Name}"); }
  ```
- **Allocation** — for ADR 0006, measure with the thread allocation counter
  (quick probe; the formal suite is BenchmarkDotNet):
  ```csharp
  static long Alloc(Action a, int n){ a(); long b=GC.GetAllocatedBytesForCurrentThread();
      for(int i=0;i<n;i++)a(); return (GC.GetAllocatedBytesForCurrentThread()-b)/n; }
  ```
  Useful facts confirmed this way: `string.Replace` returns the same ref (0 B)
  when there is no match; `char.ToString()` costs ~24 B; the literal-separator
  build path is lighter than the bound-parameter path.

## 6. Don't trust memory on DBMS grammar

Wrong-DBMS facts are the easiest way to ship a bug. State each grammar claim as
something you verified, and prefer a per-dialect API that lets the author pick
the form. Known sharp edges:
- MySQL `GROUP_CONCAT ... SEPARATOR` requires a **literal** (a `?` placeholder is
  a parse error) and silently truncates at `group_concat_max_len` (1024 B).
- Oracle `LISTAGG` **requires** `WITHIN GROUP (ORDER BY ...)`.
- `STRING_AGG` ordering differs: PostgreSQL inline `ORDER BY`, SQL Server
  `WITHIN GROUP`.

## 7. Check docs match the source

For user-visible changes, confirm the emitted SQL matches every doc surface:
- **XML docs** on the public factory and node — the described form equals what
  `Format` emits; `cref`s resolve (covered by §2's 0-warning bar).
- **README** — the function reference list, the table-of-contents entry, and any
  usage example all show the real output (real parameter markers / literals).
- **CHANGELOG** — an `[Unreleased]` entry exists and names the caveats (e.g. the
  MySQL truncation note).
- Watch for **doc/behavior drift**: a doc saying a clause is "mandatory" while the
  code allows omitting it is a real finding — fix one to match the other.

## Report

Group findings by the user's criteria and tag each with severity
(**High/Medium/Low/Nit**) and a `file:line`. Lead with the verdict
(mergeable or not) and a short list of recommended actions, most important
first. Separate "must fix" (bugs, ADR violations, invalid SQL) from "discuss"
(permissive-API trade-offs the ADRs deliberately leave to the author / analyzer).
