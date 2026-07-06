# Security Policy

## Reporting a vulnerability

Report vulnerabilities **privately** via GitHub Security Advisories:
[Report a vulnerability](https://github.com/h-tacayama/SqlArtisan/security/advisories/new).
Please do not open a public issue for anything you believe is exploitable.

You can expect an acknowledgement within **7 days**. Fixes are developed
privately and released as soon as they are ready; you will be credited in the
advisory unless you prefer otherwise.

## What counts as a vulnerability here

SqlArtisan builds SQL text and bind parameters; it does not execute queries
(the optional Dapper integration hands both to Dapper as-is). The guarantees
worth attacking are:

- **Automatic parameterization** — values always become bind parameters, never
  SQL text. Any input that makes a value render into the SQL string itself
  (SQL injection through the API) is a vulnerability.
- **Inline-literal escaping** — the few positions that deliberately emit
  inline string literals (e.g. the `LIKE ... ESCAPE` character, string-aggregation
  separators) escape them; an escape bypass is a vulnerability.

Emitting SQL that a database rejects, or a wrong dialect-availability claim,
is an ordinary bug — please open a public
[issue](https://github.com/h-tacayama/SqlArtisan/issues) for those.

## Supported versions

| Version | Supported |
|---|---|
| Latest published pre-release (0.x) | ✅ Fixes land in the next release |
| Older pre-releases | ❌ Upgrade to the latest |

From 1.0, security fixes target the latest release of the current major
version. See the
[versioning & support policy](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/versioning.md)
for the full support statement.
