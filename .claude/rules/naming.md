---
description: Identifier names — a name must not invite a misreading of what the code does
paths:
  - "src/**/*.cs"
---

# Identifier names

A name states the responsibility the code has, all of it, so a reader who has not
opened the body does not take it for another. Public names after a SQL token, builder
stages and the contract names are `public-api-design.md`'s; this file covers the rest.
It is a test for a misreading, applied to a name a change adds or widens. A name that
reads correctly where it is used is not renamed to fit a pattern, and the spellings
below are what the audit found, not a mandate over existing code. It distils the
2026-11 naming audit, which started from `IsListed` (now `IsDirectlyBindable`):
"listed" named no list a reader could find.

The misreadings the audit fixed:

- **Another scope or a hidden condition.** The name claims another scope than the code
  covers (`ResolveIntervalOperand` resolves the right operand of every `+` / `-`, so it
  became `ResolveAdditiveOperand`), or hides the condition it applies
  (`ThrowIfJoinedDeleteTargetNotRepeated` became
  `ThrowIfDeleteTargetNotRepeatedInFromOffOracle`). Two members with different scopes
  are not spelled as synonyms (`ThrowIfFrozen` and `ThrowIfNotBuildable`).
- **A bool read as an action or a value.** `QuoteName` read as an action and became
  `IsNameQuoted`. `RelistedDeleteLead` read as the value returned and became
  `IsRelistedDeleteLead`. Two kinds of bool read correctly as they are: one that reads
  a stored value (`ReadIsNullable`), and an option a caller hands in
  (`quoteName: false`).
- **A hidden side effect.** `Composed` read as a projection although it freezes the
  caller's builder, so it became `MarkComposed`. `SqlBuildingBuffer`'s `Append…` and
  `Enclose…` write text, so `QualifyUpsertTarget`, which only sets a flag, became
  `EnableUpsertTargetQualification`.
- **A `Try…` that cannot report failure.** `TryDeriveColumnNames` threw for the failure
  it named and became `DeriveColumnNames`. A `Try…` returns `bool` with an `out` and
  can return false.
- **A word a neighbour uses for something else.**
  - In the core, "bind" is parameter binding, so name capture is `ExposesTargetName`.
  - In DML code, "target" alone is the statement's table, so a column assigned twice
    is caught by `ThrowIfDuplicateColumn`.
  - `…OrThrow` beside a sibling that throws too hid the real difference
    (`ResolveOrThrow` became `ResolveNonEmpty`).
- **A name left behind when a caller widened it.** `forUpdate` became `chainEnd`, and
  `IsOrdinalType` became `IsIntegralType` once the ROUND check reused it.

Guards are spelled three ways: `ThrowIf<failure>`, `Validate…` for the ADR 0012
value-domain guards in a `*Guard` class, and TableClassGen's `Guard<Subject>`. A new
guard takes its neighbours' spelling, and no guard is renamed into another's.
