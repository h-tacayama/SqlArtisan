# ADR 0004 — Values are automatically parameterized

**Status:** Accepted

## Context

Inlining literal values invites SQL injection and defeats statement caching. A
builder that produces runnable SQL must make the safe path the default.

## Decision

Literal values are **bound as parameters**, not inlined; a built `SqlStatement`
carries the SQL text and its parameters. Parameter markers follow the dialect
(ADR 0002).

## Consequences

- Safe by default, and cache-friendly (values do not vary the SQL text).
- A few constructs *require* a literal and reject a parameter (e.g. MySQL
  `GROUP_CONCAT ... SEPARATOR`); these emit a literal by design.
- This affects value *binding* only, not the meaning of the query (ADR 0001).
