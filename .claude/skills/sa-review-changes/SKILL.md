---
name: sa-review-changes
description: Review a SqlArtisan change, PR, or diff for correctness, ADR conformance, convention consistency, and doc alignment. Use when the user asks to review changes/a PR/a diff in this repo, or to check a feature before pushing. Adds SqlArtisan-specific checks (ADRs, dialect grammar, allocation budget) on top of a generic code review, and verifies behavior empirically by building and running a throwaway harness rather than reasoning from memory. Reports defects only by default; pass an `improve` argument (or ask for improvement/idiom/style suggestions, or a "thorough"/"deep" review) to also surface non-defect better-way suggestions.
---

# Review SqlArtisan changes

This complements the generic `code-review` skill with the checks that catch the
*SqlArtisan-specific* problems: ADR violations, dialect-grammar bugs, and
allocation regressions. The highest-value move is **verifying empirically** —
build a throwaway console that references the project and observe the real SQL,
the real failure modes, and the real allocations. Do not reason about emitted
SQL or DBMS grammar from memory.

**Mode.** By default this review reports **defects only** — things that are
wrong (§8). When invoked with an `improve` argument — or the user asks for
improvement / idiom / style suggestions, or a "thorough" / "deep" review — also
surface the **improvement** axis (§8): better ways to write something that is
not wrong. Everything else (§1–§7 investigation, the gates, empirical
verification) runs identically in both modes; the mode only changes what §8
lets through and how the Report is grouped.

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
  Allman braces, file encoding). `[*.cs]` sets `charset = utf-8` — UTF-8 **without**
  a BOM — and `dotnet format` enforces it: a BOM'd `.cs` fails with
  `error CHARSET: Fix file encoding.` (#134). So a whole-file rewrite must not
  reintroduce a BOM.

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
- **0007 — validity enforcement boundary.** Misuse fails loudly at the right
  layer: compile time via return-type narrowing where possible, else a thrown
  guard — never a silently emitted invalid/wrong statement. For anything that
  can be *empty* (conditions, select lists, collections), check the change
  against the empty-state policy in `.claude/rules/guards-and-empty-states.md`
  (eager-throw vs Build()-time throw — written clauses are never elided), and
  that guard messages follow its grammar with exact-message tests.

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
- **Mutable builder fields returning `this`** are acceptable as an
  implementation — there is precedent (`SortOrder.SetNullOrdering`). Not a
  finding on its own, **but** the reuse contract is (#245): a stage call after
  `Build()` must throw once the freeze-after-Build guard lands, and docs must
  never *promise* mutation (the contract wording is "a partial chain is
  one-way"), keeping copy-on-write open as a later bug-fix. Flag docs or code
  that locks mutation in.
- **Comment audit.** Comments are part of the diff — hold them to the same bar.
  Run the `.claude/rules/code-comments.md` smell checklist (1–8) and length
  defaults over every added or changed `//` and `///`; delete restatements and
  message-echoes, and trim anything over the defaults to why-only. AI-written
  diffs skew verbose here, so this is a required pass, not an optional one.

## 5. Verify empirically with a throwaway harness

Static reading misses grammar bugs, missing enforcement, and allocation
regressions. **Build a throwaway console and run it** — use the
`sa-run-sql-harness` skill, which has the full setup and templates. The three checks
to run for a review:

- **SQL per dialect** — `Build(Dbms.X)` for every DBMS the construct targets;
  confirm `sql.Text` is valid for *that* DBMS's grammar (§6).
- **Negative / enforcement** — a misuse (e.g. a mandatory clause omitted) must
  throw, not emit invalid SQL.
- **Hazard shapes** — the four shapes that produced the #225 audit's silent
  failures (the harness skill has the code): all-`ConditionIf`-off, a nested
  all-empty OR/AND group beside an active condition, a held builder prefix
  built along two branches, and a correlated UPDATE/DELETE with an unaliased
  target. Run whichever the diff could plausibly affect.
- **Allocation** — probe with `GC.GetAllocatedBytesForCurrentThread` to back any
  ADR 0006 claim; the formal suite is `tests/SqlArtisan.Benchmark`.

## 6. Don't trust memory on DBMS grammar

Wrong-DBMS facts are the easiest way to ship a bug. State each grammar claim as
something you verified, and prefer a per-dialect API that lets the author pick
the form. Known sharp edges:
- MySQL `GROUP_CONCAT ... SEPARATOR` requires a **literal** (a `?` placeholder is
  a parse error) and silently truncates at `group_concat_max_len` (1024 B).
- Oracle `LISTAGG` **requires** `WITHIN GROUP (ORDER BY ...)`.
- `STRING_AGG` ordering differs: PostgreSQL inline `ORDER BY`, SQL Server
  `WITHIN GROUP`.
- MySQL's default `sql_mode` parses `||` as **logical OR**, not concatenation —
  valid SQL with silently different semantics (#234).
- MySQL rejects `LIMIT` inside `IN`/`ANY`/`ALL`/`SOME` subqueries while
  allowing it in scalar position — context-bounded, not matrix-expressible
  (#240).
- T-SQL cannot alias the direct UPDATE/DELETE target (`UPDATE t AS x` is
  invalid); the alias comes via the `FROM` form (#239/#237).

## 7. Check docs match the source

For every changed surface, confirm the doc/comment coverage matches current
behavior — this is the mechanics of *where* to look; §8 sets the bar for
*whether* to report what you find there:
- **XML docs** on the public factory and node — the described form equals what
  `Format` emits; `cref`s resolve (covered by §2's 0-warning bar).
- **README** — the function reference list, the table-of-contents entry, and any
  usage example all show the real output (real parameter markers / literals).
- **CHANGELOG** — an `[Unreleased]` entry exists and names the caveats (e.g. the
  MySQL truncation note).
- **CLAUDE.md / docs/adr/ / `.claude/skills/` / `.claude/rules/`** — for a
  change that touches conventions, structure, or process, confirm these
  still describe the result accurately.

## 8. Two axes: defects (always) vs. improvements (opt-in)

Every finding is either a **defect** — something wrong that must be fixed — or
an **improvement** — a better way to write something that is *not* wrong.
They get opposite default treatment; conflating them is what turns review into
noise. Decide which a finding is **before** writing it down.

### Defects — always reported (both modes)

**Code.** Reportable only if it (a) produces wrong/invalid output for a
permitted input, (b) violates a specific ADR clause or a rule in
`.claude/rules/` — cite which — or (c) **reinvents a shared pattern that
already exists in-repo for this exact recurring shape** (e.g. a `*Core` base
class built to unify near-identical `Format` logic across sibling functions)
— cite the precedent by file. (c) is a defect, not a preference, because the
divergence breaks consistency with an *established* convention; a factoring
you'd merely have done differently, with no such precedent to cite, is an
improvement, not a defect.

**Docs & comments** — where a reader is left **misinformed**, regardless of
severity or fix cost (a cheap fix that misinforms is exactly the case worth
flagging). Scope: `README.md`, `docs/**`, `CHANGELOG.md`, XML `///` and inline
`//` comments — public **and** internal (`Internal/`'s `CS1591` suppression
exempts it from doc-*generation*, not from being read by the next contributor)
— plus `CLAUDE.md`, `docs/adr/**`, `.claude/skills/**`, `.claude/rules/**`.
Three shapes:
- **Omission** — new/changed behavior with no doc/comment coverage where its
  siblings have it (a `Sql.*` factory missing the XML summary every sibling
  has; no `[Unreleased]` entry for a user-visible change; a new ADR/convention
  not in CLAUDE.md's map).
- **Inaccuracy** — something the current code contradicts: a clause described
  as optional the code now requires, a wrong dialect claim, a stale example, a
  stale count/file list — **and** an example the docs *explicitly present as
  the recommended/idiomatic form* that a newer, simpler API has since
  superseded (the recommendation claim is now false; name the superseding
  API). An example that merely *uses* an older idiom without claiming it is
  preferred is not inaccurate — it is an improvement (below).
- **Misleading ambiguity** — wording a reader could plausibly misread into an
  incorrect belief about behavior, not merely wording you'd have chosen
  differently.

### Improvements — reported only in `improve` mode

A better way to write something that is not a defect. In default mode these
are **silent** — not downgraded to a passing nit, not "worth mentioning".
Omitting them is the point. When the mode is on:
- **Code** — a helper you'd have named or factored differently; a duplication
  with a cleaner solution that is not (yet) an established in-repo pattern; a
  simplification that no ADR/rule requires.
- **Docs** — an example that still runs and makes **no** false recommendation
  claim, but no longer uses the current idiom once a simpler API covers the
  same case (name that API). This repo's docs are read by AI coding assistants
  as much as humans (ADR 0010, `llms.txt`, `docs/guides/ai-assistants.md`), so
  a stale idiom can be reproduced verbatim in generated code — worth
  surfacing when improvements are asked for, but not a defect.

A suggestion must still be **concrete** (name the alternative) and must not
contradict an ADR — you cannot pitch as an "improvement" something ADR 0001 or
`guards-and-empty-states.md` deliberately rejects. Beyond that, `improve` mode
is where general idiom and taste are legitimately in scope, *because the user
asked* — the discipline is the opt-in gate, not a citation requirement.

### Out of scope on both axes

- A rewording that changes nothing a reader could conclude — pure phrasing
  preference with no ambiguity, no factual gap, no idiom shift.
- Comment *quality* (verbosity, restating, filler): a separate pass — the
  `.claude/rules/code-comments.md` smell checklist in §4. This axis governs
  only whether the words stayed *true and complete*, not how many there are.

## Report

Lead with the verdict (mergeable or not) and a short list of recommended
actions, most important first. Then two zones, each finding tagged with a
`file:line` and severity (**High/Medium/Low**):

- **Defects (must fix)** — everything that cleared §8's defect bar: wrong/
  invalid SQL, ADR violations, established-pattern divergence, and every
  doc/comment sync gap found, however small (never fold one into a passing
  mention or drop it for being "just docs"). A doc gap can be Low severity and
  still must-fix — severity ranks the queue, it does not gate inclusion.
- **Suggestions (optional, non-blocking)** — the improvement axis, present
  **only** when the skill ran in `improve` mode. Never mix these into the
  must-fix zone, and never emit this zone at all in default mode.

Keep separate from both zones the "discuss" items — permissive-API trade-offs
the ADRs deliberately leave to the author / analyzer, not review findings.
