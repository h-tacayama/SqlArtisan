# ADR 0023 — C# fallbacks: a query object C# resolves without SqlArtisan

**Status:** Accepted

## Context

SqlArtisan builds SQL from C# operators, and C# falls back to its own built-in
operators whenever no user-defined one applies.
`.claude/rules/public-api-design.md` § *Operator overloads* requires every operand
order of every overloaded operator to bind a SqlArtisan operator or fail to
compile, and #613 closed the orders an overload can close. The A4 review (#558)
left three open, because no overload can reach them:

- `==` / `!=` with a reference-typed left operand not derived from
  `SqlExpression` (`object`, `SqlPart`, a type parameter constrained to one of
  them). C# compiles reference equality, and the argument binds a `bool`.
- `+` between a `string` and a query object no SqlArtisan `+` takes: a subquery
  stage, a node still pending its clause (`RowNumber()` before `.Over(...)`),
  an `object`. C# concatenates `ToString()`, which is the type name.
- Interpolation (`$"{col}"`), which reaches `ToString()` with no operator at
  all.

Each compiles, and each auto-parameterizes a value the author never meant: the
query runs and returns the wrong rows. No `Build()` guard can see it either —
by the time SqlArtisan receives the argument, it is an ordinary `bool` or
`string`.

## Decision

**Two analyzer rules report these, as Warnings, only where the result flows
straight into an argument of a SqlArtisan member or an operand of a SqlArtisan
operator.**

- **`SQLA0301`** — C# reference equality with a query-object operand.
- **`SQLA0302`** — a query object interpolated or concatenated into a string.
  One id for both forms: each formats the object through `ToString()`, the
  remedy is the same (build the text in SQL), and a user who silences one wants
  the other silenced too — ADR 0019's splitting test says merge.

`SQLA0301` reads any reference type from SqlArtisan, or one deriving from or
implementing one; a type parameter counts when a constraint does. Reference
equality binds a `bool` whatever the object's `ToString()` says.

`SQLA0302` needs more: proof the text is a type name. That holds when nothing on
the static type's chain overrides `ToString()` *and* no subclass can hide behind
it — the type is sealed (`DbColumn`, `DbSequence`, a generated table class), or
a SqlArtisan member created the object (a builder stage, `Listagg(...)` before
`.WithinGroup(...)`). Any other static type stays silent, whether or not the
value sits in a variable: a `DbTableBase`- or `SqlPart`-typed value, or a type
parameter constrained to one, may hold a user table class whose override
returns real text, and the rule does not split the remaining non-sealed types
(`SqlExpression`, `ISubquery`, a builder stage held in a `var`) from those. A built `SqlStatement` is never reported:
its `ToString()` is its SQL text, by design.

### Only the flow into SqlArtisan is reported

The same C# is correct code anywhere else: a log line interpolating a column, a
test asserting two parts are one instance. ADR 0003 keeps the analyzer silent
where it cannot prove the hazard, so the rule follows the value from the
operator upward through implicit conversions, an enclosing string
concatenation, and the compiler's `params` array, and reports only on reaching
an argument of a SqlArtisan invocation or constructor, or an operand of a
SqlArtisan operator (`col == $"{col}"` binds the type name as surely as a
`Select` argument does). A result held in a local, a ternary, a helper's return
value stops the walk — and stays silent, the price of no false positives. An
`object` on both sides of `==` / `!=`, or beside a string's `+`, stays silent
for the same reason: nothing proves a query object is involved.

`SQLA0301` also needs the `bool` to land in a parameter of another type, where
it is boxed and bound. `ConditionIf(bool when, …)` takes the C# test on purpose
— `ConditionIf(extra != null, extra)` is the optional-filter idiom — so a
`bool` parameter is correct code, not the hazard. A comparison with a `null`
constant (`null`, `default`, a null `const`) is skipped wherever it lands: no
operand order makes a null check SQL, so it is never the swapped-operand
mistake the rule names.

### The band

Both rules sit in `SqlArtisan.Validity`, whose question ADR 0018 put as "is this
a statement `Build()` would reject?". That question was the shape of its one
rule, not a boundary: `SQLA0300` and these two answer *can this statement mean
what it says?* — the first because a scope resolves wrongly, these because C#
resolved the operator. The Configuration, Dialect and Schema bands each answer
something else, and a fifth band for two rules would split a bulk-severity
setting across one family. The band's question is widened in the analyzer
reference.

### Gated on a configured target, like every rule

The hazard holds on every engine, but the analyzer as a whole is opt-in (ADR
0003), so both rules run only when a dialect is configured — the same gate
`SQLA0300` and the schema rules already sit behind.

## Consequences

- **The open list in `public-api-design.md` points here** instead of to #614.
  An order a future overload closes leaves the rule silent on it by
  construction: a user-defined operator binds, and the rule reads only C#'s
  built-in ones.
- **A missing warning never means the operand is safe.** A result routed
  through a variable, `object` on both sides, or an operand whose static type
  is not sealed and that no SqlArtisan member has just created reaches the same
  `ToString()` unreported.
- **Suppression is per rule ID**; no `.editorconfig` key family ships.

Related: #614 (this change), #558 (the review), #613 (the overloads that
closed the closable orders), ADR 0003 (provable or silent), ADR 0014 (an
advisory rule), ADR 0018 (the bands), ADR 0019 (the splitting test).
