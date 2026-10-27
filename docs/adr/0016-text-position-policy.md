# ADR 0016 — Text positions: what is bound, what is escaped, and what is emitted verbatim

**Status:** Accepted

## Context

ADR 0004 says values are bound as parameters, and notes that a few positions
*require* a literal and so "emit a literal by design". ADR 0001 says the library
emits what the author wrote. Neither says what happens to a caller string that
reaches an **identifier** position — an alias, a table or column name, a `CAST`
target type, a sequence name — and the audit behind #375 found the resulting
behavior read as inconsistent:

```
SELECT id "x" ; DROP TABLE t --", CAST(id AS INT) ; DROP TABLE t --),
       NEXTVAL('s''); DROP TABLE t --') FROM users; DROP TABLE t --
```

The alias and the cast type interpolate the quote character raw; the sequence
name has its quote doubled. Two identifier-ish positions appeared to escape and
three did not, with nothing recording which was intended.

A sweep of every site that writes a caller string into the SQL text shows the
behavior is in fact uniform — the rule was simply never written down. Escaping
tracks the **grammatical position** the string lands in, not the kind of thing
the string names:

| Position | Behavior | Mechanism |
|----------|----------|-----------|
| **Value** — anything the grammar accepts as a parameter | bound | `BindValue`; ADR 0004 |
| **Forced literal** — a position that needs a literal: the dialect grammar rejects a bind marker there, or a bind marker would change how the engine resolves or plans the statement | emitted inline, single-quoted, `'` doubled (`\` doubled on MySQL; on PostgreSQL a literal holding `\` is an `E'...'` constant with `\` doubled; on SQL Server a literal holding a non-ASCII character is `N'...'`, the `STRING_AGG` separator excepted, and a backslash before a line break gets a spare backslash and line break ahead of the break) | `SqlBuildingBuffer.AppendStringLiteral`; `AppendCodePageStringLiteral` for the `STRING_AGG` separator |
| **Identifier** — a name or type token in the statement's structure | emitted verbatim, between the dialect's alias quotes where the position is quoted at all | `SqlBuildingBuffer.EncloseInAliasQuotes`, or a bare append |

`Nextval` / `Currval` are the case that looked like an exception and is not.
PostgreSQL takes the sequence name as a `regclass` argument, and a `'…'`
literal there is resolved to the sequence when the statement is parsed (early
binding); a bound name would be looked up at run time instead. So
`NEXTVAL('s')` lands on the *second* row — the same code path as `LIKE …
ESCAPE` and `GROUP_CONCAT … SEPARATOR`. The identifier spellings of the same
concept, Oracle's `s.NEXTVAL` and SQL Server's `NEXT VALUE FOR s`, land on the
third row and are emitted verbatim. Nothing there is sequence-name hardening.

Three more entries on the second row are there for reasons of their own, not
because every dialect's grammar rejects a marker (#557):

- **A JSON function path.** An expression index over
  `json_extract(doc, '$.a')` matches only the literal path: on SQLite 3.45.1 the
  inline path plans `SEARCH … USING INDEX`, a bound one `SCAN` (#555). Oracle's
  `JSON_VALUE` / `JSON_QUERY` also require the literal in their grammar; SQL
  Server did only before 2017.
- **A `->` / `->>` key given as a C# `string`.** The same index matching holds
  for `doc ->> '$.a'`, and MySQL's grammar takes only a string literal on the
  right of the operator. An `int` array index is inlined too, as a bare number;
  a key given as a `SqlExpression` (a column, or `Bind(...)` to bind it) is
  emitted as that expression (#557).
- **A PostgreSQL text-search configuration.** Inline, it resolves as a
  `regconfig` constant; a string sent as a `text` parameter has no implicit
  cast to `regconfig`.

**What bounds the planning reason (#661).** A plan difference alone moves a
position onto the second row when the position is typed `string` from its
introduction, as the JSON path was. Moving a position already typed `object`
breaks every caller passing an `object`-typed argument there, even with a
`SqlExpression` overload kept for a column or `Bind(...)`, so it takes more
than a difference measured on one engine: the `->` / `->>` key made that move
because MySQL's grammar takes only a literal there too (#557). The format
arguments of `Strftime`, `DateFormat`, `ToChar`, `ToDate`, `ToNumber`,
`ToTimestamp`, `Format` and `Trunc` (Oracle's date-format unit), and the
modifiers of `Date`, `Datetime`, `Julianday` and `Strftime`, stay on the first
row on that weighing:

- The matrix sweep runs the format arguments other than `Trunc`'s, and the
  `Date` and `Datetime` modifiers, bound on their lanes, and no lane rejects
  the marker. The MySQL lane fills a marker in on the client, so MySQL 8.0's
  acceptance of `DATE_FORMAT(d, ?)` is shown by a server-side prepare instead
  (`MySqlTests.DateFormat_BoundFormat_RunsServerPrepared`), the check that
  rejects the bound `->>` key. `Trunc`'s format, which the matrix leaves out,
  and the `Julianday` and `Strftime` modifiers are run bound on no lane.
- SQLite 3.50.4, the lane's engine, plans `SEARCH … USING … INDEX` over an
  index on `strftime('%Y', d)` or `date(d, 'start of month')` only with the
  literal, and `SCAN` with the bound format or modifier
  (`SqliteTests.DateTimeArgument_ExpressionIndex_MatchesOnlyALiteral`).
- PostgreSQL 16.15 holds no index over `to_char`, `to_date`, `to_number` or
  `to_timestamp(text, text)`: each is `STABLE`, and `CREATE INDEX` refuses it
  (`PostgreSqlTests.ToCharFamily_ExpressionIndex_IsRejectedByTheEngine`), so
  binding costs it nothing.
- MySQL's, Oracle's and SQL Server's index matching over these functions is
  not measured.

The gain is one engine's, so the breaking change is not taken. A measurement
on another engine, or a caller whose query needs the index, reopens it.

## Decision

**Only values and forced literals are protected. Identifier positions
carry author-written tokens and are emitted verbatim.**

The library adds no escaping, quoting, or sanitizing to an identifier position.
Rewriting one would be exactly the silent rewriting ADR 0001 rejects: an alias
the author spelled with a quote in it is a token they chose, and a `CAST` type
of `DECIMAL(10,2)` is not a name to be quoted at all.

Escaping identifiers "uniformly" is also unachievable, which is what settles the
question rather than merely arguing it. Only the alias-quoted positions could
double a quote character: a table's alias, a select item's `As("x")` alias, a
scalar subquery's `As` alias, a CTE's name, and a derived table's name (a
`DerivedTableBase`: `AsTable(...)` on a subquery, `Sql.Values(...)`,
`Unnest(...).AsTable(...)` or a `DerivedTable`). The bare ones — the table name
(which may be schema-qualified), a column name given as a string
(`Column(name)`, `new DbColumn(owner, name)`), the `CAST` target type, the
Oracle and SQL Server sequence name, the column list of a `Sql.Values(...)` or
`Unnest(...).AsTable(...)` derived table (#679), the `RETURNING … INTO` output
variable (whose name ADR 0011 restricts to an Oracle nonquoted identifier's
shape, #679), and the `Sql.Hints` raw-SQL escape hatch — have no delimiter to
escape *into*. The outcome would be half-escaped, which is worse than a stated
contract because it invites the belief that the other half is covered too. A
name that reads another definition — a CTE column-list entry, a reference
through `Column(alias)` or `Column(column)`, an `As(column)` alias — renders as
that definition does: alias-quoted when it comes from `As("name")`, bare
otherwise (#165).

The security scope that follows is stated on the user-facing surfaces in plain
words: `SECURITY.md` (what counts as a vulnerability), the README's feature
line (injection is prevented **through values**), and `docs/functions.md`
alongside the bind-parameter types.

## Consequences

- **The guarantee is scoped, and the scope is now written down.** A caller
  string is bound, or — where the grammar demands a literal — inlined with its
  quotes escaped; either way it never reaches the SQL text raw. The slots that
  do inline a value unescaped — an `ORDER BY` ordinal, a `LAG` / `LEAD` offset,
  a window-frame bound, and the other numeric arguments the grammar fixes — are
  `int`- or `double`-typed at the factory, or verified numeric first, so none
  can carry a string. That is the library's security surface, and a bypass of
  it is a vulnerability. An identifier is not covered.
- **Building an identifier from untrusted input is an application concern.**
  The library ships no sanitizer for it and will not grow one — reaching these
  positions requires the application to derive an alias, type, or table name
  from request data, which is both unusual and the application's decision to
  make. An allowlist at that boundary is the fix, not a rewrite here.
- **`Nextval` / `Currval` stay as they are.** They are not an outlier to remove;
  removing their escaping would emit a broken PostgreSQL string literal.
- **New API surface is classified before it is written.** A new function that
  takes a `string` decides which of the three rows it is on, and the answer
  follows from the grammar of the position, or from a measured plan difference
  within the bound above — not from whether the string happens to name
  something.
- **The verbatim contract is now defended by tests**, which it was not: no test
  anywhere asserted identifier behavior with a quote character, so a future
  change could have silently started escaping one. `Cast_TypeWithQuote_…`,
  `Select_ColumnAliasWithDoubleQuote_…`, and
  `NextValueFor_SequenceNameWithQuote_…` pin it beside the existing
  `Nextval_SequenceNameWithQuote_EscapesLiteral`.
- **No session setting reopens a forced literal.** PostgreSQL reads a plain
  literal's `\` per `standard_conforming_strings`, which any role, database or
  connection can turn off; with it off, `\'` is an escaped quote and would close
  the literal. A literal holding `\` is therefore an `E'...'` constant there,
  which reads the same under either setting. MySQL's `NO_BACKSLASH_ESCAPES` can
  still change a doubled `\` into two characters, a wrong value but never a
  closed literal, since `'` stays doubled.
- **A forced literal keeps its characters on SQL Server (#663).** SQL Server
  converts a literal without `N` to the database's code page and reports no
  loss: under SQL Server 2022's `SQL_Latin1_General_CP1_CI_AS` a `JSON_VALUE` /
  `JSON_QUERY` path holding a character the page lacks (`名前`) names another
  key, which lax mode answers with NULL, and such an `ESCAPE` character (`＼`)
  escapes nothing. So there a
  forced literal holding a non-ASCII character is written `N'...'`; an ASCII
  one stays plain, as every code page reads it alike. Where the page holds the
  character, the `N` form returns the plain form's rows and keeps the index on
  a `JSON_VALUE` computed column (`Japanese_CI_AS`). The
  `STRING_AGG` separator stays plain: SQL Server rejects an `nvarchar`
  separator over a `varchar` input even where the page holds the character, so
  `N'...'` would break a query that runs today (#664), and a non-ASCII
  separator the page lacks arrives as `?`. No other dialect takes the prefix.
  Twins:
  `SqlServerTests.NonAsciiForcedLiteral_PlainForm_LosesTheCharacter`,
  `SqlServerTests.NonAsciiForcedLiteral_OnACodePageHoldingIt_KeepsWhatThePlainFormRan`.
- **A backslash before a line break keeps both on SQL Server (#679).** T-SQL
  reads `\` followed by LF or CRLF inside a string constant as a line
  continuation and drops both, in a plain or an `N'...'` literal, and before a
  lone CR it drops the backslash alone. A forced literal holding the pair lost
  it: a `JSON_VALUE` / `JSON_QUERY` path named another key, and a `STRING_AGG`
  separator lost its line break. The continuation is read in one pass, so a
  spare backslash and line break written ahead of the caller's (the same CRLF
  before a CRLF, an LF before an LF or a lone CR) are what the engine drops,
  and the literal reads as written; `BackslashNewlineContinuesStringLiterals`
  holds that spelling to SQL Server. All of it holds on SQL Server 2022 and
  2025. Twins:
  `SqlServerTests.InlineLiteral_BackslashNewline_ContinuesTheLine`,
  `SqlServerTests.InlineLiteral_DoubledBackslashNewline_KeepsThePair`,
  `SqlServerTests.StringAgg_SeparatorWithBackslashNewline_KeepsThePair`, and
  the first two on `SqlServer2025Tests`.
- **Not a Boundary-cluster ADR.** This records emission behavior; it adds no
  rejection category to ADR 0007 / 0011 / 0012 and does not count toward their
  consolidation trigger.
- Complements ADR 0001 and ADR 0004; supersedes neither. See #375.
