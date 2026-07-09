---
description: Inline comment conventions — comment the why, never the what; keep it short
paths:
  - "src/**/*.cs"
  - "tests/**/*.cs"
---

# Code comments

Comment the **why**, never the **what**. The code already states what it does; a
comment that paraphrases the adjacent statement, a method or field name, or an
exception message it throws is noise — delete it.

- **Default to no comment.** Add one only for a non-obvious rationale: a design
  choice, a hazard avoided, an ordering or timing that matters, an allocation
  note (ADR 0006), a dialect quirk.
- **Shortest form that carries the why.** One line is the target; two is a lot;
  more needs a real reason. Trim to a clause before adding a second sentence.
- **Don't echo the guard or its message.** A throw's message is self-documenting;
  a comment near it earns its place only by adding why (e.g. why guard *here*, not
  elsewhere), never by restating it.
- **No enumerations the code or call sites already show** — a list of every
  position, caller, or dialect a helper serves. State the rule once; the code is
  the index.

Audited at review time: the `sa-review-changes` skill flags diff comments that
break this and trims them.
