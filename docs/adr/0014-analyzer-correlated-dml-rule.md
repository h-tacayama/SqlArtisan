# ADR 0014 — Analyzer advisory duplication of a runtime guard: the correlated-DML rule

**Status:** Accepted

## Context

The Build()-time guard from #253 rejects a correlated UPDATE/DELETE whose
target is unaliased — the bare outer column resolves to the inner scope, a
silent every-row tautology. The guard's facts are runtime facts: reference
identity (`ReferenceEquals(owner, target)`), the alias string a constructor
received, and the buffer's subquery depth during `Format`. ADR 0011 classed
this family "structurally invisible to the analyzer" — invisible as *values*,
but #256 observed that a useful subset is provable from source: when the
target is a named symbol whose initializer is visible, symbol identity
approximates reference identity and constant propagation approximates the
alias value. Per #239's layering principle, the runtime guarantee stays the
defense; the analyzer adds the cheapest feedback tier (#232's ladder:
types → analyzer → tests → matrix).

## Decision

**A new diagnostic `SQLA0005`, implemented as `CorrelatedDmlRule` in the
existing `DialectUsageAnalyzer`, reporting the provable subset of the
runtime guard's verdict.** Three properties are new to the analyzer and
define the rule class:

- **Advisory duplication.** The rule re-states a verdict the core already
  enforces — suppressing the diagnostic does not disable the `Build()`
  throw. The message mirrors the guard's message (bar the trailing period
  RS1032 forbids on a single-sentence diagnostic), pinned by a parity test;
  every analyzer trigger shape has a twin test that executes
  `Build()` and asserts the real `ArgumentException`, so "the analyzer
  fires only where the runtime throws" is proven by execution, not
  argument.
- **Symbol-identity proof.** The first rule keyed on `ISymbol` comparison
  rather than member names: the target (argument 0 of `Update`/`DeleteFrom`,
  a local or a this-bound `readonly` field) must be the same symbol as the
  receiver of a `DbColumn`-typed member reference inside a subquery of the
  same fluent chain. A field read through another instance
  (`other._t.Dep`) is a different table object at run time and never
  matches.
- **Value-provenance proof.** "Unaliased" is a constructor-argument value,
  so a zero-argument-constructor heuristic would false-positive on a
  constructor hardcoding an alias (`: base("t", "x")`). Instead the value
  reaching `DbTableBase`'s `tableAlias` parameter is traced through the
  ctor-initializer chain (constants and forwarded parameters only, depth
  capped) and must resolve to `""`/`null`. The proof is final because
  `_tableAlias` is `readonly` — a reflection contract test pins that, the
  ctor shape, and the argument-0 targeting.

Scope whitelist (everything else is silent): a local with a visible
provably-unaliased initializer and no textual reassignment in the enclosing
member, or a `readonly` field with such an initializer and no constructor
assignment. The subquery boundary is the source image of the runtime's
`EncloseInParentheses(ISubquery)`: a Select-headed chain bound as an
argument of a SqlArtisan call, scanned only in the arguments of the chain
*after* the DML head — `With(...)` CTE bodies sit on the receiver side and
are structurally invisible, matching the runtime's behavior. Descent stops
at lambdas/local functions.

Accepted false negatives (the ADR 0003 direction — never a false
positive): table classes from referenced assemblies (no declaration
syntax), non-readonly fields, helper indirection for the table or the
subquery, a builder split across statements, and `With(...)`-headed
subqueries. A joined UPDATE/DELETE (`.From(...)` / `.Using(...)` / a join
step **visible in the same expression chain**) with an unaliased target is
deliberately silent: its own Build()-time guard throws a *different*
message ("joined … must be aliased") before the correlated guard arms, so a
"correlated" diagnostic would misdescribe it — the joined guard is the
report there. A join step added on a builder variable in a *later*
statement is invisible to the walk, so that shape still reports
"correlated" while Build() throws the joined message — accepted: the
statement is unbuildable either way, the exception type matches, and the
remediation (alias the target) is identical, so only the message label
diverges. The runtime accepted-false-positive (one instance reused for both
scopes) is reported too — accurately, since that statement cannot build.

One deliberate-circumvention escape is accepted rather than defended
against: reassigning the target through an `in` parameter via
`Unsafe.AsRef` leaves no `ref`/`out` keyword and no `ref` expression for
the no-write scan to count, so a target realiased that way can still be
reported on code that builds. Detecting it would need semantic-model
argument binding for every call — cost out of proportion to code that
defeats the language's own readonly-ref semantics. The soundness claim is
therefore "never a false positive on code that respects readonly-ref
semantics."

Identity decisions follow ADR 0013: standard Roslyn suppression only, no
`sqlartisan_*` key family (a construct-override key would misdescribe the
finding — the construct's dialect support is not what is wrong), one shared
category (`SqlArtisan.Dialect`; a second category would split users'
bulk-severity configuration for one rule, recorded as the rejected
alternative), and the analyzer-wide opt-in gate (silent until
`sqlartisan_target_dbms` resolves; the violation is dialect-independent, so
the rule fires on every configured target).

## Rejected alternatives

- **Reusing `SQLA0003`.** Context rules answer "this position on this
  dialect"; this rule is dialect-independent and duplicates a runtime
  guard — different message shape, different remediation, different
  soundness machinery.
- **A dataflow-analysis implementation** (`DataFlowAnalysis` /
  `ILocalReferenceOperation` flow tracking). Heavier than needed: the
  whitelist covers the shapes users actually write (the repository's own
  test fixtures use exactly the local and readonly-field forms), and the
  conservative textual no-write scan is sound where flow analysis would
  merely be precise.
- **Firing without a configured target** (the violation is universal). It
  would break the analyzer's purely-additive contract — enabling the
  package must not produce diagnostics until the user opts in.

## Consequences

- A second maintenance invariant joins the message: guard message drift now
  breaks a test in `SqlArtisan.Analyzers.Tests` (the parity pin), which is
  the point — the two surfaces must not diverge.
- The symbol-identity and value-provenance machinery is available as
  precedent for future rules that need "same object" or "known ctor value"
  facts (#256's follow-ups in the #232 vision).
- The Analyzer ADR cluster grows to 0003 + 0008 + 0009 + 0013 + 0014.
