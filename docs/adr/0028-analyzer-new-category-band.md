# ADR 0028 — A new analyzer category: added with its own band in a minor release

**Status:** Accepted

## Context

ADR 0018 gave each of the four categories a band of 100 IDs, and ADR 0026
covered "the four category strings and their bands" after 1.0. Neither says
whether a fifth category may be added later, or in what release. #738 asks
whether a rule there would want a `SqlArtisan.Security` band, and records that
`docs/versioning.md` leaves the release class of a new band unstated, so the
question had to be settled before 1.0 even while the rule itself stays
deferred.

## Decision

**A new category arrives with the next unused band of 100 IDs and its first
rule, in a minor release, like any other new rule. From that release its
category string and band are covered as the four are.**

Adding one changes nothing ADR 0026 covers:

- The four category strings, their bands and the rules in them stay as they
  were, so an ID's partition is unchanged.
- A `dotnet_analyzer_diagnostic.category-<category>.severity` line written for
  an existing category reaches the same rules as before.
- What the new category's first rule adds is a new report, which ADR 0026
  already classes as minor wherever the rule sits; a line that sets every
  analyzer diagnostic's severity treats it the same in either place.

The band follows ADR 0018's arithmetic: the first unused hundred after
`SQLA0399`, its category decided by the band. Covering it from the release
that adds it keeps ADR 0026's guarantee for a user who writes its name into a
build.

This decides when a category may be added, not whether any rule earns one.
ADR 0018 justified a band by the shape of the rules it holds, and ADR 0023
declined a band for two rules; a new category still has to meet that.

## Consequences

- `docs/versioning.md` and `.claude/rules/public-api-design.md` state both
  halves: a new category with its band is minor, and covered from its release.
- #738 can decide between `SqlArtisan.Security` and `SqlArtisan.Validity` when
  it starts, after 1.0 as well as before.
- `DiagnosticOrderingTests` gains the new band's case with its first rule.

Related: #738 (the question), ADR 0018 (the bands), ADR 0023 (a band declined
for two rules), ADR 0026 (the contract).
