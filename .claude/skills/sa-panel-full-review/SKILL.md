---
name: sa-panel-full-review
description: High-confidence review of a SqlArtisan scope in its entirety — a subsystem, a layer of the Layout table, or the docs corpus — by an independent three-model panel (Sonnet, Opus, Fable), adjudicated by the main agent. Unlike sa-panel-diff-review it ignores what the branch changed and reads the named scope as it currently stands, so it finds standing defects no diff review can see. Use for a pre-release audit, a subsystem review, or when explicitly asked for a multi-model / independent / panel audit of existing code or docs. The scope must be bounded — name it (a path glob, a subsystem, the docs corpus); for the whole ~700-file codebase use the sa-review-sweep workflow instead, which chunks it. Costs roughly three full reviews of whatever you name.
---

# Panel review of a whole scope

Same panel *method* as `sa-panel-diff-review`, pointed at existing code rather
than a change. Its "What this supersedes" section and its §3–§6 (briefing
discipline, dispatch, adjudication, report) apply here **unchanged** and are
the substance of this skill — read that file. Only its §1 and §2, restated
below, differ.

What changes is what the panel can find. A diff review can only judge what the
branch touched; this one judges the scope as it stands, so a defect sitting
there since before the branch is in scope rather than invisible.

Two things read across with the obvious substitution: §3's briefing includes
the resolved file list where a diff review would carry the diff range and PR
body (there is no change to make claims about, so there are none to refute),
and §6's "before merging" becomes whatever the audit gates — a release, a
refactor, nothing at all.

## 1. Fix the scope — it must be named and bounded

Take the scope from the request and state it explicitly before dispatching: a
path glob, a subsystem (`src/SqlArtisan.Analyzers/**`), one layer of
CLAUDE.md's Layout table, or the docs corpus (README, `docs/**`, `llms.txt`,
`CHANGELOG.md`). Resolve it to a concrete file list with Glob — not with
`git diff`, which is the other skill's job.

**Bounded is not a style preference — it is what makes the panel possible.**
Every panelist reads the *same* scope, so that scope has to fit one context
three times over. `src` + `tests` is ~700 files; `sa-review-sweep` chunks that
across many single-reviewer agents and still warns past 40 chunks. Tripling it
is not a bigger version of this skill, it is a different tool.

So if the named scope runs past roughly a few dozen files, either slice it
(one Layout-table layer per run) or hand the volume to `sa-review-sweep` and
reserve the panel for the slice that carries the real risk. Either way, say in
the report which slices you did **not** cover — silently reviewing a subset of
what you were asked for is the failure this skill exists to avoid.

## 2. Run the deterministic checks once, up front

Same rule as the diff panel's §2 — mechanical output is not what must stay
independent, so run it yourself rather than paying three panelists to
re-derive identical results. Which checks depends on the scope:

**Code scope** — the full solution, not just the core project, since the scope
may reach the analyzer or TableClassGen:

```bash
dotnet build SqlArtisan.sln -c Release
dotnet test tests/SqlArtisan.Tests
dotnet test tests/SqlArtisan.Analyzers.Tests
dotnet test tests/SqlArtisan.TableClassGen.Tests
dotnet format SqlArtisan.sln --verify-no-changes
```

**Docs scope** — `sa-docs-audit`'s four bundled scripts:

```bash
S=.claude/skills/sa-docs-audit/scripts
python3 $S/check_links.py
python3 $S/check_api_coverage.py
python3 $S/check_terms.py
python3 $S/verify_sql_examples.py
```

Pass the verbatim output into the briefing and tell panelists not to re-run it
— but say they *may* if they doubt a result.

A failing **build or test** gate blocks the panel: fix it first rather than
spending three reviews on code the toolchain already rejects. A non-zero exit
from a **docs script** does not — those are heuristics, and judging their hits
is part of the audit, so hand the panel the raw output and let each seat judge
independently.
