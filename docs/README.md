# SqlArtisan Reference

[← Back to README](https://github.com/h-tacayama/SqlArtisan/blob/main/README.md)

SqlArtisan is a deterministic guard rail for SQL written alongside AI — the same type-safe C# emits idiomatic SQL for **MySQL, Oracle, PostgreSQL, SQLite, and SQL Server**.
Every entry below shows the C# you write and the exact SQL it produces. Per-call signatures and
emitted SQL are also visible inline via IntelliSense (bundled XML docs).

## Guides

Task-oriented paths from your current project to SqlArtisan.

- **[Comparison Guide](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/comparison.md)** —
  how SqlArtisan compares to EF Core, linq2db, SqlKata, Dapper.SqlBuilder,
  and InterpolatedSql — and when NOT to choose SqlArtisan.
- **[Dapper + SqlArtisan from Scratch](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/guides/dapper-quickstart.md)** —
  the greenfield path: packages → table classes → executed queries.
- **[Using SqlArtisan with AI Coding Assistants](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/guides/ai-assistants.md)** —
  the analyzer target, the `llms.txt` feed, and exact-SQL test pinning for
  AI-written queries.

## [Query Cookbook](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/cookbook.md)

Realistic end-to-end queries — the C# and the SQL it emits, every recipe
pinned by an exact-SQL unit test.

[The Recipe Schema](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/cookbook.md#the-recipe-schema) ·
[Everyday Patterns](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/cookbook.md#everyday-patterns) ·
[Reporting Queries](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/cookbook.md#reporting-queries) ·
[Dynamic Search Screens](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/cookbook.md#dynamic-search-screens) ·
[Batch and UPSERT DML](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/cookbook.md#batch-and-upsert-dml)

## [Query Statements](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md)

How to assemble each statement and clause.

- **SELECT** —
  [SELECT](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#select-clause) ·
  [FROM](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#from-clause) ·
  [WHERE](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#where-clause) ·
  [JOIN](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#join-clause) ·
  [ORDER BY](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#order-by-clause) ·
  [GROUP BY / HAVING](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#group-by-and-having-clause) ·
  [Set Operators](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#set-operators) ·
  [FOR UPDATE](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#for-update-clause) ·
  [Pagination](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#pagination)
- **[DELETE](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#delete-statement)**
- **[UPDATE](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#update-statement)** —
  [Correlated UPDATE / DELETE](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#correlated-update--delete) ·
  [Joined UPDATE / DELETE](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#joined-update--delete)
- **INSERT** —
  [Standard](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#standard-syntax) ·
  [Multiple Rows](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#multiple-rows) ·
  [SET-like](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#alternative-syntax-set-like) ·
  [INSERT … SELECT](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#insert-select-syntax) ·
  [UPSERT](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#upsert-insert-update-or-skip) ·
  [MERGE](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#merge-statement)
- **[WITH / CTE](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#with-clause-common-table-expressions)**
- **RETURNING** —
  [RETURNING](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#returning-clause) ·
  [RETURNING INTO (Oracle)](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#returning-into-oracle)
- **OUTPUT (SQL Server)** —
  [OUTPUT](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#output-clause-sql-server) ·
  [OUTPUT … INTO](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#output--into)

## [Expressions](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md)

Values, predicates, and computed expressions.

[NULL](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#null-literal) ·
[Arithmetic](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#arithmetic-operators) ·
[String Concatenation](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#string-concatenation) ·
[Conditions](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#conditions) ·
[JSON Operators](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#json-operators) ·
[Array Operators](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#array-operators) ·
[Full-Text Search](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#full-text-search) ·
[Scalar Subquery](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#scalar-subquery) ·
[ALL / ANY / SOME](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#all--any--some) ·
[CASE](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#case-expressions) ·
[CAST](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#cast) ·
[Window Functions](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#window-functions) ·
[Conditional Aggregation](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#conditional-aggregation-filter) ·
[String Aggregation](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#string-aggregation) ·
[Sequence](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#sequence)

## [Functions](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/functions.md)

Built-in function catalog, grouped by family, plus bind-parameter types.

[Numeric](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/functions.md#numeric-functions) ·
[Character](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/functions.md#character-functions) ·
[Date & Time](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/functions.md#date-and-time-functions) ·
[Conversion](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/functions.md#conversion-functions) ·
[Comparison](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/functions.md#comparison-functions) ·
[JSON](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/functions.md#json-functions) ·
[Full-Text Search](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/functions.md#full-text-search-functions) ·
[Aggregate](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/functions.md#aggregate-functions) ·
[String Aggregation](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/functions.md#string-aggregation-functions) ·
[Window / Analytic](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/functions.md#window-functions) ·
[Bind Parameter Types](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/functions.md#bind-parameter-types)

## [Analyzer](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/analyzer.md)

An opt-in Roslyn analyzer that warns when a construct is used against a target
dialect it doesn't support.

[Enabling It](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/analyzer.md#enabling-it) ·
[Rules](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/analyzer.md#rules) ·
[Correcting a Warning](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/analyzer.md#correcting-a-warning-the-override-keys) ·
[Version-Aware Warnings (SQLA0003)](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/analyzer.md#version-aware-warnings-sqla0003) ·
[Context Rules (SQLA0004)](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/analyzer.md#context-rules-sqla0004) ·
[Correlated DML Target (SQLA0005)](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/analyzer.md#correlated-dml-target-sqla0005) ·
[Mixed-Dialect Projects](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/analyzer.md#mixed-dialect-projects) ·
[CI Gates](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/analyzer.md#ci-gates-and-stricter-enforcement) ·
[Verified-Against Versions](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/analyzer.md#verified-against-versions) ·
[Known Limitations](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/analyzer.md#known-limitations)
