---
description: Inline comment conventions — comment the why, never the what; keep it short
paths:
  - "src/**/*.cs"
  - "tests/**/*.cs"
---

# Code comments

Comment the **why**, never the **what**. The code already states what it does; a
comment that paraphrases the adjacent statement, a method or field name, or an
exception message it throws is noise. **Default to no comment** — add one only
for a non-obvious rationale (a design choice, a hazard avoided, an ordering or
timing that matters, an allocation note per ADR 0006, a dialect quirk).

## Length defaults

- Inline `//`: **one line preferred, two max**.
- Doc/block prose (`///` remarks, header comments): **three lines max**.
- **At most one** example per comment; none if a nearby test shows it.
- **Never** enumerate three or more items (positions, callers, dialects, cases)
  the code or call sites already name.

A comment past these earns its length only by carrying a real *why* (hazard /
ordering / allocation); otherwise trim.

## Comment smells — audit every comment against these

| # | Smell | How to spot it | Fix |
|---|---|---|---|
| 1 | Restates the code | words mirror the next statement, a method/field name, or a throw message | delete |
| 2 | Echoes the message | paraphrases an adjacent throw/log string | delete (keep only to add *why here*) |
| 3 | Over-enumeration | lists ≥3 items the code / call sites already name | state the rule once; drop the list |
| 4 | Excess examples | ≥2 `e.g.` / inline examples, or one a test already shows | keep ≤1 |
| 5 | Filler phrasing | has "Note that", "In order to", "It's important to", "Basically", "simply", "just", "obviously", "This is done so that" | rewrite without it (usually a clause shorter) |
| 6 | Over-length | exceeds the length defaults with no real why | trim to the why; split unclear code instead of narrating it |
| 7 | Duplicated rationale | the same why on ≥2 sibling members / clauses | state once (shared type or first site); reference, don't repeat |
| 8 | Re-documents policy | restates something already in a rule / ADR / CHANGELOG | short pointer, or nothing |

Smells 5 and 6 are near-mechanical (a greppable phrase list, a line count);
1 / 2 / 7 / 8 need the *is-the-why-non-obvious* judgment.

Audited at review time — the `sa-code-review` skill runs this checklist over
the diff.
