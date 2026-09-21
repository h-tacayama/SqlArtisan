---
description: Inline comment conventions — comment the why / why-not, never the what; keep it short
paths:
  - "src/**/*.cs"
  - "tests/**/*.cs"
---

# Code comments

Comment the **why** and the **why-not**, never the **what**. The *why* is the
non-obvious rationale behind the code; the *why-not* is a rejected alternative or
an avoided hazard the code itself can't show. Anything derivable from the code —
what a statement does, a method or field name, an exception message it throws —
is not a comment; it is noise. **Default to no comment** — add one only for a
non-obvious why / why-not (a design choice, a rejected alternative, a hazard
avoided, an ordering or timing that matters, an allocation note per ADR 0006, a
dialect quirk).

## Length defaults

- Inline `//`: **one line preferred, two max**.
- Doc/block prose (`///` summary, remarks, and each `<para>`, measured per
  part; header comments): **three lines max**.
  A `//` block immediately preceding a type or member declaration is a
  header and gets the three-line allowance; a `//` inside a body is inline.
- **At most one** example per comment; none if a nearby test shows it.
- **Never** enumerate three or more items (positions, callers, dialects, cases)
  the code or call sites already name.

A comment past these earns its length only by carrying a real *why* (hazard /
ordering / allocation); otherwise trim. That exception is judged one way
(release audit pass 7): the lines *past* the cap must themselves carry the
why — a comment whose why fits inside the cap is trimmed, however true the
rest reads. The caps date from `603c157f` (2026-07-09); a comment authored
after that commit is post-rule whatever its file's age, and none of the
grandfathering below reaches it.

**Comments predating the caps are grandfathered, per file.** They are pinned
in `tests/SqlArtisan.Tests/Baselines/comment-caps.txt`, trim when their file
is next edited for substance, and are not re-raised in review before then —
a bulk reflow would churn blame for no behavioral gain. A doc block split
into `<para>` parts is measured per part. `// Arrange` / `// Act` / `//
Assert` labels ride the same grace: the pre-rule ones stay, the cap ratchet
does not count them (one line each), and none is added — a label restates
the code (smell 1).

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

Audited at review time — the `sa-diff-review` skill runs this checklist over
the diff — and mechanically: `CommentCapRatchetTests` pins every over-cap block
per file (`tests/SqlArtisan.Tests/Baselines/comment-caps.txt`, the pre-rule
survivors grandfathered above), so a new or edited comment cannot add one. A
block that earns its length by a why past the cap is a deliberate baseline edit
in the same change, never a silent one (release audit pass 8).
