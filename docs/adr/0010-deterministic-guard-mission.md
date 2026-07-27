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

### What Tier 2 may learn: the admission test for a schema fact

The tier grows by collecting new facts about the schema, and each fact is a
public attribute property that freezes under SemVer. A fact is admitted only
when all three hold:

1. **Categorical** — the verdict it enables needs no statistics, row counts,
   or plan shape. This is the Tier 3 line restated at the point of collection.
2. **A scalar per column, not an expression to interpret.** A fact whose use
   would require parsing SQL text is not collected; where an expression is
   unavoidable it is *noticed, never interpreted* — matched only well enough
   to produce silence, as #266 does with expression indexes. This clause is
   what keeps `CHECK` constraints out: honoring them would mean carrying a
   five-dialect expression parser, and with it the whole of SQL semantics.
3. **Consumed on arrival** — at least one diagnostic lands with it. No fact is
   collected speculatively.

A fact no engine-wide query can answer is not disqualified; it degrades to the
tri-state's unknown, and unknown is silence.

### What Tier 2 may conclude: query-shape-dependent judgments

A diagnostic that reads only the column's own facts (SQLA0008, SQLA0009) is
cheap and sound. One whose verdict also depends on the shape of the
surrounding query — which joins null-supply the row (SQLA0007, SQLA0010) — is
where the false positives live, and every finding of #365's review landed
there. Such a rule reports only where the statement **visibly builds its own
query**; a chain assembled across statements, in a helper method, or in a
field is left alone rather than judged on what one statement happens to show.

Both clauses are gated by tests, not intent: a shape catalog asserted silent
against every rule that reads the query (`SchemaRuleParityTests`), and a
coverage test that fails when the core ships a join step the analyzer has not
classified.

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
- Growth is bounded by the admission test, not by ambition: with `Nullable`,
  `HasDefault`, `Indexed` (#266) and `DataType` (#362) the categorical
  questions are close to exhausted, and a new one costs five bespoke catalog
  queries plus live verification per engine.
- The no-false-positive property is spent one noisy diagnostic at a time, so
  each schema rule carries its shapes in the shared parity catalog rather than
  in its own suite. Adding a shape there is how a hazard becomes a regression
  test for every rule at once.
- Positioning surfaces (#226, #228) may state the mission in plain words;
  user-facing pages still do not cite ADR numbers (docs-style rule).
- CLAUDE.md carries a summary of this decision; this ADR is the source.
