---
name: sa-diff-review-refinement
description: sa-diff-review over the same diff, plus a refinement pass that surfaces non-defect "better way to write this" findings for code and docs (idiom, factoring, style). It widens the finding *types*, not the scope — the files reviewed are identical to sa-diff-review's, so it is not a wider audit; for a corpus-wide docs sweep use sa-docs-audit. Use when the user explicitly asks for a deep/thorough review, or for improvement/idiom/style suggestions on top of the usual defect check. Do not use for a routine pre-push check — use sa-diff-review for that; it stays quieter by design. Accepts the same scope argument as sa-diff-review (default: the diff's hunks only; `files`; `paths:<glob>`).
---

# Review a diff, with refinements

This is `sa-diff-review` plus one more pass. Run `sa-diff-review`'s full
procedure first, unchanged, with whatever scope argument you were given —
every gate, every ADR check, the empirical harness, and its defect-only
Report. Everything below is **additive**: a second, non-blocking pass that
only this skill runs.

## Refinement axis

A finding here is a **better way to write something that is not wrong** — if
it were wrong, it belongs in `sa-diff-review`'s defect report instead, not
here.

- **Code** — a helper you'd have named or factored differently; a duplication
  with a cleaner solution that is not (yet) an established in-repo pattern (if
  it *is* an established pattern, that's a defect — see `sa-diff-review` §9,
  not this section); a simplification that no ADR/rule requires.
- **Docs** — an example that still runs and makes no false recommendation
  claim, but no longer uses the current idiom once a simpler API covers the
  same case (name that API). This repo's docs are read by AI coding assistants
  as much as humans (ADR 0010, `llms.txt`, `docs/guides/ai-assistants.md`), so
  a stale idiom can be reproduced verbatim in generated code — worth
  surfacing here even though it is not a defect.

A refinement must still be **concrete** (name the alternative) and must not
contradict an ADR — you cannot pitch as an "improvement" something ADR 0001 or
`guards-and-empty-states.md` deliberately rejects. Beyond that, general idiom
and taste are legitimately in scope here, *because reaching this skill was a
deliberate choice* — the discipline is that choice, not a citation
requirement.

## Adversarial pass (inherited)

The adversarial verification pass (`sa-diff-review` §10, not skippable) is
inherited and runs **once**, after this refinement pass, covering both
reports. Refinements themselves are opinions, not refutation targets — but
any factual claim inside one (e.g. "API X already covers this case") gets
the primary-source check before it is reported.

## Report

Append a **Refinements (optional, non-blocking)** zone after
`sa-diff-review`'s must-fix findings, each tagged `file:line`. Never mix a
refinement into the must-fix zone, and never let one block the mergeable
verdict.

A refinement is still something you are proposing the author *do* — a change
they could apply. An observation with no change attached ("worth knowing",
"already fine") is not a refinement and belongs nowhere in the report. **An
empty refinements zone is a normal result**: reaching for this variant does
not oblige you to find something, and inventing one to fill the zone costs the
author more attention than it returns.
