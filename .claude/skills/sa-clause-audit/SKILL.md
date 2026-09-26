---
name: sa-clause-audit
description: Audit one bounded unit of the SqlArtisan codebase clause by clause — one reviewer per rule clause, each enumerating every member the clause applies to and judging each — then adjudicate against primary sources and land every finding in an issue. The pre-1.0 review runs this unit by unit from the tracking issue (#558). Use when asked to review a unit of the #558 plan, or to audit a bounded scope against its rules with coverage rather than sampling. Not for a branch diff (sa-diff-review) or an open-ended multi-model audit (sa-panel-audit).
---

# Clause audit of one unit

An open-ended review samples "anything worth mentioning": independent passes
return different sets, never converge, and outrun what a human can adjudicate
(#522 retired such an audit). Asking **one narrow question per rule clause**
and making the reviewer **enumerate every member it applies to** turns
sampling into coverage. On the pilot unit it recovered 3 of 4 known findings
where an open pass found 1–2 (#558 has the measurements). Its blind spot is a
defect class no clause names — which is why step 4 turns each such class
into a clause.

The unit is **done when reviewed and every finding has landed**, never when a
pass returns zero. Fixes ship separately and do not reopen the unit.

## 1. Fix the unit and pick the clauses

Take the unit from #558 (or the request) and resolve its globs to a file list.
Then list the clauses:

- every `##` section of each `.claude/rules/` file whose `paths:` frontmatter
  matches the unit, plus the ADRs those sections cite;
- **one open-ended pass** — Phase A: "what here could not change after 1.0
  without a major version?"; Phase B: "where does emitted SQL or a guard
  deviate from ADR 0001/0007?"

Skip a section that constrains nothing the unit contains, and say so. Tag each
clause **H** (dialect grammar, analyzer design, the open pass — judgment
depends on engine or Roslyn facts) or **M** (naming, types, arity, formatting
— the enumeration does the work).

## 2. Run one reviewer per clause

One `general-purpose` agent per clause, `model: opus`, in parallel, each with
the prompt below. A subagent runs at **this session's** effort — the Agent
tool takes no effort argument — so run the H batch under `/effort high` and
the M batch under `/effort medium`, or everything at high: measured at about
+20% tokens and 1.5× wall time, and in exchange one medium pass stated a false
premise ("no member has an optional parameter") that hid a real finding.

```text
You are reviewing ONE bounded unit of the SqlArtisan repository against ONE
rule clause. Never modify files inside the repository; any throwaway harness
goes under {SCRATCH}.

## Unit
{UNIT_GLOBS} — {UNIT_DESCRIPTION}. You may read anything else in the repo to
judge it; findings must be about this unit.

## The clause
{CLAUSE_LOCATION}: {CLAUSE_SUMMARY}
Applies to: {WHAT_TO_ENUMERATE_AND_HOW_TO_JUDGE_IT}

## Method: exhaustive enumeration, not sampling
1. Read the clause in full, and the ADRs/rules it cites.
2. Enumerate EVERY member of the unit the clause applies to — build the
   candidate list mechanically with grep/scripts, then read each one.
3. Verify every factual claim about the unit ("no member has X") with a
   command before relying on it — a wrong premise silently shrinks the list.
4. Verdict per member: CONFORMS / VIOLATES / RECORDED EXCEPTION. An exception
   must cite the file:line of its record, and the record's stated *reasoning*
   must hold for this member — a record that names the category but whose
   reason does not apply is NOT an exception.

## Output
- The enumeration table (member, file:line, verdict, one-line reason).
- Findings = the VIOLATES rows, at most five, most severe first: location,
  one-sentence defect, evidence checkable in two minutes (file:line of member,
  clause and siblings; harness or engine output for claims about emitted SQL
  or engine behavior), whether fixing it after 1.0 is breaking and why,
  confidence (high/medium).
- Before calling a fix breaking, weigh every non-breaking remedy — an
  analyzer rule (Roslyn sees a call's expanded `params` count, whether an
  optional argument was supplied, and constant values), an added overload, a
  record in docs/rules/ADR — and say why each fails. If one works, the
  finding is non-breaking.
- Engine claims you cannot run: cite the source and say "not live-verified".
- Zero findings is a valid result. Do not pad.
- End with the candidate-list command, the candidate count, and whether you
  ran a harness.
```

## 3. Adjudicate against primary sources

The reviewers' reports are drafts, not verdicts. For every finding:

- Re-read the cited lines and re-run the decisive check yourself (a harness,
  `python3 -c 'import sqlite3 …'` for SQLite; this container has no database
  daemon, so other engines go into the landing issue as a verification item).
- Where reviewers disagree that something is "recorded", open the record: a
  matching word is not a matching reason (four passes dropped #555 on ADR
  0016's wording; its stated reason did not apply to that function).
- Reject a "breaking" claim when an analyzer, overload or record remedy
  exists.
- Drop what is non-breaking **and** already recorded; keep everything else.

## 4. Land every finding

- **Breaking** (a public signature, namespace or emitted SQL must change) →
  its own issue, targeted before 1.0.
- **Non-breaking** (record corrections, analyzer gaps, doc fixes) → one issue
  for the unit, as a checklist, linking the unit's breaking issues.
- **A finding no clause covered** → also a new clause in the rules file that
  owns its subject (#557 → the property-vs-method clause), so the next unit's
  clause pass can see the class.

## 5. Tick the unit

Check the unit off in #558 with links to the issues it produced, and note
anything the method itself taught (a prompt fix, a clause that needs
splitting) in the unit issue.
