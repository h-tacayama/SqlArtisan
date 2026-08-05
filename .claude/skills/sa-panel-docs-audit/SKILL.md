---
name: sa-panel-docs-audit
description: High-confidence audit of the SqlArtisan documentation corpus (README, docs/, llms.txt, CHANGELOG) by an independent three-model panel (Sonnet, Opus, Fable), adjudicated by the main agent. Same corpus-wide scope as sa-docs-audit — not diff-scoped — but audited three times over by models that cannot see each other's work or yours; the main agent then re-derives every finding against primary sources and issues the verdict. Use before a release or a docs overhaul, or when explicitly asked for a multi-model / independent / panel docs audit. Costs roughly three full audits — do NOT use for a routine docs check (`sa-docs-audit`). For a branch diff instead of the corpus, use `sa-panel-diff-review`.
---

# Panel audit of the documentation corpus

The panel *method* is defined by **`sa-panel-diff-review`** — its
"What this supersedes" section and its §3–§6 (briefing discipline, dispatch,
adjudication, report) apply here **unchanged**, and they are the substance of
this skill. Read that file; only its §1 and §2, restated below, differ.

Nothing about the method changes because the target does: the reason three
independent models beat one is the same whether they are reading C# or prose,
and docs defects are exactly where the panel has already paid off — the lone
finding on PR #427 that only Opus caught was a stale docs example.

## 1. Fix the scope

The rubric is `sa-docs-audit`, and the scope is what that skill audits: the
**whole documentation corpus**, regardless of what the current branch changed
— README, `docs/**`, `llms.txt`, `CHANGELOG.md`, and the package READMEs.

Do not narrow this to a diff. Corpus-wide coverage is the entire reason the
audit rubric exists: the coverage and link defects it hunts (an undocumented
public API, a phantom API, a broken anchor) are structurally invisible to a
diff review, so a diff-scoped panel here would triple the cost of a check that
cannot find them. For docs changes inside a branch, `sa-panel-diff-review`
already covers them.

## 2. Run the deterministic checks once, up front

Same rule as the diff panel's §2 — mechanical output is not what must stay
independent, so run it yourself rather than paying three panelists to
re-derive identical results. Here that means `sa-docs-audit`'s four bundled
scripts:

```bash
S=.claude/skills/sa-docs-audit/scripts
python3 $S/check_links.py
python3 $S/check_api_coverage.py
python3 $S/check_terms.py
python3 $S/verify_sql_examples.py
```

Pass the verbatim output into the briefing and tell panelists not to re-run
them — but say they *may* if they doubt a result.

Unlike a failing build gate, a non-zero exit here does **not** block convening
the panel: these scripts are heuristics, and judging their hits is part of the
audit rather than a precondition for it. Hand the panel the raw output and let
each seat judge the hits independently.
