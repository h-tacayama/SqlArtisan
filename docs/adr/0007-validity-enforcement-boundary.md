# ADR 0007 — What the library rejects: grammatical completeness vs dialect availability

**Status:** Accepted

## Context

ADR 0001 makes the author own DBMS correctness and has the library emit faithfully;
ADR 0003 keeps the API permissive and pushes *wrong-DBMS* detection to an opt-in
analyzer (and ultimately the database). Neither says what the **library itself**
should reject outright.

Two changes made the library throw for some misuse rather than emit it: window /
analytic functions used without `.Over(...)` (#150) and the ordered-set aggregates
(`Listagg`, `PercentileCont`, `PercentileDisc`) used without `.WithinGroup(...)`,
plus the actionable message for both (#190). On the surface this looks like it
contradicts "emit faithfully; the analyzer is the safety net." It does not — but
only because these sit on a different axis, and that boundary was never written
down. This ADR records it.

## Decision

The library rejects **only constructs that are incomplete — ungrammatical in every
supported dialect because a mandatory element is missing.** A window function has
no meaning without `OVER`; an ordered-set aggregate has none without
`WITHIN GROUP (ORDER BY ...)`. There is no dialect, and no configuration, in which
the bare token is valid SQL, so it is not "SQL you wrote that we declined to run" —
the expression is simply unfinished.

Such cases are enforced **as early as the API surface allows:**

- **Compile time** where the position is typed `SqlExpression`. The mandatory
  trailing clause uses the "pending" type pattern — the incomplete form is
  deliberately *not* a `SqlExpression`, and only the completing call (`.Over(...)`,
  `.WithinGroup(...)`) yields one. This matches the existing convention of making
  invalid fluent chains uncompilable via the return type.
- **Runtime** (`ArgumentException`) as a backstop in the `object`-typed value
  positions the compiler cannot constrain — `Select(params object[])`, `OrderBy`,
  `GroupBy`, `Values`, and the resolved operands of `==`. The message names the
  completing call (#190).

The library does **not** judge **dialect availability** — a *complete* construct
that some engine happens not to support (e.g. `CUBE` on MySQL, or any `GROUP BY`
extension on SQLite). That is emitted faithfully (ADR 0001) and surfaced by the
opt-in analyzer (ADR 0003), with the database as the final arbiter. The library
never throws for it.

The dividing test: **is there any supported dialect, in any configuration, where
this exact text is valid SQL?** If no → incomplete → the library rejects it. If
yes-somewhere → dialect availability → permissive.

## Consequences

- **Two safety mechanisms on orthogonal axes, not redundant ones.** Completeness is
  always-on and type-first (a property of whether you finished writing the
  expression); availability is opt-in, build-time, and advisory (a property of the
  target engine). The runtime throw is not "doing the analyzer's job early."
- **A deliberate, bounded exception to faithful emission.** ADR 0001's "emit what
  you wrote" presumes a complete expression. An incomplete one has no faithful SQL
  to emit, so rejecting it does not weaken ADR 0001.
- **Mechanism order is fixed:** prefer compile-time (pending / narrowed types);
  fall back to a runtime exception only where the surface is `object`-typed. New
  mandatory-clause constructs follow the same pattern.
- **Guardrail:** the library must never throw for dialect availability. A future
  change that, say, threw on `CUBE` for MySQL would violate this ADR — that belongs
  to the analyzer and the database. ADR 0011 carves one narrow, enumerated
  exception — an aliased `UPDATE`/`DELETE` target on SQL Server — admitted only
  because the analyzer structurally cannot see the construct *and* the resolved
  target has no valid spelling at all; any further exception must clear the same
  bar.
- **Scopes the analyzer (#93):** it need not re-check completeness the type system
  already guarantees; its remit stays dialect availability, arity, and unknown
  target.
- Complements ADR 0001 and ADR 0003; supersedes neither. Refined by ADR 0011
  (a bounded exception). See #150, #190, #93.
