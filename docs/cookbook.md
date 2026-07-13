# Query Cookbook

[← Back to README](https://github.com/h-tacayama/SqlArtisan/blob/main/README.md) ·
[Reference Home](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/README.md)

Realistic end-to-end queries: the reference pages answer "what does X emit",
this page answers "how do I write the whole thing". Every recipe shows the C#
you write and the SQL it emits, and every emitted-SQL comment on this page is
pinned by an exact-SQL unit test (`tests/SqlArtisan.Tests/CookbookTests.cs`),
so the examples cannot drift from the code.

> **How to read this page.** The SQL in comments is line-wrapped for
> readability; `sql.Text` is a single line carrying the same tokens in the same
> order. `Build()` with no argument targets the default dialect (PostgreSQL);
> recipes that target another DBMS pass it explicitly. Pick the API for your
> target DBMS — a single call is not rewritten per dialect.

## Contents

- [The Recipe Schema](#the-recipe-schema)
- [Everyday Patterns](#everyday-patterns)
  - [Paginated list with reusable query](#paginated-list-with-reusable-query) ·
    [Filtered aggregate report](#filtered-aggregate-report) ·
    [Custom sort with CASE](#custom-sort-with-case)
- [Reporting Queries](#reporting-queries)
  - [Top-N per group](#top-n-per-group) ·
    [Pivoting rows to columns](#pivoting-rows-to-columns) ·
    [Rows without a match](#rows-without-a-match)
- [Dynamic Search Screens](#dynamic-search-screens)
  - [Optional filters, runtime sort, shared COUNT](#optional-filters-runtime-sort-shared-count)
- [Batch and UPSERT DML](#batch-and-upsert-dml)
  - [UPSERT on every dialect](#upsert-on-every-dialect) ·
    [Synchronizing a table from staging](#synchronizing-a-table-from-staging)

---

## The Recipe Schema

The recipes share a small web-shop schema. Each table follows the standard
[table class](https://github.com/h-tacayama/SqlArtisan/blob/main/README.md#quick-start)
pattern — shown once here for `customer` and `orders`, the rest
(`category`, `product`, `order_item`, `staging_product`) differ only
in their columns:

```csharp
internal sealed class Customer : DbTableBase
{
    public Customer(string alias = "") : base("customer", alias)
    {
        CustomerId = new DbColumn(this, "customer_id");
        FirstName = new DbColumn(this, "first_name");
        LastName = new DbColumn(this, "last_name");
        Email = new DbColumn(this, "email");
        Region = new DbColumn(this, "region");
        CreatedAt = new DbColumn(this, "created_at");
    }

    public DbColumn CustomerId { get; }
    public DbColumn FirstName { get; }
    public DbColumn LastName { get; }
    public DbColumn Email { get; }
    public DbColumn Region { get; }
    public DbColumn CreatedAt { get; }
}

internal sealed class Order : DbTableBase
{
    public Order(string alias = "") : base("orders", alias)
    {
        OrderId = new DbColumn(this, "order_id");
        CustomerId = new DbColumn(this, "customer_id");
        OrderedAt = new DbColumn(this, "ordered_at");
        Status = new DbColumn(this, "status");
        TotalAmount = new DbColumn(this, "total_amount");
    }

    public DbColumn OrderId { get; }
    public DbColumn CustomerId { get; }
    public DbColumn OrderedAt { get; }
    public DbColumn Status { get; }
    public DbColumn TotalAmount { get; }
}
```

---

## Everyday Patterns

### Paginated list with reusable query

A builder chain is
[single-use](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#reusing-a-builder-chain)
— once `Build()` runs, that instance is finished. Wrap the chain in a local
function parameterized by the parts that change; each call builds a fresh chain:

```csharp
Customer c = new("c");
Order o = new("o");

SqlStatement Page(int offset) =>
    Select(c.CustomerId, c.FirstName, c.LastName, o.OrderId, o.TotalAmount)
    .From(c)
    .InnerJoin(o).On(c.CustomerId == o.CustomerId)
    .Where(o.Status == "shipped")
    .OrderBy(o.OrderedAt.Desc, o.OrderId)
    .Limit(20).Offset(offset)
    .Build();

SqlStatement page1 = Page(0);
SqlStatement page2 = Page(20);

// Both emit:
// SELECT "c".customer_id, "c".first_name, "c".last_name,
//   "o".order_id, "o".total_amount
// FROM customer "c"
// INNER JOIN orders "o" ON "c".customer_id = "o".customer_id
// WHERE "o".status = :0
// ORDER BY "o".ordered_at DESC, "o".order_id LIMIT :1 OFFSET :2
//   page1: :2 = 0    page2: :2 = 20
```

On Oracle and SQL Server, replace `.Limit(20).Offset(offset)` with
`.OffsetRows(offset).FetchNext(20)`.

### Filtered aggregate report

A category sales report where both the row filter and the active-product filter
are optional at runtime. `ConditionIf` composes naturally with `JOIN`,
`GROUP BY`, and `HAVING` — when a filter is off, that predicate drops out of the
`WHERE` clause and the rest of the query stays intact:

```csharp
Category c = new("c");
Product p = new("p");
OrderItem oi = new("oi");

var where =
    ConditionIf(filterByCategory, c.Name == "Electronics")
    & ConditionIf(filterByActive, p.Active == true);

SqlStatement sql =
    Select(
        c.Name.As("category"),
        Count(Distinct, p.ProductId).As("products"),
        Sum(oi.Quantity * oi.UnitPrice).As("revenue"))
    .From(c)
    .InnerJoin(p).On(c.CategoryId == p.CategoryId)
    .InnerJoin(oi).On(p.ProductId == oi.ProductId)
    .Where(where)
    .GroupBy(c.Name)
    .Having(Sum(oi.Quantity * oi.UnitPrice) > 1000)
    .OrderBy(Sum(oi.Quantity * oi.UnitPrice).Desc)
    .Build();

// SELECT "c".name "category",
//   COUNT(DISTINCT "p".product_id) "products",
//   SUM(("oi".quantity * "oi".unit_price)) "revenue"
// FROM category "c"
// INNER JOIN product "p" ON "c".category_id = "p".category_id
// INNER JOIN order_item "oi" ON "p".product_id = "oi".product_id
// WHERE ("c".name = :0) AND ("p".active = :1)
// GROUP BY "c".name
// HAVING SUM(("oi".quantity * "oi".unit_price)) > :2
// ORDER BY SUM(("oi".quantity * "oi".unit_price)) DESC
```

### Custom sort with CASE

A status priority sort — `CASE` in `ORDER BY` maps each value to a numeric
rank, so rows sort in business order rather than alphabetically. Every literal
in the `CASE` becomes a bind parameter:

```csharp
Order o = new("o");

SqlStatement sql =
    Select(o.OrderId, o.CustomerId, o.Status, o.TotalAmount)
    .From(o)
    .OrderBy(
        Case(
            When(o.Status == "pending").Then(1),
            When(o.Status == "shipped").Then(2),
            When(o.Status == "cancelled").Then(3),
            Else(4)),
        o.OrderId)
    .Build();

// SELECT "o".order_id, "o".customer_id, "o".status, "o".total_amount
// FROM orders "o"
// ORDER BY CASE WHEN ("o".status = :0) THEN :1
//   WHEN ("o".status = :2) THEN :3
//   WHEN ("o".status = :4) THEN :5 ELSE :6 END, "o".order_id
```

---

## Reporting Queries

### Top-N per group

The three best-selling products per category: rank inside a CTE with
`ROW_NUMBER()` partitioned by group, then filter on the rank outside. The
window's `ORDER BY` takes the aggregate directly.

```csharp
Category c = new("c");
Product p = new("p");
OrderItem oi = new("oi");
Cte ranked = new("ranked");

SqlStatement sql =
    With(ranked.As(
        Select(
            c.Name.As(ranked.Column("category_name")),
            p.Name.As(ranked.Column("product_name")),
            Sum(oi.Quantity * oi.UnitPrice).As(ranked.Column("sales")),
            RowNumber()
                .Over(PartitionBy(c.Name).OrderBy(Sum(oi.Quantity * oi.UnitPrice).Desc))
                .As(ranked.Column("rn")))
        .From(c)
        .InnerJoin(p).On(c.CategoryId == p.CategoryId)
        .InnerJoin(oi).On(p.ProductId == oi.ProductId)
        .GroupBy(c.Name, p.Name)))
    .Select(
        ranked.Column("category_name"),
        ranked.Column("product_name"),
        ranked.Column("sales"))
    .From(ranked)
    .Where(ranked.Column("rn") <= 3)
    .OrderBy(ranked.Column("category_name"), ranked.Column("sales").Desc)
    .Build();

// WITH "ranked" AS (SELECT "c".name category_name, "p".name product_name,
//   SUM(("oi".quantity * "oi".unit_price)) sales,
//   ROW_NUMBER() OVER (PARTITION BY "c".name
//     ORDER BY SUM(("oi".quantity * "oi".unit_price)) DESC) rn
//   FROM category "c"
//   INNER JOIN product "p" ON "c".category_id = "p".category_id
//   INNER JOIN order_item "oi" ON "p".product_id = "oi".product_id
//   GROUP BY "c".name, "p".name)
// SELECT "ranked".category_name, "ranked".product_name, "ranked".sales
// FROM "ranked" WHERE "ranked".rn <= :0
// ORDER BY "ranked".category_name, "ranked".sales DESC
```

This shape runs on all five dialects (only markers and quoting differ). On
MySQL, Oracle (12c+), PostgreSQL, and SQL Server, a
[row-limited `LATERAL` / `APPLY` subquery](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#row-limited-queries-as-subqueries)
is the per-group top-N alternative.

### Pivoting rows to columns

One column per status, one row per customer — conditional aggregation
(`SUM(CASE ...)`) is the pivot spelling that runs on all five dialects. Each
`THEN` / `ELSE` literal becomes a bind parameter, like every other literal.

```csharp
Order o = new("o");
SqlStatement sql =
    Select(
        o.CustomerId,
        Sum(Case(When(o.Status == "shipped").Then(1), Else(0))).As("shipped_cnt"),
        Sum(Case(When(o.Status == "cancelled").Then(1), Else(0))).As("cancelled_cnt"),
        Count(Asterisk).As("total"))
    .From(o)
    .GroupBy(o.CustomerId)
    .Build();

// SELECT "o".customer_id,
//   SUM(CASE WHEN ("o".status = :0) THEN :1 ELSE :2 END) "shipped_cnt",
//   SUM(CASE WHEN ("o".status = :3) THEN :4 ELSE :5 END) "cancelled_cnt",
//   COUNT(*) "total"
// FROM orders "o" GROUP BY "o".customer_id
```

On PostgreSQL and SQLite, the `FILTER` clause says the same thing more
directly:

```csharp
Select(
    o.CustomerId,
    Count(Asterisk).Filter(o.Status == "shipped").As("shipped_cnt"),
    Count(Asterisk).As("total"))
.From(o)
.GroupBy(o.CustomerId)
.Build();

// SELECT "o".customer_id,
//   COUNT(*) FILTER (WHERE "o".status = :0) "shipped_cnt", COUNT(*) "total"
// FROM orders "o" GROUP BY "o".customer_id
```

### Rows without a match

Products never ordered, in the three standard anti-join spellings — all
equally direct; pick by taste and plan:

```csharp
Product p = new("p");
OrderItem oi = new("oi");

// (i) NOT EXISTS
Select(p.Name).From(p)
    .Where(NotExists(
        Select(oi.OrderId).From(oi).Where(oi.ProductId == p.ProductId)))
    .Build();
// SELECT "p".name FROM product "p" WHERE NOT EXISTS (
//   SELECT "oi".order_id FROM order_item "oi"
//   WHERE "oi".product_id = "p".product_id)

// (ii) LEFT JOIN ... IS NULL
Select(p.Name).From(p)
    .LeftJoin(oi).On(p.ProductId == oi.ProductId)
    .Where(oi.OrderId.IsNull)
    .Build();
// SELECT "p".name FROM product "p"
// LEFT JOIN order_item "oi" ON "p".product_id = "oi".product_id
// WHERE "oi".order_id IS NULL

// (iii) NOT IN (subquery)
Select(p.Name).From(p)
    .Where(p.ProductId.NotIn(Select(oi.ProductId).From(oi)))
    .Build();
// SELECT "p".name FROM product "p" WHERE "p".product_id NOT IN (
//   SELECT "oi".product_id FROM order_item "oi")
```

Mind the SQL semantics of (iii): if the subquery returns any `NULL`,
`NOT IN` matches nothing — prefer (i) or (ii) when the subquery column is
nullable.

---

## Dynamic Search Screens

### Optional filters, runtime sort, shared COUNT

The standard admin search: each predicate applies only when its filter is
active (`ConditionIf`), the sort column and direction come from user input,
and the page query shares its condition with a companion `COUNT(*)`.

```csharp
Customer c = new("c");
var where =
    ConditionIf(useName, c.LastName.Like("%son%"))
    & ConditionIf(useRegion, c.Region == "west")
    & ConditionIf(useDateRange, c.CreatedAt.Between(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31)));

DbColumn sortCol = sortKey switch
{
    "last_name" => c.LastName,
    "created_at" => c.CreatedAt,
    _ => c.CustomerId,
};
object orderItem = sortDesc ? sortCol.Desc : sortCol;

SqlStatement page =
    Select(c.CustomerId, c.FirstName, c.LastName, c.Email)
    .From(c)
    .Where(where)
    .OrderBy(orderItem, c.CustomerId)
    .Limit(20).Offset(40)
    .Build();

// SELECT "c".customer_id, "c".first_name, "c".last_name, "c".email
// FROM customer "c"
// WHERE ("c".last_name LIKE :0) AND ("c".region = :1)
//   AND ("c".created_at BETWEEN :2 AND :3)
// ORDER BY "c".last_name DESC, "c".customer_id LIMIT :4 OFFSET :5

SqlStatement count =
    Select(Count(Asterisk))
    .From(c)
    .Where(where)                              // the same condition object, reused
    .Build();

// SELECT COUNT(*) FROM customer "c"
// WHERE ("c".last_name LIKE :0) AND ("c".region = :1)
//   AND ("c".created_at BETWEEN :2 AND :3)
```

When *every* `ConditionIf` is excluded, `Build()` throws rather than run an
unintended full-table read — see
[Dynamic Condition](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/expressions.md#dynamic-condition).
For a screen where all filters can legitimately be off, omit the `Where(...)`
clause entirely — build separate chains with and without it, keyed on whether
any filter survived.

---

## Batch and UPSERT DML

### UPSERT on every dialect

Insert-or-update and insert-or-skip, in each dialect's native spelling — see
[UPSERT](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#upsert-insert-update-or-skip)
for the full reference:

```csharp
Product u = new();

// PostgreSQL / SQLite — ON CONFLICT ... DO UPDATE:
InsertInto(u, u.ProductId, u.Price)
    .Values(1, 990)
    .OnConflict(u.ProductId)
    .DoUpdateSet(u.Price == Excluded(u.Price))
    .Build();
// INSERT INTO product (product_id, price) VALUES (:0, :1)
// ON CONFLICT (product_id) DO UPDATE SET price = EXCLUDED.price

// UPSERT + RETURNING (PostgreSQL / SQLite) — read back the affected row:
InsertInto(u, u.ProductId, u.Price)
    .Values(1, 990)
    .OnConflict(u.ProductId)
    .DoUpdateSet(u.Price == Excluded(u.Price))
    .Returning(u.ProductId, u.Price)
    .Build();
// INSERT INTO product (product_id, price) VALUES (:0, :1)
// ON CONFLICT (product_id) DO UPDATE SET price = EXCLUDED.price
// RETURNING product_id, price

// MySQL — ON DUPLICATE KEY UPDATE (8.0.19+ row-alias form):
InsertInto(u, u.ProductId, u.Price)
    .Values(1, 990)
    .OnDuplicateKeyUpdate(u.Price == Excluded(u.Price))
    .Build(Dbms.MySql);
// INSERT INTO product (product_id, price) VALUES (?0, ?1)
// AS new ON DUPLICATE KEY UPDATE price = new.price

// PostgreSQL / SQLite — insert-or-skip:
InsertInto(u, u.ProductId, u.Price)
    .Values(1, 990)
    .OnConflict()
    .DoNothing()
    .Build();
// INSERT INTO product (product_id, price) VALUES (:0, :1)
// ON CONFLICT DO NOTHING

// MySQL — insert-or-skip:
InsertIgnoreInto(u, u.ProductId, u.Price)
    .Values(1, 990)
    .Build(Dbms.MySql);
// INSERT IGNORE INTO product (product_id, price) VALUES (?0, ?1)

// Oracle — single-row upsert via MERGE against DUAL:
Product t = new("t");
Product cols = new();                          // unaliased, for the INSERT column list
MergeInto(t).Using(Dual).On(t.ProductId == 1)
    .WhenMatched().ThenUpdateSet(t.Price == 990)
    .WhenNotMatched().ThenInsert(cols.ProductId, cols.Price).Values(1, 990)
    .Build(Dbms.Oracle);
// MERGE INTO product "t" USING DUAL ON ("t".product_id = :0)
// WHEN MATCHED THEN UPDATE SET "t".price = :1
// WHEN NOT MATCHED THEN INSERT (product_id, price) VALUES (:2, :3)
```

SQL Server's UPSERT is also `MERGE`, but it has no `DUAL` — for a single
literal row there, stage the row in a table (or run `UPDATE` then a
conditional `INSERT` in one transaction) and `MERGE` from that source, as in
[Synchronizing a table from staging](#synchronizing-a-table-from-staging).

On MySQL and SQL Server there is no `RETURNING` clause — read the row back by
key (or use the driver's last-insert-id facility) in the same transaction. See
[RETURNING](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#returning-clause).

### Synchronizing a table from staging

Full catalog sync — update changed rows, insert new ones, delete rows gone
from the source:

```csharp
Product t = new("t");
StagingProduct s = new("s");
Product cols = new();                          // unaliased, for the INSERT column list

// SQL Server (WHEN NOT MATCHED BY SOURCE is SQL Server-only):
SqlStatement sql =
    MergeInto(t).Using(s).On(t.ProductId == s.ProductId)
        .WhenMatched(t.Price != s.Price).ThenUpdateSet(t.Price == s.Price)
        .WhenNotMatched().ThenInsert(cols.ProductId, cols.Name, cols.Price)
            .Values(s.ProductId, s.Name, s.Price)
        .WhenNotMatchedBySource().ThenDelete()
        .Build(Dbms.SqlServer);

// MERGE INTO product "t" USING staging_product "s"
// ON ("t".product_id = "s".product_id)
// WHEN MATCHED AND "t".price <> "s".price
//   THEN UPDATE SET "t".price = "s".price
// WHEN NOT MATCHED THEN INSERT (product_id, name, price)
//   VALUES ("s".product_id, "s".name, "s".price)
// WHEN NOT MATCHED BY SOURCE THEN DELETE;

// Oracle — the delete rides the update branch instead:
MergeInto(t).Using(s).On(t.ProductId == s.ProductId)
    .WhenMatched().ThenUpdateSet(t.Price == s.Price)
        .DeleteWhere(t.Active == false)
    .Build(Dbms.Oracle);
// MERGE INTO product "t" USING staging_product "s"
// ON ("t".product_id = "s".product_id)
// WHEN MATCHED THEN UPDATE SET "t".price = "s".price
// DELETE WHERE "t".active = :0
```

Mind the SQL Server `MERGE` caveats (auto-appended semicolon, `HOLDLOCK`,
known engine quirks) — see the
[MERGE reference](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#merge-statement).
