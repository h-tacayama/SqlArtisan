# ADR 0009 — Analyzer distribution: bundled in the core package, coupled only by contract

**Status:** Accepted

## Context

The dialect-usage analyzer (#93; ADR 0003/0008) ships inside the main
`SqlArtisan` nupkg (`analyzers/dotnet/cs`), so every consumer gets it with no
extra package reference and it stays silent until configured. #223 proposed
splitting it into its own `SqlArtisan.Analyzers` package for an independent
release cadence — the standard shape for first-party analyzers with fast-moving
rules (`xunit.analyzers`, `Roslynator.Analyzers`).

The project's goal is adoption growth of a free library. The analyzer is its
most differentiating feature, and a separate package turns "install SqlArtisan
and it just works" into a second install step most users would never take.
The distribution shape also had to be decided *before* the analyzer's first
release: un-bundling after shipping bundled silently removes diagnostics from
every consumer on their next update — a behavioral break SemVer does not
capture — so whichever shape ships first is effectively permanent.

## Decision

**The analyzer stays bundled in the `SqlArtisan` package, permanently.** The
precedent is the packages that treat analysis as part of the product's quality
story (`System.Text.Json`, `Microsoft.Extensions.Logging`), not the ones that
sell or version analysis separately.

Independence is preserved *architecturally* instead of by packaging: coupling
between the core and the analyzer is limited to a **three-point contract** —

1. **Assembly identity** — the analyzer recognizes usages by
   containing-assembly *name* (`"SqlArtisan"`); there is no build reference in
   either direction.
2. **Member names** — matrix keys mirror the core's public member names,
   enforced both ways by the integrity and coverage gate tests.
3. **Configuration surface** — the `sqlartisan_target_dbms` /
   `sqlartisan_construct_*` `.editorconfig` keys and the
   `build_property.SqlArtisanTargetDbms` MSBuild property.

No shared types and no `InternalsVisibleTo` between the two shipped
assemblies (the analyzer's `InternalsVisibleTo` grants go to test assemblies
only). Analyzer-owned artifacts live in the analyzer project — the
buildTransitive props (`src/SqlArtisan.Analyzers/build/SqlArtisan.props`) sits
with its owner and the core package merely carries it.

## Consequences

- **A matrix correction ships as a core patch release** with no library
  changes. Accepted: one release line is cheaper to operate than two, and the
  core's release process is lightweight.
- **Bundling is a one-way door and we walked through it knowingly.** Removing
  the analyzer from the package later would silently drop diagnostics for
  every consumer; any future extraction would have to be an additive
  compatibility shim, not a removal.
- **The three-point contract is the complete list of seams.** If extraction is
  ever genuinely needed (it is not planned), the analyzer plus its unit tests,
  gates, and dialect sweep move as one unit and reference the core as a NuGet
  package; nothing else holds them here.
- **Guardrail:** never add a build reference or shared type between the core
  and the analyzer. The analyzer must stay loadable and correct against any
  core version — the gates, not the compiler, define compatibility.
- Closes #223 (considered and declined; its loose-coupling substance is
  folded into this contract). See ADR 0003 (the analyzer's reason to exist)
  and ADR 0008 (its configuration design).
