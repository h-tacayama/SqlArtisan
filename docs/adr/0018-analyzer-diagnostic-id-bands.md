# ADR 0018 — Analyzer diagnostic ID bands: one numbered range per category

**Status:** Accepted

## Context

The diagnostics were numbered in a single flat sequence. ADR 0013 established
the ordering principle and #264 applied it as a breaking renumber: configuration
first, then whether a construct exists at all (on the dialect, then at the
declared version), then how it is used, then a property of what is named.

That ordering was right. What it did not give each family was *room*, and the
scheme has been failing on that ever since:

- The dialect range `0001`–`0006` filled up. `DiagnosticDescriptors` said so in
  a comment, and resolved it by ruling that a seventh dialect rule would take
  the next free number — which ends the family-order property #264 established.
- One rule had already been moved mid-flight for the same reason: the
  identifier-length rule was `0003` before becoming `0006` (#326/#349).
- `DiagnosticOrderingTests` encoded the expiry date. Its category assertion read
  `id <= 6 ? Dialect : Schema`, and its own comment instructed a future reader to
  *"expect to delete this test rather than satisfy it."* A gate written to be
  deleted stops being a gate.
- `SQLA0001` validates `.editorconfig` values and reads no dialect, but sat in
  `SqlArtisan.Dialect` because that was the only category when it shipped.

#432 forced the question. It adds a *configuration* diagnostic; under the flat
sequence that lands at `SQLA0013` — eleven numbers from `SQLA0001`, the only
other configuration diagnostic, in a category that describes neither.

The window for fixing this is closing. `docs/versioning.md` allows breaking
changes in any 0.x release and none from 1.0; the current version is
`0.8.0-beta.1`. After 1.0 the answer is "never".

## Decision

**Four numbered bands of 100, one per category. The band an ID falls in decides
its category, and `DiagnosticOrderingTests` gates the pairing.**

| Band | Category | Answers |
|---|---|---|
| `SQLA0001`–`0099` | `SqlArtisan.Configuration` | is the analyzer itself configured correctly? |
| `SQLA0100`–`0199` | `SqlArtisan.Dialect` | will this run on the engine you configured? |
| `SQLA0200`–`0299` | `SqlArtisan.Schema` | does it agree with what the table classes say? |
| `SQLA0300`–`0399` | `SqlArtisan.Validity` | is this a statement `Build()` would reject? |

Old → new, behavior unchanged in every case (full mapping, with what each rule
reports, in `CHANGELOG.md`):

`0001`→`0001`, `0002`→`0100`, `0003`→`0101`, `0004`→`0102`, `0006`→`0103`,
`0007`→`0200`, `0008`→`0201`, `0009`→`0202`, `0010`→`0203`, `0011`→`0204`,
`0012`→`0205`, `0005`→`0300`.

Within the dialect band #264's ordering survives with `0005` lifted out: exists
on the dialect → exists at the declared version → allowed in this position → a
property of what is named. (Listed last above for the same reason — it moves
out of the band the other four stay in.)

### `SqlArtisan.Validity` — a new category ADR 0014 rejected

ADR 0014 recorded a second category for the correlated-DML rule as a *rejected*
alternative, on the grounds that *"a second category would split users'
bulk-severity configuration for one rule"* — while noting the schema split as
the contrasting case, where *"the split buys a knob over a whole family rather
than over one rule."*

**This ADR supersedes that part of ADR 0014.** What changes is not the
cost/benefit but the unit: a band is a family slot, and a rule that mirrors a
`Build()`-time guard is a repeatable shape — the library has many such guards,
and each is a candidate for the same early-surfacing treatment. The rest of
ADR 0014 — the rule's soundness claim, its suppression story, its opt-in gate —
stands unchanged.

Being explicit about the cost, since the ADR should not imply otherwise: **on
the day this lands, `SqlArtisan.Validity` holds exactly one rule.** The band is
justified by the shape of the rule, not by a queue of pending ones.

### What the bands do not buy

Ordering *within* a band still degrades on insertion — a rule that belongs
semantically in the middle is appended at the end. Bands fix the family problem
permanently; nothing short of renumbering on every addition would fix the other,
and that trade is not worth making.

## Consequences

- **Breaking for users' suppressions**, the second time after #264. A
  `dotnet_diagnostic.SQLA000x.severity` line, `#pragma warning disable`, or
  `[SuppressMessage]` written against `0.8.0-beta.1` now targets a different
  diagnostic or none at all.
- **`AnalyzerReleases.Shipped.md` was empty**, so the renumber is a rewrite of
  the `Unshipped` table with no `### Removed Rules` / `### Changed Rules`
  bookkeeping. That is a fact about the RS2000 release-tracking ledger, not
  about users: the packages *are* published on NuGet at 0.x, so suppressions
  exist in the wild regardless.
- **`DiagnosticOrderingTests` gets stronger rather than deleted.** Band → category
  is a standing invariant, so the test now holds for every rule added from here
  instead of expiring once a family fills up.
- **Existing ADRs were updated in place.** ADRs are immutable once Accepted, and
  that rule protects the *decision* — not an identifier that no longer names
  anything. Leaving `SQLA0004` in ADR 0013 would have made it describe a
  diagnostic that does not exist. The same reasoning does **not** extend to
  `CHANGELOG.md`, whose entries are timestamped claims about what a given release
  shipped and remain true only in the old numbering; those keep their old IDs.
- **Heading anchors in `docs/analyzer.md` changed once more.** IDs stay in the
  headings — a user reading `SQLA0101` out of build output should be able to
  find the section — and because the bands remove the pressure that caused both
  previous renumbers, this is intended to be the last time they move. The
  released `CHANGELOG.md` entries that link into those headings were repointed
  to the current anchors: the link *text* names no ID, so the href is a pointer
  into living documentation rather than part of the historical claim, and a
  working link beats a dead one with nothing gained.

Related: #433 (this change), #264 (the first renumber, into semantic order),
#326 / #349 (the `0003` → `0006` move), #266 (the `SqlArtisan.Schema` split),
ADR 0013, ADR 0014 (superseded in part).
