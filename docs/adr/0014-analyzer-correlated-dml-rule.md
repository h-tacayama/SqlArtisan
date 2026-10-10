# ADR 0014 — Analyzer advisory duplication of a runtime guard: the correlated-DML rule

**Status:** Accepted — the shared-category decision superseded in part by
[ADR 0018](0018-analyzer-diagnostic-id-bands.md), which gives this rule the
`SqlArtisan.Validity` category the *Identity decisions* paragraph below
rejects; the opt-in gate's `sqlartisan_target_dbms` is now
`sqlartisan_syntax_<dbms>`, the old key removed by
[ADR 0024](0024-analyzer-legacy-target-pair-removal.md). Everything else here
stands.

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

**A new diagnostic `SQLA0300`, implemented as `CorrelatedDmlRule` in the
existing `DialectUsageAnalyzer`, reporting the provable subset of the
runtime guard's verdict.** Three properties are new to the analyzer and
define the rule class:

- **Advisory duplication.** The rule re-states a verdict the core already
  enforces — suppressing the diagnostic does not disable the `Build()`
  throw. The message mirrors the guard's message (bar the trailing period
  RS1032 forbids on a single-sentence diagnostic), pinned by a parity test;
  each trigger shape in the parity suite has a twin test that executes
  `Build()` and asserts the real `ArgumentException`, so agreement with the
  runtime is proven by execution for those shapes, not argued for all. The
  rule can still report code that builds; the known cases are recorded
  below.
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
are structurally invisible. The runtime guards a CTE body block by block,
each set-operator branch a block of its own, throwing where a block
correlates without listing the target (#607, #611). This walk does not model
a block's listing, so it leaves every CTE body to `Build()`: it does not
descend into the arguments of `With(...)` / `WithRecursive(...)`, whether the
chain heads the statement or is passed as a subquery or a MERGE `USING`
source. Walking a body would report one that reads the target as its own
relation, which `Build()` accepts (#610); a body that correlates loses only
the early report, since `Build()` throws. The rule reports once per
statement, on a target column read inside a subquery. Descent stops at
lambdas/local functions.

Accepted false negatives (the ADR 0003 direction — silence where the walk
cannot prove the shape): table classes from referenced assemblies (no
declaration syntax), non-readonly fields, helper indirection for the table
or the subquery, a builder split across statements, `With(...)`-headed
subqueries, and every CTE body. A joined UPDATE/DELETE (`.From(...)` /
`.Using(...)` / a join step **visible in the same expression chain**) with
an unaliased target is deliberately silent: its own guard throws a
*different* message ("joined … must be aliased") at the joining call,
before the correlated guard arms, so a "correlated" diagnostic would
misdescribe it — the joined guard is the
report there. A join step added on a builder variable in a *later*
statement is invisible to the walk, so that shape still reports
"correlated" while the join step throws the joined message — accepted: the
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
defeats the language's own readonly-ref semantics.

The rule identifies a target column by its receiver symbol, not by the
column's run-time owner, so a table class exposing another instance's
`DbColumn` (`public DbColumn OrderId => _o.Id;`) reports on code that
builds; TableClassGen emits columns owned by their own instance. The walk
is also flow-insensitive: a correlated subquery under `ConditionIf(false,
…)` or in an untaken `?:` arm reports though it never renders. So the rule
fails toward silence where it cannot prove a shape, but it is not free of
false positives: these cases and the `Unsafe.AsRef` escape are the known
ones, not a closed list.

Identity decisions follow ADR 0013: standard Roslyn suppression only, no
`sqlartisan_*` key family (a construct-override key would misdescribe the
finding — the construct's dialect support is not what is wrong), one shared
category (`SqlArtisan.Dialect`; a second category would split users'
bulk-severity configuration for one rule, recorded as the rejected
alternative — the #266 schema rules later took a `SqlArtisan.Schema`
category of their own, where the split buys a knob over a whole family
rather than over one rule; this rule stays where it is), and the
analyzer-wide opt-in gate (silent until
`sqlartisan_target_dbms` resolves; the violation is dialect-independent, so
the rule fires on every configured target).

## Rejected alternatives

- **Reusing `SQLA0102`.** Context rules answer "this position on this
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
- **A MERGE `USING` derived-table source reading the unaliased target is
  guarded; a CTE body block listing it as its own relation is not** (a block
  that correlates without listing it is, #607). The source resolves in its own
  scope, as the CTE body does, but it is also where the bare-column tautology
  appears, aliasing the target is the documented remedy, and the loud path
  costs nothing. The asymmetry is deliberate.
- **`MergeInto(...)` is a target too** (release audit, pass 6). The runtime
  guard has armed for MERGE since pass 4, and the rule fell behind it: a
  `MergeInto(t).Using(subquery-reading-t)` chain was silent. The rule now
  dispatches on `MergeInto` with the same alias and reference facts; a
  MERGE has no joined form, so the joined-step exemption above does not
  apply to it. The false-negative list is otherwise unchanged.
