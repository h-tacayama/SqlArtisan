---
name: sa-panel-diff-review
description: High-confidence review of a SqlArtisan branch diff by an independent three-model panel (Sonnet, Opus, Fable), adjudicated by the main agent. Same scope as sa-diff-review — the branch-point diff — but reviewed three times over by models that cannot see each other's work or yours; the main agent then re-derives every finding against primary sources and issues the verdict. Each reviewer runs fresh with no knowledge of the fix history, so no one is anchored by how the change came to be. Use before merging something high-stakes, when the implementing session went through several fix rounds, or when explicitly asked for a multi-model / independent / panel review. Costs roughly three full reviews — do NOT use as the routine pre-push check (`sa-diff-review`) or for idiom suggestions (`sa-diff-review-suggest`). For the docs corpus instead of a diff, use `sa-panel-docs-audit`.
---

# Panel review of a branch diff

`sa-diff-review` runs **one** reviewer and buys confidence through *depth*.
This skill buys it through *independence and model diversity*: the same diff,
reviewed three times by three different models that cannot see each other's
work or yours, then adjudicated here.

The axis is **confidence in the verdict**, not breadth of finding types — for
"better way to write this" suggestions, run `sa-diff-review-suggest` instead;
for the docs corpus rather than a diff, `sa-panel-docs-audit`.

## What this supersedes

`sa-diff-review` §10 says to spawn exactly one adversarial subagent and warns
against spawning more "just to be sure". **This skill deliberately replaces
that pass**, and does not violate its reasoning: §10 argues against *redundant
self-checking by the same model*, which a strong model already does
internally. Three *different* models are not that — they fail differently, and
the payoff is the finding one model's blind spot hides.

The evidence is on the record. Reviewing PR #427, Opus alone caught a docs
example still referencing a variable the change had deleted — a
compile-breaking defect that the primary pass, Sonnet, and Fable all missed.
On #428 no finding was unanimous either: the stale `docs/expressions.md`
restriction was raised by Opus and Fable but not Sonnet. Both changes shipped
with a defect that a single-seat review had an even chance of missing.

Each panelist still runs §10 *within its own review* (`sa-reviewer` has no
Agent tool, so it self-verifies as that section's fallback). Every report
reaching you is therefore already self-refuted once.

## 1. Fix the scope

The rubric is `sa-diff-review`, and the scope is what that skill would review:
the **branch-point** diff, never a stale-`main` diff.

```bash
git fetch origin main
git merge-base origin/main HEAD      # the base; never trust local main
git diff <base>..HEAD --stat
```

A scope argument (`files`, `paths:<glob>`) widens it exactly as it does there.

## 2. Run the deterministic checks once, up front

Gates and scripts are mechanical — three panelists re-deriving identical
output wastes two thirds of that cost for no independence gain (judgment is
what must stay independent, not command output). Run them yourself:

```bash
dotnet build src/SqlArtisan/SqlArtisan.csproj -c Release
dotnet test tests/SqlArtisan.Tests
dotnet format SqlArtisan.sln --verify-no-changes
```

Pass the verbatim results into the briefing and tell panelists not to re-run
them — but say they *may* if they doubt a result, so a misread gate can still
be caught rather than inherited three times over.

A failing gate is itself a must-fix: fix it before convening the panel rather
than spending three deep reviews on code the toolchain already rejects.

## 3. Brief the panel — what to withhold is the whole point

The panel is worth its cost only if the three reviews are genuinely
independent. That independence is destroyed by *your* framing, not by the
repository's own contents. Split the two:

**Include — these are claims under test, not contamination.** The diff range,
the originating issue, the PR body and commit messages, the rubric to follow,
the defect bar, and the gate/script output from §2. A panelist is *supposed*
to try to refute what the commit message and PR body assert.

**Withhold — every trace of how the change came to be.** No earlier review
findings, yours or another panelist's. No list of what was already fixed, how
many rounds it took, or which concerns are settled. No draft verdict. Nothing
about what you suspect or are unsure of.

That last exclusion is stricter than `sa-diff-review` §10, which permits
naming the claims you are unsure of to a single subagent. Here it is
forbidden: a hint given to all three lands identically in all three and buys
you three correlated opinions at triple the price. Withholding it is the
product you are paying for.

Write **one** prompt and send it to all three unchanged — only `model` may
differ. Otherwise a difference between reports cannot be attributed to the
model rather than to your wording.

## 4. Dispatch

Launch all three in a **single message** so they run concurrently, each with
`subagent_type: "sa-reviewer"` and `model:` set to `sonnet`, `opus`, and
`fable` respectively. (Haiku is not a panelist — it is not strong enough for a
deep review, and a weak seat costs adjudication effort without adding
coverage.)

If a panelist errors or never returns, the panel is smaller than it looks. Say
so in the report — "2-of-3 panel, Fable did not return" — and never present a
short panel as a full one. Re-dispatching the missing seat once is reasonable;
proceeding silently is not.

## 5. Adjudicate — this stage is yours and cannot be delegated

You weigh the reports. Do not spawn an agent to synthesize them: the
adjudication *is* the skill, and handing it to a fourth model reintroduces
exactly the single-point-of-judgment the panel exists to remove.

**Re-derive every finding against primary sources before acting on it** — the
code, a test catalog, an ADR, or a live harness probe. A panelist's report is
evidence, not a verdict; agents report plausible-but-wrong results, and a
confidently-worded finding is not thereby correct.

**Never majority-vote.** A finding raised by one panelist gets exactly the same
verification as one raised by all three. #428 produced two unanimous findings
and one raised by two seats; #427's real defect had a single voice — a 2-of-3
rule would have thrown that one away. Convergence is a **prioritization**
signal (verify those first; they are likely real), never a **truth** signal.

A panelist reporting **no findings** is data about that panelist's coverage,
not a vote against another's finding. Three clean reports are meaningful; two
clean reports do not refute the third's defect.

Classify each finding, and keep the classification visible in the report:

- **CONFIRMED** — survived your re-derivation; cite the primary source or
  probe output that settles it.
- **REFUTED** — you disproved it; cite the disproving evidence.
- **OVERREACH** — technically true but stated too broadly (a quantifier with a
  real exception); narrow it rather than dropping it.
- **OUT OF SCOPE** — real, but not this change's doing. Say where it belongs.

Fix what is CONFIRMED. Then, before merging, run one final `sa-reviewer` pass
over the *fixed* state — the panel reviewed the code as it was, and your fixes
are themselves unreviewed changes.

## 6. Report

Report only findings you are asking someone to change (`sa-diff-review` §9's
bar applies unchanged). Beyond the usual verdict and severity ordering, the
panel adds two obligations:

- **Attribute every finding** to the panelist(s) who raised it, and give its
  classification. The reader is paying for three opinions; show them the
  disagreement rather than a laundered consensus.
- **State the panel's actual shape** — which models ran, which returned, what
  each verified empirically. A conclusion drawn from a 2-of-3 panel with one
  unprobed dialect is a weaker conclusion, and the reader cannot discount it
  if you do not say so.

Lead with the verdict, then findings by severity, then the panel record. Zero
confirmed findings is a good and complete result — three independent models
finding nothing is the strongest signal this skill can produce, and padding it
with non-actionable observations destroys exactly that value.
