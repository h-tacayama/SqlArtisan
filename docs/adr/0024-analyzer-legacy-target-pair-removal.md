# ADR 0024 — Removing the legacy target pair: a removed key is reported, never ignored

**Status:** Accepted

## Context

ADR 0019 replaced `sqlartisan_target_dbms` / `sqlartisan_target_version` (and
their `<SqlArtisanTargetDbms>` / `<SqlArtisanTargetVersion>` MSBuild
properties) with the `sqlartisan_syntax_*` family. It kept the pair working,
deprecated under `SQLA0002`, with an extra `SQLA0001` reason for the pair
coexisting with a family that drops its DBMS. That deprecation warning existed
so the pair's removal "in the major version that removes it" would be expected
rather than sudden.

1.0 is that major. Shipping the pair in it would bind the legacy resolution
path, the coexistence rules and `SQLA0002` until 2.0. Removing it now costs
nothing beyond the deprecation already in place (#654).

## Decision

**The pair is removed: its keys configure nothing.** The resolved target set
comes from the family alone.

**A removed key still set reports `SQLA0001`; it is never dropped unread.** An
unconfigured analyzer is silent, so a project left on the pair would otherwise
lose every diagnostic and read the silence as a clean build. Each key is
reported on the surface it was set on, with the family line that replaces it
in that surface's spelling:

- With no family line for that DBMS, the replacement is built from the pair,
  carrying its version over (`= any` would shed the dialect's `SQLA0101`
  coverage).
- With a family line already naming that DBMS — the mid-migration state — the
  report quotes that line and asks for the leftover key to be deleted. Any
  replacement built from the pair would override the user's newer value, or
  re-enable a dialect they set to `none`.

The two `CompilerVisibleProperty` entries for the removed MSBuild properties
stay, solely so their removal can be reported; a blank value (the SDK emits
one for every declared property) is unset.

**`SQLA0002` is retired and never reused.** A suppression or escalation written
for it would otherwise attach silently to whatever rule took the number.
`DiagnosticOrderingTests.RetiredId_IsNeverReused` gates it.

## Consequences

- A project still on the pair gets no dialect checks until it migrates, and a
  `SQLA0001` per removed key naming the line to write. A leftover
  `<NoWarn>SQLA0002</NoWarn>` is inert.
- ADR 0019's legacy-pair decisions — the pair still resolving, `SQLA0002`, and
  the coexistence `SQLA0001` reason — are superseded. The rest of 0019 stands.
- The reporting path reads the four removed keys for as long as 1.x ships;
  dropping it is a decision for a later major.

Related: #654 (this change), #432 / ADR 0019 (the family and the deprecation),
ADR 0018 (the id bands), #655 (recording the analyzer's configuration surface
in `docs/versioning.md`).
