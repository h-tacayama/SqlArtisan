# ADR 0022 — Review convergence: the termination criterion for release audits

**Status:** Accepted

## Context

Toward 1.0, repeated review passes over the codebase were not converging:
each pass reached different judgments, and no pass returned zero findings
(#506). That is structural, not a thoroughness problem. A review samples
from the space of things one *could* mention; independent passes sample
different points, so a raw finding count never stabilizes at zero —
especially for model-run reviews, and especially while preference-grade
findings are admissible. And a decline with no durable record is re-raised
by every fresh-context pass, round after round.

Most of the convergence machinery already existed: the durable-closure
convention and the reporting bar (CLAUDE.md), the defect bar
(`sa-diff-review` §9), adversarial verification (§10), and fresh-context
passes at audit scale (`sa-audit-sweep`, `sa-panel-audit`). What was
missing was a written definition of "converged". This ADR fixes it —
arithmetic included, the same move as `docs/adr/README.md`'s
consolidation-trigger arithmetic — so no round re-litigates it.

## Decision

**Converged = K = 2 consecutive independent fresh passes yield zero new
findings that both (a) meet `sa-diff-review` §9's defect classifications
(a)–(d) and (b) survive adversarial verification (§10).**

Four terms carry the weight:

- **Defect-bar findings only.** Preference-tier output (`sa-audit-sweep`'s
  SHOULD DISCUSS / NITS) never advances or resets the counter.
  Adversarial verification refutes factual claims only, so a
  factually-accurate taste-level nit would always arrive as "new" and the
  criterion would re-import the unreachability it exists to fix.
- **One pass = the full battery the `sa-release-audit` skill defines** —
  the layer-scoped sweep runs that jointly cover CLAUDE.md's Layout
  table, plus the named panel and docs scopes — never any single review.
  Two `paths:`-scoped runs of one layer must not "converge" a codebase
  90% unexamined.
- **The K passes postdate the last triage-driven repository change**
  (fix or record). A pass that reviewed an older tree does not count as
  consecutive.
- **"New" = not matching any previously *triaged* finding-class** —
  including declined ones — at the class granularity the durable-closure
  convention already uses, never file:line. A pass that returns only
  already-decided points is a *successful, converged* pass.

## Consequences

- The criterion is reachable. Without the defect-bar restriction the
  preference supply is unbounded and K never completes; without the
  dedup-against-declines clause, rejected findings reappear every round
  and the loop cannot terminate.
- Declines must have an in-tree, grep-able record to bind fresh passes,
  whose executors have no GitHub access: design-level → an ADR (the
  #491 → ADR 0021 precedent), convention-level → a `.claude/rules/`
  clause, below both bars → the decline ledger
  (`.claude/rules/review-declines.md`, which also carries the
  precision-gated match-adjudication protocol). A `not_planned` issue
  closure remains tracking, never the binding record.
- Findings that flip between rounds are a signal about the rule, not the
  code: when two passes judge the same shape differently, the judgment
  criterion is unwritten, and the fix is a sentence in `.claude/rules/`
  or an ADR — not another code change.
- Each round's deliverable is not the fixes — it is the transfer of
  finding-classes out of "review catches it" into gates, rules, and
  records, until review only spends budget on genuinely undecided
  questions. This is ADR 0010's move — replace nondeterministic judgment
  with deterministic guards — applied to the review process itself.
