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
| **Grammar-forced literal** — a position whose dialect grammar rejects a bind marker | emitted inline, single-quoted, `'` doubled (and `\` doubled on MySQL) | `SqlBuildingBuffer.AppendStringLiteral` |
| **Identifier** — a name or type token in the statement's structure | emitted verbatim, between the dialect's alias quotes where the position is quoted at all | `SqlBuildingBuffer.EncloseInAliasQuotes`, or a bare append |

`Nextval` / `Currval` are the case that looked like an exception and is not.
PostgreSQL's grammar takes the sequence name as a `'…'` string literal (cast to
`regclass`), so `NEXTVAL('s')` lands on the *second* row — the same code path as
`LIKE … ESCAPE`, `GROUP_CONCAT … SEPARATOR`, a JSON path, and a text-search
configuration. The identifier spellings of the same concept, Oracle's
`s.NEXTVAL` and SQL Server's `NEXT VALUE FOR s`, land on the third row and are
emitted verbatim. Nothing there is sequence-name hardening.

## Decision

**Only values and grammar-forced literals are protected. Identifier positions
carry author-written tokens and are emitted verbatim.**

The library adds no escaping, quoting, or sanitizing to an identifier position.
Rewriting one would be exactly the silent rewriting ADR 0001 rejects: an alias
the author spelled with a quote in it is a token they chose, and a `CAST` type
of `DECIMAL(10,2)` is not a name to be quoted at all.

Escaping identifiers "uniformly" is also unachievable, which is what settles the
question rather than merely arguing it. Only the alias-quoted positions could
double a quote character; the bare ones — the table name (which may be
schema-qualified), the column name, the `CAST` target type, a CTE column list,
an `OUTPUT … INTO` variable, and the `Sql.Hints` raw-SQL escape hatch — have no
delimiter to escape *into*. The outcome would be half-escaped, which is worse
than a stated contract because it invites the belief that the other half is
covered too.

The security scope that follows is stated on the user-facing surfaces in plain
words: `SECURITY.md` (what counts as a vulnerability), the README's feature
line (injection is prevented **through values**), and `docs/functions.md`
alongside the bind-parameter types.

## Consequences

- **The guarantee is scoped, and the scope is now written down.** A value can
  never reach the SQL text; a grammar-forced literal is always escaped. Those
  two are the library's security surface, and a bypass of either is a
  vulnerability. An identifier is not covered.
- **Building an identifier from untrusted input is an application concern.**
  The library ships no sanitizer for it and will not grow one — reaching these
  positions requires the application to derive an alias, type, or table name
  from request data, which is both unusual and the application's decision to
  make. An allowlist at that boundary is the fix, not a rewrite here.
- **`Nextval` / `Currval` stay as they are.** They are not an outlier to remove;
  removing their escaping would emit a broken PostgreSQL string literal.
- **New API surface is classified before it is written.** A new function that
  takes a `string` decides which of the three rows it is on, and the answer
  follows from the grammar of the position — not from whether the string
  happens to name something.
- **The verbatim contract is now defended by tests**, which it was not: no test
  anywhere asserted identifier behavior with a quote character, so a future
  change could have silently started escaping one. `Cast_TypeWithQuote_…`,
  `Select_ColumnAliasWithDoubleQuote_…`, and
  `NextValueFor_SequenceNameWithQuote_…` pin it beside the existing
  `Nextval_SequenceNameWithQuote_EscapesLiteral`.
- **Not a Boundary-cluster ADR.** This records emission behavior; it adds no
  rejection category to ADR 0007 / 0011 / 0012 and does not count toward their
  consolidation trigger.
- Complements ADR 0001 and ADR 0004; supersedes neither. See #375.
