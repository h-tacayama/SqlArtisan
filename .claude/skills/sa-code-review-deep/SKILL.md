---
name: sa-code-review-deep
description: Thorough variant of sa-code-review — runs its full defect review, then additionally surfaces non-defect "better way to write this" suggestions for code and docs (idiom, factoring, style). Use when the user explicitly asks for a deep/thorough review, or for improvement/idiom/style suggestions on top of the usual defect check. Do not use for a routine pre-push check — use sa-code-review for that; it stays quieter by design. Accepts the same scope argument as sa-code-review (default: the diff's hunks only; `files`; `paths:<glob>`).
---

# Deep-review SqlArtisan changes

This is `sa-code-review` plus one more pass. Run `sa-code-review`'s full
procedure first, unchanged, with whatever scope argument you were given —
every gate, every ADR check, the empirical harness, and its defect-only
Report. Everything below is **additive**: a second, non-blocking pass that
only this skill runs.

## Improvement axis

A finding here is a **better way to write something that is not wrong** — if
it were wrong, it belongs in `sa-code-review`'s defect report instead, not
here.

- **Code** — a helper you'd have named or factored differently; a duplication
  with a cleaner solution that is not (yet) an established in-repo pattern (if
  it *is* an established pattern, that's a defect — see `sa-code-review` §8,
  not this section); a simplification that no ADR/rule requires.
- **Docs** — an example that still runs and makes no false recommendation
  claim, but no longer uses the current idiom once a simpler API covers the
  same case (name that API). This repo's docs are read by AI coding assistants
  as much as humans (ADR 0010, `llms.txt`, `docs/guides/ai-assistants.md`), so
  a stale idiom can be reproduced verbatim in generated code — worth
  surfacing here even though it is not a defect.

A suggestion must still be **concrete** (name the alternative) and must not
contradict an ADR — you cannot pitch as an "improvement" something ADR 0001 or
`guards-and-empty-states.md` deliberately rejects. Beyond that, general idiom
and taste are legitimately in scope here, *because reaching this skill was a
deliberate choice* — the discipline is that choice, not a citation
requirement.

## Adversarial pass (inherited)

The mandatory adversarial verification pass (`sa-code-review` §9) is
inherited and runs **once**, after this improvement pass, covering both
reports. Suggestions themselves are opinions, not refutation targets — but
any factual claim inside one (e.g. "API X already covers this case") gets
the primary-source check before it is reported.

## Report

Append a **Suggestions (optional, non-blocking)** zone after
`sa-code-review`'s must-fix findings, each tagged `file:line`. Never mix a
suggestion into the must-fix zone, and never let one block the mergeable
verdict.
