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
