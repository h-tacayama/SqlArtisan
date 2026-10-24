---
description: Implementing an analyzer rule — what a rule may read, and what it concludes when it cannot
paths:
  - "src/SqlArtisan.Analyzers/**/*.cs"
---

# Implementing an analyzer rule

- **A symbol's syntax outside the compilation is unknown.** A rule reads a
  declaration only through `SourceDeclaration`, which returns syntax and a
  semantic model from the compilation under analysis, or nothing. A table class
  in a referenced project is reachable from its symbol in the IDE, yet
  `GetSemanticModel` throws on its tree (AD0001), and an emitted reference
  carries no syntax at all. Either way the fact is unknown and the rule is
  silent, so the editor and `dotnet build` agree. The same holds for a
  diagnostic's location: one taken from a symbol's `Locations` in another
  project's tree throws when reported, so a rule never reports at another
  compilation's syntax.
  `RuleGuardCoverageTests.NoRule_ReadsDeclarationSyntaxOutsideSourceDeclaration`
  holds the routing, and `SchemaRuleParityTests` runs its whole catalog a
  second time with the table class in a referenced project.
- **An operation that is not evaluated is not a usage.** An operand of `nameof`
  builds no SQL, so no rule reports on it: `DialectUsageAnalyzer` registers
  every operation action through one wrapper that skips them. A context that
  names a member without calling it is the same case, whichever rule reads it.
- **A constant the core rejects at the call or at `Build()` belongs to the
  core's guard.** A dialect rule stays silent on it, so the message never
  points at the wrong problem: a `GroupBy` ordinal below 1, a cast-in
  undefined `DateTimePart`, and a `RegexpOptions` value with a bit no member
  defines.
- **Every null-supplying construct is classified.** A schema rule silenced
  past an outer join is silenced past a super-aggregate grouping too
  (`FluentChain.NullSupplyingGroupings`), and the parity gate reflects both
  sets from the core (`SchemaRuleParityTests.EveryCoreJoinStep_IsClassified`,
  `EveryCoreNullSupplyingGrouping_IsClassified`), so a new join step or
  grouping element fails the suite until it is placed.
- **A climb that leaves through a variable, a helper, a lambda or a
  conditional proves nothing about what lies beyond it.** A rule that reports
  the *absence* of an enclosing construct reports only where the climb ends at
  a SqlArtisan step that settles it: `SQLA0102` at a builder step other than
  `Output(...)`, `SQLA0200` for a correlated column at a visible chain reading
  its table with every chain in between visible. A member on an operand
  (`col.In(...)`, `cte.As(...)`) or a static factory only wraps, and is
  climbed through rather than read as a chain.
