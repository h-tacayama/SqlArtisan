---
name: sa-release-audit
description: The release-audit procedure — drive the whole codebase to review convergence before a release. Freeze feature work, run the full audit battery (layer-scoped sa-audit-sweep runs jointly covering the source and test layers of CLAUDE.md's Layout table, sa-panel-audit on the high-stakes bounded scopes, sa-docs-audit on the docs corpus), triage every verified finding into a durable closure, and repeat fresh passes until ADR 0022's termination criterion is met. Not a replacement for sa-audit-sweep — it orchestrates it: a one-off audit of a single scope uses sa-audit-sweep (or sa-panel-audit) directly; use this skill only for a release-gating, run-until-converged audit of everything.
---

# Release audit: run the battery until it converges

One `sa-audit-sweep` run audits one scope once and stops; it knows nothing
about what "done" means. This skill owns the loop around it: which runs
make up a *full pass*, what happens to each finding, and when repeated
passes may stop — the termination criterion is **ADR 0022** (K = 2
consecutive clean-of-new-defect passes; read it, don't re-derive it).

## 1. Freeze

Feature work stops for the audit window. Every triage-driven change (fix
or record) moves the tree, and ADR 0022 only counts passes that postdate
the last such change — concurrent feature churn resets the counter
indefinitely.

## 2. The battery — what one pass is

One pass is **all** of the following, on the same tree. A subset — however
clean — is not a pass (ADR 0022: two `paths:`-scoped runs of one layer
must not "converge" a codebase 90% unexamined).

Layer-scoped `sa-audit-sweep` runs (pass each row as `args.paths`; the
rows partition the workflow's own `FULL_CODEBASE_GLOBS`, so together they
cover exactly what the full-codebase sweep would, without its
synthesis-context risk):

| Slice | `paths` globs |
|-------|---------------|
| Public surface | `src/SqlArtisan/Sql/*.cs`, `src/SqlArtisan/SqlBuilder/**`, `src/SqlArtisan/SqlPart/**`, `src/SqlArtisan/Metadata/**` |
| Expression/clause nodes | `src/SqlArtisan/Internal/SqlPart/**` |
| Builders, dialects, guards | `src/SqlArtisan/Internal/SqlBuilder/**`, `src/SqlArtisan/Internal/Extensions/**` |
| Analyzer | `src/SqlArtisan.Analyzers/**` |
| Companion packages | `src/SqlArtisan.ArrayBind/**`, `src/SqlArtisan.Dapper/**`, `src/SqlArtisan.TableClassGen/**` |
| Tests | `tests/SqlArtisan.Tests/**`, `tests/SqlArtisan.Benchmark/**`, `tests/SqlArtisan.Analyzers.Tests/**`, `tests/SqlArtisan.IntegrationTests/**`, `tests/SqlArtisan.TableClassGen.Tests/**` |

Plus, on top of (not instead of) the sweep slices — deliberate double
coverage where a defect is costliest:

- **`sa-panel-audit`** on the high-stakes bounded scopes: the dialect
  layer (`src/SqlArtisan/Internal/SqlBuilder/DbmsDialect/**`), the
  guard/empty-state surface (the statement builders' validation guards),
  and the analyzer core. Respect that skill's bounded-scope rule — slice
  a scope that runs past a few dozen files.
- **`sa-docs-audit`** on the docs corpus (README, `docs/**`, `llms.txt`,
  `CHANGELOG.md`).

## 3. Triage — every verified finding lands durably

For each finding that survived adversarial verification, pick exactly one
landing (CLAUDE.md's durable-closure convention — the one-off fix alone
never closes a finding):

- **Fix + gate** — change the code and add the test that keeps the
  finding's *class* closed.
- **Rule or ADR clause** — when the finding is real but the judgment
  criterion was unwritten: a sentence in `.claude/rules/` or an ADR. This
  is also the mandatory route when two passes judged the same shape
  differently — a flip indicts the rule, not the code (ADR 0022).
- **Recorded decline** — design-level → an ADR; convention-level → a
  rules clause; below both bars → a line in
  `.claude/rules/review-declines.md`. Adjudicate any
  `possibly decided by RD-NNN` tags per that file's protocol, and promote
  each confirmed match to a precisely worded record.

Output below §9's defect bar may be acted on or declined, but per
ADR 0022 it never affects the convergence counter — and the bar is judged
per finding, not per report tier: a §9 defect filed under SHOULD DISCUSS
still counts.

## 4. The loop

Repeat: run the battery → triage → (if anything moved the tree) run the
battery again on the new tree. Stop when ADR 0022 is met — two
consecutive full-battery passes, postdating the last triage-driven
change, with zero new defect-bar findings surviving verification.

Track the counter in the release tracking issue, one line per pass: date,
tree SHA, battery completeness, and the count of new defect-bar findings.
The issue is bookkeeping only — the binding records live in the tree.
