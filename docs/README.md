# SqlArtisan Reference

[← Back to README](https://github.com/h-tacayama/SqlArtisan/blob/main/README.md)

The same type-safe C# emits idiomatic SQL for **MySQL, Oracle, PostgreSQL, SQLite, and SQL Server**.
Every entry below shows the C# you write and the exact SQL it produces. Per-call signatures and
emitted SQL are also visible inline via IntelliSense (bundled XML docs).

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
- **[UPDATE](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#update-statement)**
- **INSERT** —
  [Standard](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#standard-syntax) ·
  [Multiple Rows](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#multiple-rows) ·
  [SET-like](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#alternative-syntax-set-like) ·
  [INSERT … SELECT](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#insert-select-syntax) ·
  [UPSERT](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#upsert-insert-or-update) ·
  [MERGE](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#merge-statement)
- **[WITH / CTE](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#with-clause-common-table-expressions)**
- **RETURNING** —
  [RETURNING](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#returning-clause) ·
  [RETURNING INTO (Oracle)](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#returning-into-oracle)

## [Expressions](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md)

Values, predicates, and computed expressions.

[NULL](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#null-literal) ·
[Arithmetic](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#arithmetic-operators) ·
[Conditions](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#conditions) ·
[CASE](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#case-expressions) ·
[CAST](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#cast) ·
[Window Functions](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#window-functions) ·
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
