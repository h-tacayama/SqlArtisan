---
description: Identifier names — state the whole responsibility; predicate, side-effect, Try and guard shapes
paths:
  - "src/**/*.cs"
---

# Identifier names

A name states the responsibility the code has, all of it, so a reader who has not
opened the body does not take it for another. Public names after a SQL token, builder
stages and the contract names are `public-api-design.md`'s; this file covers every
other type, member, parameter and local. It distils the 2026-11 naming audit, which
started from `IsListed` (now `IsDirectlyBindable`): "listed" named no list a reader
could find.

- **The whole scope, no hidden condition.** A name that claims more than the code
  covers, or hides the condition it applies, is wrong. A guard scoped to a dialect or
  a shape names it (`ThrowIfDeleteTargetNotRepeatedInFromOffOracle`,
  `ThrowIfDeleteCombinedWithRepeatedTarget`), and two guards with different scopes are
  not spelled as synonyms (`ThrowIfFrozen` vs `ThrowIfNotBuildable`).
- **A bool reads as a question.** Use `Is` / `Has` / `Can`, or a third-person verb
  phrase (`ConsumedAsSubquery`, `ReadsPseudoRowTarget`). Never use an imperative
  (`QuoteName` became `IsNameQuoted`) or a bare noun phrase (`RelistedDeleteLead`). Avoid
  a verb whose stem already names an action in scope: in the core, "bind" is parameter
  binding, so name capture is `ExposesTargetName`.
- **A side effect shows in the name.** A member that changes state is named for the
  change (`Mark…`, `Enable…`, `Set…`, `Swap…`, `Note…`). Never use a past participle
  that reads as a projection (`Composed` became `MarkComposed`). On `SqlBuildingBuffer`
  an imperative verb writes text, so a state setter says so
  (`EnableUpsertTargetQualification`).
- **`Try…` returns `bool` with an `out` and never throws for the failure it names.** A
  method that cannot return false is not `Try…`.
- **Guards follow three spellings.**
  - `ThrowIf<failure>` names one failure.
  - `Validate<Subject>` in a `*Guard` class is the ADR 0012 value-domain guard. Several
    check more than one condition and hand the value back, which a single
    `ThrowIf<failure>` could not name.
  - `…OrThrow` sits only beside a sibling that lacks the throw.
- **One word per layer.** `Resolve` turns a caller's item into a node. A clause's
  `Parse` builds the clause from the caller's items. Text-to-value `Parse` lives in the
  analyzer and TableClassGen. In DML code, "target" is the statement's target table, so
  a column assigned twice is a duplicate *column*.
- **A name stays true when a caller is added.** When a second caller gives a member or
  parameter a wider meaning, rename it in the same change (`forUpdate` became
  `chainEnd`; `IsOrdinalType` became `IsIntegralType` once the ROUND check reused it).
