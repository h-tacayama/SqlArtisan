---
description: SQL emission style — one-token keyword atoms, const-interpolated phrases, buffer spacing helpers
paths:
  - "src/SqlArtisan/Internal/**/*.cs"
---

# SQL building style (keywords & spacing)

Two layers, chosen so neither requires a judgment call (#207 / #208):

1. **Keyword atoms.** Every constant in `Keywords.cs` is exactly **one SQL
   lexical token** — no spaces (`ADD_MONTHS` counts as one token). A fixed
   multi-word phrase is composed at the use site by **const interpolation of
   atoms** — `$"{Keywords.Group} {Keywords.By}"` — which the compiler folds
   into a single string constant (zero runtime cost). Embedded spaces are
   allowed **only** between keyword atoms inside such const interpolations,
   and the interpolation contains only the phrase — no leading or trailing
   spaces. (Four legacy compound constants are being decomposed in #208.)

2. **Runtime spacing** goes through the `SqlBuildingBuffer` helpers, never
   through spaces embedded in strings:
   - keyword/operator between two operands → `.EncloseInSpaces(Keywords.X)`
     (or `.EncloseInSpaces($"{Keywords.Not} {Keywords.In}")` for a phrase)
   - keyword/phrase followed by a part → `.Append(phrase).AppendSpace()`
   - fixed trailing token → `.AppendSpace().Append(phrase)`
   - optional trailing part → `.PrependSpaceIfNotNull(part)`; for a
     flag-gated token, `.PrependSpaceIfNotNull(flag ? Keywords.X : null)`

Never build SQL text with a **runtime** interpolation inside `Format` —
`$"{Keywords.Wait} {_seconds}"` allocates a string on every build (ADR 0006).
Stringify values at construction (`seconds.ToInvariantString()`) and append
the pieces, or hoist the whole text to construction time.
