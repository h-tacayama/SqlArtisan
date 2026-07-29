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

Those two are the whole surface. **Identifier positions are emitted verbatim by
design** — an alias, a table or column name, a `CAST` target type, and a sequence
name in the Oracle (`s.NEXTVAL`) and SQL Server (`NEXT VALUE FOR s`) spellings.
These carry tokens you wrote, so the library neither escapes nor sanitizes them;
the SQL you write is the SQL that runs. The same holds for `Sql.Hints`, which is
not an identifier but a raw-SQL escape hatch — emitted verbatim by definition.
Deriving any of them from untrusted input is an application-level issue —
validate it against an allowlist at that boundary — not a vulnerability in
SqlArtisan. (PostgreSQL's `NEXTVAL('s')` *is* escaped: its grammar takes the
sequence name as a string literal, so it falls under the second guarantee
above.)

Rejected SQL or a wrong dialect-availability claim is an ordinary bug — open
a public [issue](https://github.com/h-tacayama/SqlArtisan/issues) for those.

## Supported versions

Only the latest published version is supported — no backporting; upgrade
for fixes. See the
[versioning & support policy](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/versioning.md)
for the full statement.
