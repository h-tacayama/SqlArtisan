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

*(No entries yet — the first below-bar decline from a release-audit triage
lands here.)*
