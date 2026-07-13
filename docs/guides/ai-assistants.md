# Guide: Using SqlArtisan with AI Coding Assistants

[← Back to README](https://github.com/h-tacayama/SqlArtisan/blob/main/README.md) ·
[Reference Home](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/README.md)

An AI assistant writing raw SQL strings produces *plausible* SQL — nothing
checks it until it hits the database. Writing SqlArtisan instead puts
deterministic checks between generation and production: column names and
statement structure are compile-checked, the analyzer flags wrong-dialect
constructs at build time, and exact-SQL tests freeze what was reviewed. Wrong
output becomes a red build instead of a runtime surprise.

This page is the setup that makes those checks bite.

## Contents

- [Feed the assistant the reference](#feed-the-assistant-the-reference)
- [Configure the analyzer target](#configure-the-analyzer-target)
- [Pin generated queries with exact-SQL tests](#pin-generated-queries-with-exact-sql-tests)
- [Rules snippet for your agent file](#rules-snippet-for-your-agent-file)

## Feed the assistant the reference

The repository ships an `llms.txt` index built for AI tools — a one-paragraph
model of the library plus links to every reference page, each entry paired
with the exact SQL it emits:

```
https://raw.githubusercontent.com/h-tacayama/SqlArtisan/main/llms.txt
```

Point your assistant at it (fetch it into context, or list it in your tool's
docs-sources configuration). The key facts it front-loads: SqlArtisan emits
the SQL you write with **no cross-dialect rewriting**, so the assistant must
pick the API for your target DBMS — and the reference tells it which call
emits what.

## Configure the analyzer target

The bundled Roslyn analyzer is the deterministic reviewer for dialect
correctness — the failure mode AI assistants hit most, because their training
data mixes all five dialects. Declare your engine once in `.editorconfig`:

```ini
[*.cs]
sqlartisan_target_dbms = postgresql   # mysql | oracle | postgresql | sqlite | sqlserver
```

From then on, a generated query that reaches for another dialect's construct
gets a build-time warning:

```csharp
// sqlartisan_target_dbms = mysql
var g = Rollup(t.Code, t.Name);
// warning SQLA0001: 'Rollup' is not supported on MySQL. ...
```

For an AI-heavy workflow, promote it to a build error so wrong-dialect code
cannot merge:

```ini
dotnet_diagnostic.SQLA0001.severity = error
```

The [analyzer reference](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/analyzer.md)
covers the rules, per-construct overrides, mixed-dialect projects, and CI
gates.

## Pin generated queries with exact-SQL tests

`Build()` is deterministic: the same chain always yields the same SQL text
and parameters. So after reviewing what an AI-written query emits, freeze it
in a unit test — the query's SQL is then part of the reviewed contract, and
any later edit (human or AI) that changes the emitted SQL fails the suite:

```csharp
[Fact]
public void OverdueOrdersQuery_CorrectSql()
{
    OrdersTable o = new("o");
    SqlStatement sql =
        Select(o.OrderId, o.CustomerId)
        .From(o)
        .Where(o.Status == "overdue")
        .OrderBy(o.OrderId)
        .Build(Dbms.PostgreSql);

    Assert.Equal(
        "SELECT \"o\".order_id, \"o\".customer_id FROM orders \"o\" "
        + "WHERE \"o\".status = :0 ORDER BY \"o\".order_id",
        sql.Text);
    Assert.Equal("overdue", sql.Parameters.Get<string>(":0"));
}
```

To see a query's SQL while reviewing, print `sql.Text` and `sql.Parameters`
from a scratch console — then paste what you approved into the assertion.
This repository pins its own
[cookbook](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/cookbook.md)
recipes the same way.

## Rules snippet for your agent file

Copy into your project's `CLAUDE.md`, `AGENTS.md`, or equivalent agent-rules
file, and set the dialect line to your engine:

```markdown
## SQL (SqlArtisan)

- Write database queries with SqlArtisan (`using static SqlArtisan.Sql;`),
  never as raw SQL strings.
- Target DBMS: PostgreSQL. SqlArtisan emits SQL faithfully with no dialect
  translation — always pick the API documented for this DBMS, and treat
  SQLA0001 analyzer warnings as errors to fix, not suppress.
- API reference (each call shown with its emitted SQL):
  https://raw.githubusercontent.com/h-tacayama/SqlArtisan/main/llms.txt
- Reference database tables through their table classes (one `DbTableBase`
  subclass per table); never name a table or column with a string literal.
- A builder chain is single-use — build a fresh chain per statement; never
  hold a partially built query for reuse.
- Every new or changed query gets an exact-SQL unit test asserting `sql.Text`
  and its bind parameters.
```

Why these lines: the dialect pin plus the analyzer turns the most common AI
failure (mixing dialects) into a build signal; the table-class rule keeps
generated queries inside the compile-checked surface; the single-use rule
matches the builder contract; and the test rule makes every generated query's
SQL reviewable — and regression-locked — in the diff.
