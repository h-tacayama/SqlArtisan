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

- **Automatic parameterization** — a value you supply is bound as a parameter,
  or, where the grammar demands a literal, inlined with its quotes escaped.
  Either way it never reaches the SQL text as raw input; anything that gets it
  there (SQL injection through the API) is a vulnerability.
- **Inline-literal escaping** — the positions that take that literal path
  (e.g. `LIKE ... ESCAPE`, string-aggregation separators) double `'` on every
  dialect and `\` on MySQL; a bypass is a vulnerability.

Several slots do inline a value unescaped — an `ORDER BY` ordinal, a `LAG` /
`LEAD` offset, a window-frame bound, `NTILE`'s bucket count and `FOR UPDATE
WAIT`'s seconds among them. None can carry a string: each is `int`- or
`double`-typed at the factory, or verified numeric before it is written.

The two guarantees above are the whole protected surface. **Identifier positions
are emitted verbatim by design** — an alias, a table or column name, a `CAST`
target type, and a sequence name in the Oracle (`s.NEXTVAL`) and SQL Server
(`NEXT VALUE FOR s`) spellings. These carry tokens you wrote, so the library
neither escapes nor sanitizes them; the SQL you write is the SQL that runs. The
same holds for `Sql.Hints`, which is not an identifier but a raw-SQL escape
hatch — emitted verbatim by definition. Deriving any of them from untrusted
input is an application-level issue — validate it against an allowlist at that
boundary — not a vulnerability in SqlArtisan. (PostgreSQL's `NEXTVAL('s')` *is*
escaped: its grammar takes the sequence name as a string literal, so it falls
under the second guarantee above.)

Rejected SQL or a wrong dialect-availability claim is an ordinary bug — open
a public [issue](https://github.com/h-tacayama/SqlArtisan/issues) for those.

## Supported versions

Only the latest published version is supported — no backporting; upgrade
for fixes. See the
[versioning & support policy](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/versioning.md)
for the full statement.
