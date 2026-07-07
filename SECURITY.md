# Security Policy

## Reporting a vulnerability

Report vulnerabilities privately via [GitHub Security Advisories](https://github.com/h-tacayama/SqlArtisan/security/advisories/new) — not a public issue.

Reports are triaged as promptly as possible. This is an individually-maintained
project, so there's no fixed response SLA or fix timeline. A fix, if made, is
developed privately and released once ready, with credit in the advisory
unless you prefer otherwise.

## What counts as a vulnerability here

SqlArtisan builds SQL text and bind parameters — it does not execute queries
(the optional Dapper integration hands both to Dapper as-is). The guarantees
worth attacking:

- **Automatic parameterization** — values always become bind parameters,
  never SQL text. Anything that makes a value render into the SQL text
  itself (SQL injection through the API) is a vulnerability.
- **Inline-literal escaping** — the few positions that emit inline literals
  (e.g. `LIKE ... ESCAPE`, string-aggregation separators) escape them; a
  bypass is a vulnerability.

Rejected SQL or a wrong dialect-availability claim is an ordinary bug — open
a public [issue](https://github.com/h-tacayama/SqlArtisan/issues) for those.

## Supported versions

Only the latest published version is supported — no backporting; upgrade
for fixes. See the
[versioning & support policy](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/versioning.md)
for the full statement.
