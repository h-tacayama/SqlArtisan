# 0004 — Values are automatically parameterized

**Status:** Accepted

## Context

Inlining literal values into SQL text invites SQL injection and defeats statement
caching/plan reuse. A query builder that produces "the SQL you run" must make the
safe path the default one.

## Decision

- Literal values passed to the API are **bound as parameters**, not inlined. A
  built `SqlStatement` carries both the SQL **text** and its **parameters**.
- Parameter markers are dialect-correct (see ADR 0003): PostgreSQL/Oracle/SQLite
  `:`, SQL Server `@`, MySQL `?`, with stable generated names.

## Consequences

- **Safe by default**; the common path produces parameterized SQL with no extra
  effort.
- **Cache-friendly** SQL text (values do not vary the string).
- **Known exceptions** must be handled deliberately: a few constructs *require* a
  literal and reject a bind parameter (e.g. MySQL `GROUP_CONCAT ... SEPARATOR`
  takes a string literal). Such cases emit a literal by design and are documented
  at the API.
- Output remains faithful (ADR 0001): parameterization changes value *binding*,
  not the meaning of the query.
