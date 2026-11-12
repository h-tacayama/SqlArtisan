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
- **A bool reads as a state at its call sites, never as an action or a value.** `Is` /
  `Has` / `Can`, a third-person verb (`ReadsPseudoRowTarget`), a participle or an
  adjective (`ConsumedAsSubquery`, `TargetRepeatedInFrom`, `Compatible`) all read as
  states. An imperative reads as an action (`QuoteName` became `IsNameQuoted`), and a
  noun phrase as the value returned (`RelistedDeleteLead` became `IsRelistedDeleteLead`).
  An option a caller hands in may stay the instruction, at the call that reads as one: a
  parameter (`quoteName: false`), a settings property mirroring a flag
  (`QualifySchema`), or the field that stores either. Avoid a verb whose stem names an
  action in scope: in the core, "bind" is parameter binding, so name capture is
  `ExposesTargetName`.
- **A side effect shows in the name.** A member that changes state is named for the
  change (`Mark…`, `Enable…`, `Set…`, `Swap…`, `Note…`). Never use a past participle
  that reads as a projection (`Composed` became `MarkComposed`). On `SqlBuildingBuffer`
  an imperative verb writes text, so a state setter says so
  (`EnableUpsertTargetQualification`).
- **`Try…` returns `bool` with an `out` and never throws for the failure it names.** A
  method that cannot return false is not `Try…`.
- **Guards.** `ThrowIf<failure>` names the failure or failures it throws for
  (`ThrowIfNullOrEmpty`), whether or not it hands the value back. An ADR 0012
  value-domain guard is `Validate…` in a `*Guard` class (`LockWaitGuard.ValidateSeconds`).
  TableClassGen's private guards are `Guard<Subject>` (`GuardClassNames`). `…OrThrow`
  sits only beside a sibling that lacks the throw.
- **One word per layer.** In the item resolvers (`ExpressionResolver`,
  `*ItemResolver`), `Resolve` turns a caller's item into a node. A clause's `Parse`
  builds the clause from those items. Text-to-value `Parse` lives in the
  analyzer and TableClassGen. In DML code, a bare "target" is the statement's target
  table and a "SET target" the column a `SET` assigns, so a column assigned twice is a
  duplicate *column*.
- **A name stays true when a caller is added.** When a second caller gives a member or
  parameter a wider meaning, rename it in the same change (`forUpdate` became
  `chainEnd`; `IsOrdinalType` became `IsIntegralType` once the ROUND check reused it).
