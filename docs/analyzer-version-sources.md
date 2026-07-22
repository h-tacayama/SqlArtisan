# Analyzer version-bound sources

[← Back to the Analyzer reference](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/analyzer.md#version-bound-constructs)

The minimum versions in the analyzer's
[version-bound constructs](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/analyzer.md#version-bound-constructs)
table are drawn from each vendor's own documentation, linked below. They are
more than citations: the integration suite runs each construct against a live
engine at that dialect's verified baseline, so the "supported from version N"
direction is reproduced, not just quoted. The "unsupported below N" direction
rests on the documentation alone, because the suite does not pin a
below-baseline image of every engine.

**MySQL** — the reference manual gives the introducing release for each:

- [`WITH RECURSIVE` / common table expressions](https://dev.mysql.com/doc/refman/8.0/en/with.html) — 8.0.1.
- [`GROUPING()` under `WITH ROLLUP`](https://dev.mysql.com/doc/refman/8.0/en/group-by-modifiers.html) — 8.0.1.
- [`INTERSECT` / `EXCEPT`](https://dev.mysql.com/doc/refman/8.0/en/set-operations.html) — 8.0.31.
- [`FOR UPDATE ... NOWAIT` / `SKIP LOCKED`](https://dev.mysql.com/doc/refman/8.0/en/innodb-locking-reads.html) — 8.0.1.
- [row-alias `INSERT ... AS new ON DUPLICATE KEY UPDATE`](https://dev.mysql.com/doc/refman/8.0/en/insert-on-duplicate.html) — 8.0.19.
- [`JSON_VALUE()`](https://dev.mysql.com/doc/refman/8.0/en/json-search-functions.html) — 8.0.21.

(`WITH RECURSIVE` and `NOWAIT` / `SKIP LOCKED` arrived in the 8.0.1 development
milestone; their bounds round to `8.0`, whose first production release — 8.0.11
— already includes them.)

**Oracle** — `EXCEPT [ALL]`, `INTERSECT ALL`, and `MINUS ALL` are new in Oracle
Database 21c, per the
[21c New Features Guide](https://docs.oracle.com/en/database/oracle/oracle-database/21/nfcon/)
and the
[21c SQL Language Reference](https://docs.oracle.com/en/database/oracle/oracle-database/21/sqlrf/sql-language-reference.pdf)
(the set-operators section).

**PostgreSQL** — the
[version 15 release notes](https://www.postgresql.org/docs/15/release-15.html)
list both `MERGE` and the `regexp_count` / `regexp_instr` / `regexp_like` /
`regexp_substr` family as new in 15.

**SQLite** — the per-release change logs:

- [`NULLS FIRST` / `NULLS LAST`](https://sqlite.org/releaselog/3_30_0.html) — 3.30.0 (2019-10-04).
- [`RETURNING`](https://sqlite.org/releaselog/3_35_0.html) — 3.35.0 (2021-03-12).
- [`RIGHT JOIN` / `FULL OUTER JOIN`](https://sqlite.org/releaselog/3_39_0.html) — 3.39.0 (2022-06-25).
- [`string_agg()` / `concat()`](https://sqlite.org/releaselog/3_44_0.html) — 3.44.0 (2023-11-01).

**SQL Server** — Microsoft Learn's "Applies to" notes:

- [`TRIM`](https://learn.microsoft.com/en-us/sql/t-sql/functions/trim-transact-sql) — 2017; its optional trim-characters argument — 2022.
- [`DATETRUNC`](https://learn.microsoft.com/en-us/sql/t-sql/functions/datetrunc-transact-sql), [`GREATEST`](https://learn.microsoft.com/en-us/sql/t-sql/functions/logical-functions-greatest-transact-sql), [`LEAST`](https://learn.microsoft.com/en-us/sql/t-sql/functions/logical-functions-least-transact-sql), and the [`LTRIM`](https://learn.microsoft.com/en-us/sql/t-sql/functions/ltrim-transact-sql) / [`RTRIM`](https://learn.microsoft.com/en-us/sql/t-sql/functions/rtrim-transact-sql) trim-characters argument — 2022 (16.x).
