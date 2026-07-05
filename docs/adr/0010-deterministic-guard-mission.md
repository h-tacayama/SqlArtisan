# ADR 0010 — The mission: deterministic guard rails for AI-assisted SQL

**Status:** Accepted

## Context

ADR 0001 fixed the emission philosophy — the SQL you write is the SQL that
runs — and ADRs 0002/0003/0007 built the machinery around it. The #225
expressibility audit and its triage (#231–#245, consolidated in #232) forced
the question of what that machinery is ultimately *for*, and produced two
observations:

1. **Generative AI changes who writes the SQL.** Assistants produce
   plausible-but-wrong SQL probabilistically; a reviewer can no longer assume
   the author knew the target DBMS. What a library can uniquely add is
   *determinism*: constraints that hold every time, not advice that holds
   usually.
2. **Encoding an opinion as an API hole fails.** `COUNT(*)` was deliberately
   omitted to steer users toward counting indexed columns. The audit showed
   the omission was invisible, unexplained, unsuppressible — and the premise
   does not hold on modern engines. The adoption test is binary ("one of our
   queries can't be written → we're out"), so the omission cost adoption
   without delivering the guidance (#233).

## Decision

**SqlArtisan's mission is to be a deterministic guard rail for SQL written
alongside generative AI. Faithful emission (ADR 0001) is the foundation of
that mission, not the whole of it.**

The guard is a ladder of layers, each deterministic because it rests on the
one below (the analyzer reasons over a typed AST, not strings):

1. **Types** — misuse fails to compile (narrowed step interfaces, pending
   types; ADR 0007) or throws loudly.
2. **Analyzer** — deterministically flags what the configured target rejects
   (ADR 0003). Its knowledge scope is tiered:
   - *Dialect availability* — shipped (the dialect matrix, #93).
   - *Version boundaries and literal-decidable limits* (feature introduction
     points, identifier-length ceilings) and *schema-aware **categorical**
     diagnostics* (a nullable column under `COUNT`/`NOT IN`, SARGability
     loss) — the planned direction, tracked in #232.
   - ***Cost-based judgments are permanently out of scope.*** "This query is
     slow" depends on statistics and hardware — the optimizer's domain.
     Guessed-cost warnings would break the no-false-positive property that
     makes the analyzer trustworthy (ADR 0003's degradable design).
3. **Exact-SQL unit tests** — pin the emission.
4. **Live-engine integration matrix** — prove it runs (#151).

**Corollary: opinions live in docs and the analyzer, never in API holes.** No
legitimate SQL spelling is omitted to steer users; guidance is delivered as
an explicit, sourced, suppressible diagnostic or a docs note.

## Consequences

- The `COUNT(*)` omission is reversed (#233); its guidance moves to docs.
- Feature triage judges additions by whether they strengthen a deterministic
  layer or unblock a constituency's adoption (planned waves #237/#159), and
  can still say wontfix where a same-dialect, vendor-recommended workaround
  exists (CONNECT BY, PIVOT, legacy index hints — #225).
- Tier 2 *extends* the analyzer's planned remit beyond the
  dialect-availability scope noted in ADR 0007's consequences (and ADR 0003's
  original out-of-scope list); the decisions themselves — the permissive API
  and the rejection boundary — stand unchanged.
- Analyzer facts require a primary source or live verification; unverified
  grammar claims carry the `grammar-unverified` tag. Version-bounded and
  context-bounded facts the construct-level matrix cannot express are
  recorded as docs notes plus #232 seeds — never as wrong matrix entries.
- Positioning surfaces (#226, #228) may state the mission in plain words;
  user-facing pages still do not cite ADR numbers (docs-style rule).
- CLAUDE.md carries a summary of this decision; this ADR is the source.
