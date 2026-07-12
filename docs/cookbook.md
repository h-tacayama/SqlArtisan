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
- [Reporting Queries](#reporting-queries)
  - [Top-N per group](#top-n-per-group) ·
    [Month-over-month change](#month-over-month-change) ·
    [Pivoting rows to columns](#pivoting-rows-to-columns) ·
    [Running total and moving average](#running-total-and-moving-average) ·
    [Deciles over an aggregate](#deciles-over-an-aggregate) ·
    [Monthly cohort retention](#monthly-cohort-retention) ·
    [Rows without a match](#rows-without-a-match) ·
    [Hierarchy with a path column](#hierarchy-with-a-path-column) ·
    [Chained CTEs with pagination](#chained-ctes-with-pagination)
- [Dynamic Search Screens](#dynamic-search-screens)
  - [Optional filters, runtime sort, shared COUNT](#optional-filters-runtime-sort-shared-count) ·
    [A fully optional filter set](#a-fully-optional-filter-set) ·
    [Faceted filters](#faceted-filters)
- [Batch and UPSERT DML](#batch-and-upsert-dml)
  - [UPSERT on every dialect](#upsert-on-every-dialect) ·
    [Skip-existing INSERT](#skip-existing-insert) ·
    [Reading back affected rows](#reading-back-affected-rows) ·
    [Synchronizing a table from staging](#synchronizing-a-table-from-staging)

---

## The Recipe Schema

The recipes share a small web-shop schema. Each table follows the standard
[table class](https://github.com/h-tacayama/SqlArtisan/blob/main/README.md#quick-start)
pattern — shown once here for `customer` and `orders`, the rest
(`category`, `product`, `order_item`, `staff`, `staging_product`) differ only
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

### Month-over-month change

Bucket into months in a CTE, then compare each row to the previous one with
`LAG(...)`. `NULLIF` guards the division against a zero previous month.

```csharp
Order o = new("o");
Cte monthly = new("monthly");

SqlStatement sql =
    With(monthly.As(
        Select(
            DateTrunc(DateTimePart.Month, o.OrderedAt).As(monthly.Column("month")),
            Sum(o.TotalAmount).As(monthly.Column("revenue")))
        .From(o)
        .GroupBy(DateTrunc(DateTimePart.Month, o.OrderedAt))))
    .Select(
        monthly.Column("month"),
        monthly.Column("revenue"),
        ((monthly.Column("revenue") - Lag(monthly.Column("revenue")).Over(OrderBy(monthly.Column("month"))))
            / Nullif(Lag(monthly.Column("revenue")).Over(OrderBy(monthly.Column("month"))), 0))
            .As("mom_ratio"))
    .From(monthly)
    .OrderBy(monthly.Column("month"))
    .Build();

// WITH "monthly" AS (SELECT DATE_TRUNC('MONTH', "o".ordered_at) month,
//   SUM("o".total_amount) revenue
//   FROM orders "o" GROUP BY DATE_TRUNC('MONTH', "o".ordered_at))
// SELECT "monthly".month, "monthly".revenue,
//   (("monthly".revenue - LAG("monthly".revenue) OVER (ORDER BY "monthly".month))
//     / NULLIF(LAG("monthly".revenue) OVER (ORDER BY "monthly".month), :0)) "mom_ratio"
// FROM "monthly" ORDER BY "monthly".month
```

`DATE_TRUNC` is the PostgreSQL bucketing spelling; each DBMS has its own.
Hold the bucket expression in one variable and pass the same instance to both
the `SELECT` list and `GROUP BY` — the occurrences then share bind markers
(see [GROUP BY and HAVING](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#group-by-and-having-clause)):

```csharp
// MySQL
var month = DateFormat(o.OrderedAt, "%Y-%m");
Select(month, Sum(o.TotalAmount)).From(o).GroupBy(month).Build(Dbms.MySql);
// SELECT DATE_FORMAT(`o`.ordered_at, ?0), SUM(`o`.total_amount)
// FROM orders `o` GROUP BY DATE_FORMAT(`o`.ordered_at, ?0)

// Oracle
var month = Trunc(o.OrderedAt, "MM");
Select(month, Sum(o.TotalAmount)).From(o).GroupBy(month).Build(Dbms.Oracle);
// SELECT TRUNC("o".ordered_at, :0), SUM("o".total_amount)
// FROM orders "o" GROUP BY TRUNC("o".ordered_at, :0)

// SQL Server (2022+)
var month = Datetrunc(DateTimePart.Month, o.OrderedAt);
Select(month, Sum(o.TotalAmount)).From(o).GroupBy(month).Build(Dbms.SqlServer);
// SELECT DATETRUNC(MONTH, "o".ordered_at), SUM("o".total_amount)
// FROM orders "o" GROUP BY DATETRUNC(MONTH, "o".ordered_at)
```

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

### Running total and moving average

A frameless `SUM(...) OVER (ORDER BY ...)` accumulates; a `ROWS BETWEEN`
frame bounds the average to the last seven days.

```csharp
Order o = new("o");
Cte daily = new("daily");

SqlStatement sql =
    With(daily.As(
        Select(
            Cast(o.OrderedAt, "DATE").As(daily.Column("day")),
            Sum(o.TotalAmount).As(daily.Column("revenue")))
        .From(o)
        .GroupBy(Cast(o.OrderedAt, "DATE"))))
    .Select(
        daily.Column("day"),
        daily.Column("revenue"),
        Sum(daily.Column("revenue")).Over(OrderBy(daily.Column("day"))).As("running_total"),
        Avg(daily.Column("revenue"))
            .Over(OrderBy(daily.Column("day")).RowsBetween(Preceding(6), CurrentRow))
            .As("ma7"))
    .From(daily)
    .OrderBy(daily.Column("day"))
    .Build();

// WITH "daily" AS (SELECT CAST("o".ordered_at AS DATE) day,
//   SUM("o".total_amount) revenue
//   FROM orders "o" GROUP BY CAST("o".ordered_at AS DATE))
// SELECT "daily".day, "daily".revenue,
//   SUM("daily".revenue) OVER (ORDER BY "daily".day) "running_total",
//   AVG("daily".revenue) OVER (ORDER BY "daily".day
//     ROWS BETWEEN 6 PRECEDING AND CURRENT ROW) "ma7"
// FROM "daily" ORDER BY "daily".day
```

### Deciles over an aggregate

`NTILE(10)` ordered by an aggregate splits customers into lifetime-value
deciles — a window function ordering over `SUM(...)` composes directly on a
grouped query.

```csharp
Customer c = new("c");
Order o = new("o");
SqlStatement sql =
    Select(
        c.CustomerId,
        Sum(o.TotalAmount).As("ltv"),
        Ntile(10).Over(OrderBy(Sum(o.TotalAmount).Desc)).As("decile"))
    .From(c)
    .InnerJoin(o).On(c.CustomerId == o.CustomerId)
    .GroupBy(c.CustomerId)
    .Build();

// SELECT "c".customer_id, SUM("o".total_amount) "ltv",
//   NTILE(10) OVER (ORDER BY SUM("o".total_amount) DESC) "decile"
// FROM customer "c" INNER JOIN orders "o" ON "c".customer_id = "o".customer_id
// GROUP BY "c".customer_id
```

### Monthly cohort retention

Find each customer's first-order month in a CTE, join activity back to it,
and count distinct active customers per (cohort, activity) month pair.
Functions compose over aggregates (`DATE_TRUNC(MIN(...))`), and `ORDER BY`
accepts column positions.

```csharp
Order o = new("o");
Cte first = new("first_order");

SqlStatement sql =
    With(first.As(
        Select(
            o.CustomerId.As(first.Column("customer_id")),
            DateTrunc(DateTimePart.Month, Min(o.OrderedAt)).As(first.Column("cohort_month")))
        .From(o)
        .GroupBy(o.CustomerId)))
    .Select(
        first.Column("cohort_month"),
        DateTrunc(DateTimePart.Month, o.OrderedAt).As("activity_month"),
        Count(Distinct, o.CustomerId).As("active"))
    .From(o)
    .InnerJoin(first).On(o.CustomerId == first.Column("customer_id"))
    .GroupBy(first.Column("cohort_month"), DateTrunc(DateTimePart.Month, o.OrderedAt))
    .OrderBy(1, 2)
    .Build();

// WITH "first_order" AS (SELECT "o".customer_id customer_id,
//   DATE_TRUNC('MONTH', MIN("o".ordered_at)) cohort_month
//   FROM orders "o" GROUP BY "o".customer_id)
// SELECT "first_order".cohort_month,
//   DATE_TRUNC('MONTH', "o".ordered_at) "activity_month",
//   COUNT(DISTINCT "o".customer_id) "active"
// FROM orders "o"
// INNER JOIN "first_order" ON "o".customer_id = "first_order".customer_id
// GROUP BY "first_order".cohort_month, DATE_TRUNC('MONTH', "o".ordered_at)
// ORDER BY 1, 2
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

### Hierarchy with a path column

An org chart under one manager, with depth and a readable path. The anchor
member seeds the recursion; the recursive member joins the CTE to itself.
Declare a typed `CteBase` when the CTE's columns are referenced this often.

```csharp
internal sealed class OrgCte : CteBase
{
    public OrgCte(string name) : base(name)
    {
        StaffId = new DbColumn(this, "staff_id");
        FirstName = new DbColumn(this, "first_name");
        ReportsTo = new DbColumn(this, "reports_to");
        Depth = new DbColumn(this, "depth");
        Path = new DbColumn(this, "path");
    }

    public DbColumn StaffId { get; }
    public DbColumn FirstName { get; }
    public DbColumn ReportsTo { get; }
    public DbColumn Depth { get; }
    public DbColumn Path { get; }
}

Staff s = new("s");
Staff s2 = new("s2");
OrgCte org = new("org");

SqlStatement sql =
    WithRecursive(org.As(
        Select(
            s.StaffId.As(org.StaffId),
            s.FirstName.As(org.FirstName),
            s.ReportsTo.As(org.ReportsTo),
            Cast(1, "INT").As(org.Depth),      // a bare literal cannot be aliased; CAST can
            s.FirstName.As(org.Path))
        .From(s)
        .Where(s.StaffId == 42)
        .UnionAll
        .Select(
            s2.StaffId,
            s2.FirstName,
            s2.ReportsTo,
            org.Depth + 1,
            DoublePipe(org.Path, " > ", s2.FirstName))
        .From(s2)
        .InnerJoin(org).On(s2.ReportsTo == org.StaffId)))
    .Select(org.StaffId, org.FirstName, org.Depth, org.Path)
    .From(org)
    .OrderBy(org.Path)
    .Build();

// WITH RECURSIVE "org" AS (SELECT "s".staff_id staff_id,
//   "s".first_name first_name, "s".reports_to reports_to,
//   CAST(:0 AS INT) depth, "s".first_name path
//   FROM staff "s" WHERE "s".staff_id = :1
//   UNION ALL
//   SELECT "s2".staff_id, "s2".first_name, "s2".reports_to,
//     ("org".depth + :2), ("org".path || :3 || "s2".first_name)
//   FROM staff "s2" INNER JOIN "org" ON "s2".reports_to = "org".staff_id)
// SELECT "org".staff_id, "org".first_name, "org".depth, "org".path
// FROM "org" ORDER BY "org".path
```

Two per-dialect forks, both faithful spellings:

- **The `RECURSIVE` keyword** — `WithRecursive()` is required on MySQL,
  PostgreSQL, and SQLite; on Oracle (< 23ai) and SQL Server, recurse with plain
  `With(...)` instead — those engines accept the recursive body but reject the
  keyword.
- **The path concatenation** — `DoublePipe(...)` (`||`) works on Oracle,
  PostgreSQL, and SQLite; on MySQL use `Concat(...)`, and on SQL Server use the
  `+` operator (`org.Path + " > " + s2.FirstName`).

### Chained CTEs with pagination

A funnel: customers with orders, narrowed to repeat customers, aggregated per
region — the second CTE selects from the first. The page tail is per dialect
family: `Limit/Offset` on MySQL, PostgreSQL, and SQLite; `OffsetRows/FetchNext`
on Oracle and SQL Server.

```csharp
Customer c = new("c");
Order o = new("o");
Cte buyers = new("buyers");
Cte repeaters = new("repeaters");

SqlStatement sql =
    With(
        buyers.As(
            Select(
                c.CustomerId.As(buyers.Column("customer_id")),
                c.Region.As(buyers.Column("region")),
                Count(Asterisk).As(buyers.Column("orders")))
            .From(c)
            .InnerJoin(o).On(c.CustomerId == o.CustomerId)
            .GroupBy(c.CustomerId, c.Region)),
        repeaters.As(
            Select(
                buyers.Column("region").As(repeaters.Column("region")),
                Count(Asterisk).As(repeaters.Column("repeat_customers")))
            .From(buyers)
            .Where(buyers.Column("orders") >= 2)
            .GroupBy(buyers.Column("region"))))
    .Select(repeaters.Column("region"), repeaters.Column("repeat_customers"))
    .From(repeaters)
    .Where(repeaters.Column("repeat_customers") > 10)
    .OrderBy(repeaters.Column("repeat_customers").Desc)
    .Limit(10).Offset(0)                       // Oracle / SQL Server: .OffsetRows(0).FetchNext(10)
    .Build();

// WITH "buyers" AS (SELECT "c".customer_id customer_id, "c".region region,
//   COUNT(*) orders
//   FROM customer "c"
//   INNER JOIN orders "o" ON "c".customer_id = "o".customer_id
//   GROUP BY "c".customer_id, "c".region),
// "repeaters" AS (SELECT "buyers".region region, COUNT(*) repeat_customers
//   FROM "buyers" WHERE "buyers".orders >= :0 GROUP BY "buyers".region)
// SELECT "repeaters".region, "repeaters".repeat_customers
// FROM "repeaters" WHERE "repeaters".repeat_customers > :1
// ORDER BY "repeaters".repeat_customers DESC LIMIT :2 OFFSET :3
//
// The OFFSET/FETCH tail instead ends:
// ... ORDER BY "repeaters".repeat_customers DESC
//     OFFSET @2 ROWS FETCH NEXT @3 ROWS ONLY
```

Build each variant as its own chain — a
[builder chain is single-use](https://github.com/h-tacayama/SqlArtisan/blob/main/docs/query-statements.md#reusing-a-builder-chain),
so chaining both pagination families onto one held prefix throws.

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
For a screen where all filters can legitimately be off, use the next recipe.

### A fully optional filter set

Collect the active predicates in a list, fold them into one condition, and
call `Where(...)` only when something survived — the all-off state then reads
every row on purpose, by *omitting* the clause:

```csharp
Customer c = new("c");
List<SqlCondition> filters = [];
if (nameFilter is not null) filters.Add(c.LastName.Like(nameFilter));
if (regionFilter is not null) filters.Add(c.Region == regionFilter);

SqlCondition? acc = null;
foreach (SqlCondition f in filters)
{
    acc = acc is null ? f : acc & f;
}

SqlStatement sql = acc is null
    ? Select(c.CustomerId, c.LastName).From(c).OrderBy(c.CustomerId).Build()
    : Select(c.CustomerId, c.LastName).From(c).Where(acc).OrderBy(c.CustomerId).Build();

// zero filters:
// SELECT "c".customer_id, "c".last_name FROM customer "c" ORDER BY "c".customer_id
//
// both filters:
// SELECT "c".customer_id, "c".last_name FROM customer "c"
// WHERE ("c".last_name LIKE :0) AND ("c".region = :1) ORDER BY "c".customer_id
```

The two branches build separate chains, which is exactly what the single-use
builder contract wants.

### Faceted filters

A runtime `IN` list (one bind per element), an `EXISTS` facet that applies
conditionally, and `NULLS LAST` ordering:

```csharp
List<int> regionIds = [3, 7];                  // runtime facet values

Customer c = new("c");
Order o = new("o");
var where =
    ConditionIf(regionIds.Count > 0, c.CustomerId.In([.. regionIds.Cast<object>()]))
    & ConditionIf(requireOrders, Exists(
        Select(o.OrderId).From(o).Where(o.CustomerId == c.CustomerId)));

SqlStatement sql =
    Select(c.CustomerId, c.LastName)
    .From(c)
    .Where(where)
    .OrderBy(c.LastName.Asc.NullsLast, c.CustomerId)
    .Build();

// SELECT "c".customer_id, "c".last_name FROM customer "c"
// WHERE ("c".customer_id IN (:0, :1)) AND (EXISTS (
//   SELECT "o".order_id FROM orders "o"
//   WHERE "o".customer_id = "c".customer_id))
// ORDER BY "c".last_name ASC NULLS LAST, "c".customer_id
```

`In(params object[])` takes the expanded array — a typed list needs the
`[.. list.Cast<object>()]` (or `.Cast<object>().ToArray()`) step shown above.

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
// (Build(Dbms.Sqlite) emits SQLite's lowercase "excluded" instead,
//  and .Returning(...) chains onto it — UPSERT and RETURNING combine.)

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

### Skip-existing INSERT

The portable insert-only-what's-missing: `INSERT ... SELECT` with a
`NOT EXISTS` dedup — runs on all five dialects.

```csharp
Product target = new();
StagingProduct sp = new("sp");
Product p = new("p");

SqlStatement sql =
    InsertInto(target, target.ProductId, target.Name, target.Price)
        .Select(sp.ProductId, sp.Name, sp.Price)
        .From(sp)
        .Where(NotExists(
            Select(p.ProductId).From(p).Where(p.ProductId == sp.ProductId)))
        .Build();

// INSERT INTO product (product_id, name, price)
// SELECT "sp".product_id, "sp".name, "sp".price FROM staging_product "sp"
// WHERE NOT EXISTS (SELECT "p".product_id FROM product "p"
//   WHERE "p".product_id = "sp".product_id)
```

### Reading back affected rows

`RETURNING` reads generated keys and updated values without a second
round-trip:

```csharp
Order u = new();

// PostgreSQL / SQLite — read the generated key:
InsertInto(u, u.CustomerId, u.Status, u.TotalAmount)
    .Values(7, "pending", 1980)
    .Returning(u.OrderId)
    .Build();
// INSERT INTO orders (customer_id, status, total_amount)
// VALUES (:0, :1, :2) RETURNING order_id

// Oracle — RETURNING ... INTO an output parameter:
InsertInto(u, u.CustomerId, u.Status, u.TotalAmount)
    .Values(7, "pending", 1980)
    .Returning(u.OrderId)
    .Into(new OutputParameter("outOrderId", DbType.Int32))
    .Build(Dbms.Oracle);
// INSERT INTO orders (customer_id, status, total_amount)
// VALUES (:0, :1, :2) RETURNING order_id INTO :outOrderId
```

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
