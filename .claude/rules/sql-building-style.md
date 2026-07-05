---
description: SQL emission style — one-token keyword atoms, const-interpolated phrases, buffer spacing helpers
paths:
  - "src/SqlArtisan/Internal/**/*.cs"
---

# SQL building style (keywords & spacing)

Rules for `Format` implementations and `Keywords.cs` (#207 / #208):

1. **Keyword atoms.** Every constant in `Keywords.cs` is exactly **one SQL
   lexical token** — no spaces (`ADD_MONTHS` counts as one token). A fixed
   multi-word phrase is composed at the use site by **const interpolation of
   atoms** — `$"{Keywords.Group} {Keywords.By}"` — which the compiler folds
   into a single string constant (zero runtime cost). Edge spaces inside such
   const interpolations are fine: `$"{Keywords.Where} "`, `$" {Keywords.End}"`.
   The one-token rule is CI-enforced by `KeywordsTests` (a constant value
   containing a space fails the unit suite).

2. **Helpers cover what a const interpolation cannot.** Use them for spacing
   that involves a `SqlPart`, a runtime value, or a condition — do not
   decompose an existing const interpolation just to move its space into a
   helper call:
   - keyword/operator between two operand parts → `.EncloseInSpaces(Keywords.X)`
     (or `.EncloseInSpaces($"{Keywords.Not} {Keywords.In}")` for a phrase)
   - optional token/part → `.PrependSpaceIfNotNull(part)`; for a flag-gated
     token, `.PrependSpaceIfNotNull(flag ? Keywords.X : null)`
   - a space adjacent to a runtime token where no const string exists →
     `.AppendSpace()` / `.AppendSpace(part)`

3. **Never build SQL text with a runtime interpolation inside `Format`** —
   `$"{Keywords.Wait} {_seconds}"` allocates a string on every build
   (ADR 0006). Stringify values at construction
   (`seconds.ToInvariantString()`) and append the pieces.

4. **Chain wrapping.** An expression-bodied `Format` (`=> buffer`) wraps its
   chain one method per line — the dominant shape across the condition and
   clause nodes — even when it has only two calls. A statement chain inside a
   block body stays on one line while it is short
   (`buffer.Append(_offset).AppendSpace();`); once it wraps, one method per
   line applies there too.

5. **Builder ordering.** Order a statement builder's **implemented-interface
   list** and its **member methods** alphabetically by name (e.g.
   `DeleteBuilder`, `UpdateBuilder`). Properties precede methods and the
   `protected` build hook trails; overloads stay adjacent and explicit
   interface implementations sort by their simple name. Within an interface
   definition, declare members alphabetically too. This is mechanical and
   keeps builders consistent as they grow.
