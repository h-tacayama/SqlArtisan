---
description: C# layout .editorconfig cannot express — wrapped lists, fluent chains, operators, blank lines
paths:
  - "src/**/*.cs"
  - "tests/**/*.cs"
---

# C# formatting beyond .editorconfig

`.editorconfig`'s `max_line_length` is editor-only, and `dotnet format` holds
indentation, braces, spacing, and blank-line runs (IDE2000) — but Roslyn's
formatter preserves the author's line breaks inside argument lists and chains
and has no wrapping options
(dotnet/roslyn#33872). The limit itself is gated (below); the *shape* of each
wrap is convention held at review time (`sa-diff-review`), since a mechanical
check for it would flag ~126 existing, mostly deliberate sites, so #504
recorded the decision to keep those prose.

## The 100-column limit

`LineLengthSweepTests` holds it at zero offenders — there is no baseline to
grow. Three shapes are exempt, and the gate reads these rules, not a count:

- **A doc tag's own signature.** A `<param>`, `<returns>`, `<exception>`,
  `<typeparam>` line, or an `<inheritdoc cref="...">` whose cref carries a full
  parameter list: the signature is one token and breaking it helps no one.
- **A row of a data table.** A collection-initializer entry (`["key"] = …,`)
  or a catalog's single registration call (`Add…(…);`) — the row is the unit
  of reading, and one entry per line is the point of the file.
- **Text inside a raw string literal** (`"""…"""`). It is a fixture — the SQL
  or C# under test — and wrapping it would change what is being tested.

Anything else wraps. A line that fits none of these and cannot be wrapped is a
name that needs shortening, not a new exemption.

## Wrapped argument and parameter lists

When a call's argument list or a declaration's parameter list does not fit
within the 100-column limit, put **one argument per line**, each indented one
level — no first argument hanging after the open paren, no packing two or
three to a line:

```csharp
return cnn.Execute(
    sql.Text,
    sql.Parameters.ToDynamicParameters(),
    transaction,
    commandTimeout,
    commandType);
```

A list that fits on one line stays on one line — only the shape of the wrap
is fixed, not whether to wrap.

Packing is deliberate — and stays — in exactly three shapes:

- **Semantic grouping** — lines that group related arguments on purpose,
  e.g. `CatalogColumnIndexReader.Read` splitting inputs from outputs:

  ```csharp
  ReadLeadingKeys(
      conn, tableName, LeadingKeyQuery(),
      leadingColumns, expressionTexts, partialLeadingColumns);
  ```

- **A short trailing argument after a long literal** — the literal forced
  the wrap and the tail is too slight to earn a line:
  `Assert.Equal("…long guard message…", ex.Message)`,
  `new(@"…regex…", RegexOptions.Compiled)`. Common in `tests/`.

- **A trailing wrapped call** — when the last argument is itself a call
  that wraps, it opens on the outer call's line and carries the wrap, as
  `SqlMapper.Async.cs` does on every verb:

  ```csharp
  => cnn.QuerySingleAsync(type, ToCommand(
      cnn,
      sqlBuilder,
      ...
  ```

A sync/async twin (`SqlMapper.cs` / `SqlMapper.Async.cs`) keeps its wrapped
argument lists in the same shape — the packed-vs-one-per-line divergence
#503 left between the two is what this rule exists to prevent. Body
structure may differ (the async file routes through a helper the sync file
has no use for).

## Wrapped fluent chains

When a builder/fluent chain wraps, put **one member per line**, the `.`
leading each continuation line:

```csharp
SqlStatement sql =
    Select(_t.Code)
    .From(_t)
    .Where(_t.Code == 1)
    .Build();
```

For `Format` chains over `SqlBuildingBuffer` (under
`src/SqlArtisan/Internal/**`), `sql-building-style.md` rule 4 is the
authority — it additionally lets a short statement chain in a block body
stay on one line.

## Wrapped operators

A condition or expression that wraps breaks **before** the operator, so
`&&` / `||` / `+` lead the continuation line.
(`dotnet_style_operator_placement_when_wrapping` tells editors the same;
the formatter cannot enforce it.)

## Blank lines

Blank-line *runs* are gated (IDE2000). The shapes the formatter cannot see:
no blank line immediately after `{` or before `}` (`FormattingSweepTests`
gates this over `src/` and `tests/`), and a single blank line
between members.
