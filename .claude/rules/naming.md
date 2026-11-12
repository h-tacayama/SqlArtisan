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
- **A bool that reports a state reads as a question.** Use `Is` / `Has` / `Can`, or a
  third-person verb phrase (`ConsumedAsSubquery`, `ReadsPseudoRowTarget`). Never use an
  imperative (`QuoteName` became `IsNameQuoted`) or a bare noun phrase
  (`RelistedDeleteLead`). An option a caller hands in may stay the instruction, at the
  call that reads as one: a parameter (`quoteName: false`), a settings property
  mirroring a flag (`QualifySchema`), or the field that stores either. Avoid a verb whose stem already names an action in
  scope: in the core, "bind" is parameter binding, so name capture is
  `ExposesTargetName`.
- **A side effect shows in the name.** A member that changes state is named for the
  change (`Mark…`, `Enable…`, `Set…`, `Swap…`, `Note…`). Never use a past participle
  that reads as a projection (`Composed` became `MarkComposed`). On `SqlBuildingBuffer`
  an imperative verb writes text, so a state setter says so
  (`EnableUpsertTargetQualification`).
- **`Try…` returns `bool` with an `out` and never throws for the failure it names.** A
  method that cannot return false is not `Try…`.
- **Guards follow four spellings.**
  - `ThrowIf<failure>` names one failure.
  - `Validate…` in a `*Guard` class is the ADR 0012 value-domain guard, named
    for the value it checks: some check more than one condition and some hand the
    value back, which a single `ThrowIf<failure>` could not say.
  - TableClassGen's `Guard<Subject>` names the subject checked against several
    conditions (`GuardClassNames`).
  - `…OrThrow` sits only beside a sibling that lacks the throw.
- **One word per layer.** In the item resolvers (`ExpressionResolver`,
  `*ItemResolver`), `Resolve` turns a caller's item into a node. A clause's `Parse`
  builds the clause from those items. Text-to-value `Parse` lives in the
  analyzer and TableClassGen. In DML code, "target" is the statement's target table, so
  a column assigned twice is a duplicate *column*.
- **A name stays true when a caller is added.** When a second caller gives a member or
  parameter a wider meaning, rename it in the same change (`forUpdate` became
  `chainEnd`; `IsOrdinalType` became `IsIntegralType` once the ROUND check reused it).
