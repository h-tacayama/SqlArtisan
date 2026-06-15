# 0001 — The SQL you write is the SQL that runs; portability is a non-goal

**Status:** Accepted

## Context

SQL query builders typically abstract over SQL to gain *cross-database
portability* — one query that runs unchanged on many DBMS. That abstraction is
also their main source of surprise: the generated SQL diverges from what the
author intended, performance characteristics are hidden, and vendor-specific
power is lost.

SqlArtisan targets developers who know their target DBMS and want to write
deliberate SQL in C# with type safety, low allocation, and predictable output.

## Decision

**SqlArtisan emits the SQL you wrote, faithfully. Cross-database portability is a
deliberate non-goal.**

- No semantic rewriting of the user's SQL to "make it portable".
- DBMS differences are honoured **only where the *syntax* genuinely differs**
  (identifier/alias quoting, parameter markers, pagination, etc.), via the
  dialect layer (see ADR 0002) — never by changing what the query means.
- Where a construct exists under different spellings or shapes per DBMS, the
  library exposes those per dialect rather than inventing one unified rewrite.

## Consequences

- **Predictable, inspectable output**: the produced `SqlStatement.Text` matches
  the author's intent; tests assert exact SQL strings.
- **The user owns DBMS correctness.** This is the deliberate trade for fidelity;
  guidance for it is a separate, opt-in concern (see ADR 0003), not a rewrite.
- **No false portability promise** — clearer expectations than abstraction-heavy
  ORMs/builders.
- Features whose only purpose is to run one query unchanged across DBMS are out of
  scope.
