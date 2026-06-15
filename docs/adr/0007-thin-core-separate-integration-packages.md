# 0007 — Thin core; integration and execution live in separate opt-in packages

**Status:** Accepted

## Context

"Build SQL" and "execute / materialise SQL" are different concerns with different
dependencies. Folding execution helpers, ORM features, or provider integrations
into the core would add dependencies and weight to a library whose value is a
lightweight, fast SQL-string builder (ADR 0006).

## Decision

The **core `SqlArtisan` package produces SQL text + parameters and nothing more.**
Integration and execution concerns ship as **separate, opt-in packages**.

- Existing/planned satellites: **`SqlArtisan.Dapper`** (execution integration),
  **`SqlArtisan.TableClassGen`** (a tool that generates table classes), and the
  planned **`SqlArtisan.BulkCopy`** (high-throughput execution, e.g. Oracle array
  binding — issue #90).
- Explicitly **out of core scope**: stored-procedure execution, true bulk-copy in
  core, POCO/column-name mapping, type handlers (see Epic #91).

Note: a build-time **analyzer** (ADR 0005) is *not* an exception to "thin core" —
it carries no runtime dependency and may ship bundled, because the rule protects
the **runtime** footprint, not the package's build-time tooling.

## Consequences

- The core stays dependency-light and easy to adopt; users pull only the
  integration they need.
- More packages to version and document; the "core thin / integration separate"
  boundary must be applied consistently when new execution-shaped features arrive.
- Features are evaluated against this boundary: if a request is about *executing*
  SQL rather than *building* it, it belongs in a satellite package, not core.
