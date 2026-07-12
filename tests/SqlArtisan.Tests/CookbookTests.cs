using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

// Pins the emitted SQL of every recipe in docs/cookbook.md (#227), so the doc
// examples never rot — the DocsIndexTests approach applied to page content.
// Table classes live in TestFixtures/CookbookTables.cs (Cookbook* prefix).
public class CookbookTests
{
    [Fact]
    public void TopNPerGroup_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("WITH \"ranked\" AS (");
        expected.Append("SELECT \"c\".name category_name, \"p\".name product_name, ");
        expected.Append("SUM((\"oi\".quantity * \"oi\".unit_price)) sales, ");
        expected.Append("ROW_NUMBER() OVER (PARTITION BY \"c\".name ");
        expected.Append("ORDER BY SUM((\"oi\".quantity * \"oi\".unit_price)) DESC) rn ");
        expected.Append("FROM category \"c\" ");
        expected.Append("INNER JOIN product \"p\" ON \"c\".category_id = \"p\".category_id ");
        expected.Append("INNER JOIN order_item \"oi\" ON \"p\".product_id = \"oi\".product_id ");
        expected.Append("GROUP BY \"c\".name, \"p\".name) ");
        expected.Append("SELECT \"ranked\".category_name, \"ranked\".product_name, \"ranked\".sales ");
        expected.Append("FROM \"ranked\" WHERE \"ranked\".rn <= :0 ");
        expected.Append("ORDER BY \"ranked\".category_name, \"ranked\".sales DESC");

        CookbookCategory c = new("c");
        CookbookProduct p = new("p");
        CookbookOrderItem oi = new("oi");
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

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(3, sql.Parameters.Get<int>(":0"));
    }

    [Fact]
    public void MonthOverMonth_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("WITH \"monthly\" AS (");
        expected.Append("SELECT DATE_TRUNC('MONTH', \"o\".ordered_at) month, ");
        expected.Append("SUM(\"o\".total_amount) revenue ");
        expected.Append("FROM orders \"o\" GROUP BY DATE_TRUNC('MONTH', \"o\".ordered_at)) ");
        expected.Append("SELECT \"monthly\".month, \"monthly\".revenue, ");
        expected.Append("((\"monthly\".revenue - LAG(\"monthly\".revenue) OVER (ORDER BY \"monthly\".month)) ");
        expected.Append("/ NULLIF(LAG(\"monthly\".revenue) OVER (ORDER BY \"monthly\".month), :0)) \"mom_ratio\" ");
        expected.Append("FROM \"monthly\" ORDER BY \"monthly\".month");

        CookbookOrder o = new("o");
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

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(0, sql.Parameters.Get<int>(":0"));
    }

    [Fact]
    public void MonthBucket_MySql_CorrectSql()
    {
        CookbookOrder o = new("o");
        var month = DateFormat(o.OrderedAt, "%Y-%m");
        SqlStatement sql =
            Select(month, Sum(o.TotalAmount)).From(o).GroupBy(month).Build(Dbms.MySql);

        Assert.Equal(
            "SELECT DATE_FORMAT(`o`.ordered_at, ?0), SUM(`o`.total_amount) "
            + "FROM orders `o` GROUP BY DATE_FORMAT(`o`.ordered_at, ?0)",
            sql.Text);
        Assert.Equal("%Y-%m", sql.Parameters.Get<string>("?0"));
    }

    [Fact]
    public void MonthBucket_Oracle_CorrectSql()
    {
        CookbookOrder o = new("o");
        var month = Trunc(o.OrderedAt, "MM");
        SqlStatement sql =
            Select(month, Sum(o.TotalAmount)).From(o).GroupBy(month).Build(Dbms.Oracle);

        Assert.Equal(
            "SELECT TRUNC(\"o\".ordered_at, :0), SUM(\"o\".total_amount) "
            + "FROM orders \"o\" GROUP BY TRUNC(\"o\".ordered_at, :0)",
            sql.Text);
        Assert.Equal("MM", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void MonthBucket_SqlServer_CorrectSql()
    {
        CookbookOrder o = new("o");
        var month = Datetrunc(DateTimePart.Month, o.OrderedAt);
        SqlStatement sql =
            Select(month, Sum(o.TotalAmount)).From(o).GroupBy(month).Build(Dbms.SqlServer);

        Assert.Equal(
            "SELECT DATETRUNC(MONTH, \"o\".ordered_at), SUM(\"o\".total_amount) "
            + "FROM orders \"o\" GROUP BY DATETRUNC(MONTH, \"o\".ordered_at)",
            sql.Text);
    }

    [Fact]
    public void PivotByConditionalAggregation_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("SELECT \"o\".customer_id, ");
        expected.Append("SUM(CASE WHEN (\"o\".status = :0) THEN :1 ELSE :2 END) \"shipped_cnt\", ");
        expected.Append("SUM(CASE WHEN (\"o\".status = :3) THEN :4 ELSE :5 END) \"cancelled_cnt\", ");
        expected.Append("COUNT(*) \"total\" ");
        expected.Append("FROM orders \"o\" GROUP BY \"o\".customer_id");

        CookbookOrder o = new("o");
        SqlStatement sql =
            Select(
                o.CustomerId,
                Sum(Case(When(o.Status == "shipped").Then(1), Else(0))).As("shipped_cnt"),
                Sum(Case(When(o.Status == "cancelled").Then(1), Else(0))).As("cancelled_cnt"),
                Count(Asterisk).As("total"))
            .From(o)
            .GroupBy(o.CustomerId)
            .Build();

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("shipped", sql.Parameters.Get<string>(":0"));
        Assert.Equal("cancelled", sql.Parameters.Get<string>(":3"));
    }

    [Fact]
    public void PivotByAggregateFilter_CorrectSql()
    {
        CookbookOrder o = new("o");
        SqlStatement sql =
            Select(
                o.CustomerId,
                Count(Asterisk).Filter(o.Status == "shipped").As("shipped_cnt"),
                Count(Asterisk).As("total"))
            .From(o)
            .GroupBy(o.CustomerId)
            .Build();

        Assert.Equal(
            "SELECT \"o\".customer_id, "
            + "COUNT(*) FILTER (WHERE \"o\".status = :0) \"shipped_cnt\", COUNT(*) \"total\" "
            + "FROM orders \"o\" GROUP BY \"o\".customer_id",
            sql.Text);
        Assert.Equal("shipped", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void RunningTotalAndMovingAverage_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("WITH \"daily\" AS (");
        expected.Append("SELECT CAST(\"o\".ordered_at AS DATE) day, SUM(\"o\".total_amount) revenue ");
        expected.Append("FROM orders \"o\" GROUP BY CAST(\"o\".ordered_at AS DATE)) ");
        expected.Append("SELECT \"daily\".day, \"daily\".revenue, ");
        expected.Append("SUM(\"daily\".revenue) OVER (ORDER BY \"daily\".day) \"running_total\", ");
        expected.Append("AVG(\"daily\".revenue) OVER (ORDER BY \"daily\".day ");
        expected.Append("ROWS BETWEEN 6 PRECEDING AND CURRENT ROW) \"ma7\" ");
        expected.Append("FROM \"daily\" ORDER BY \"daily\".day");

        CookbookOrder o = new("o");
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

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DecilesByNtile_CorrectSql()
    {
        CookbookCustomer c = new("c");
        CookbookOrder o = new("o");
        SqlStatement sql =
            Select(
                c.CustomerId,
                Sum(o.TotalAmount).As("ltv"),
                Ntile(10).Over(OrderBy(Sum(o.TotalAmount).Desc)).As("decile"))
            .From(c)
            .InnerJoin(o).On(c.CustomerId == o.CustomerId)
            .GroupBy(c.CustomerId)
            .Build();

        Assert.Equal(
            "SELECT \"c\".customer_id, SUM(\"o\".total_amount) \"ltv\", "
            + "NTILE(10) OVER (ORDER BY SUM(\"o\".total_amount) DESC) \"decile\" "
            + "FROM customer \"c\" INNER JOIN orders \"o\" ON \"c\".customer_id = \"o\".customer_id "
            + "GROUP BY \"c\".customer_id",
            sql.Text);
    }

    [Fact]
    public void CohortRetention_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("WITH \"first_order\" AS (");
        expected.Append("SELECT \"o\".customer_id customer_id, ");
        expected.Append("DATE_TRUNC('MONTH', MIN(\"o\".ordered_at)) cohort_month ");
        expected.Append("FROM orders \"o\" GROUP BY \"o\".customer_id) ");
        expected.Append("SELECT \"first_order\".cohort_month, ");
        expected.Append("DATE_TRUNC('MONTH', \"o\".ordered_at) \"activity_month\", ");
        expected.Append("COUNT(DISTINCT \"o\".customer_id) \"active\" ");
        expected.Append("FROM orders \"o\" ");
        expected.Append("INNER JOIN \"first_order\" ON \"o\".customer_id = \"first_order\".customer_id ");
        expected.Append("GROUP BY \"first_order\".cohort_month, DATE_TRUNC('MONTH', \"o\".ordered_at) ");
        expected.Append("ORDER BY 1, 2");

        CookbookOrder o = new("o");
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

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void AntiJoin_NotExists_CorrectSql()
    {
        CookbookProduct p = new("p");
        CookbookOrderItem oi = new("oi");
        SqlStatement sql =
            Select(p.Name).From(p)
                .Where(NotExists(
                    Select(oi.OrderId).From(oi).Where(oi.ProductId == p.ProductId)))
                .Build();

        Assert.Equal(
            "SELECT \"p\".name FROM product \"p\" WHERE NOT EXISTS ("
            + "SELECT \"oi\".order_id FROM order_item \"oi\" "
            + "WHERE \"oi\".product_id = \"p\".product_id)",
            sql.Text);
    }

    [Fact]
    public void AntiJoin_LeftJoinIsNull_CorrectSql()
    {
        CookbookProduct p = new("p");
        CookbookOrderItem oi = new("oi");
        SqlStatement sql =
            Select(p.Name).From(p)
                .LeftJoin(oi).On(p.ProductId == oi.ProductId)
                .Where(oi.OrderId.IsNull)
                .Build();

        Assert.Equal(
            "SELECT \"p\".name FROM product \"p\" "
            + "LEFT JOIN order_item \"oi\" ON \"p\".product_id = \"oi\".product_id "
            + "WHERE \"oi\".order_id IS NULL",
            sql.Text);
    }

    [Fact]
    public void AntiJoin_NotIn_CorrectSql()
    {
        CookbookProduct p = new("p");
        CookbookOrderItem oi = new("oi");
        SqlStatement sql =
            Select(p.Name).From(p)
                .Where(p.ProductId.NotIn(Select(oi.ProductId).From(oi)))
                .Build();

        Assert.Equal(
            "SELECT \"p\".name FROM product \"p\" WHERE \"p\".product_id NOT IN ("
            + "SELECT \"oi\".product_id FROM order_item \"oi\")",
            sql.Text);
    }

    [Fact]
    public void RecursiveOrgChart_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("WITH RECURSIVE \"org\" AS (");
        expected.Append("SELECT \"s\".staff_id staff_id, \"s\".first_name first_name, ");
        expected.Append("\"s\".reports_to reports_to, CAST(:0 AS INT) depth, \"s\".first_name path ");
        expected.Append("FROM staff \"s\" WHERE \"s\".staff_id = :1 ");
        expected.Append("UNION ALL ");
        expected.Append("SELECT \"s2\".staff_id, \"s2\".first_name, \"s2\".reports_to, ");
        expected.Append("(\"org\".depth + :2), (\"org\".path || :3 || \"s2\".first_name) ");
        expected.Append("FROM staff \"s2\" INNER JOIN \"org\" ON \"s2\".reports_to = \"org\".staff_id) ");
        expected.Append("SELECT \"org\".staff_id, \"org\".first_name, \"org\".depth, \"org\".path ");
        expected.Append("FROM \"org\" ORDER BY \"org\".path");

        CookbookStaff s = new("s");
        CookbookStaff s2 = new("s2");
        CookbookOrgCte org = new("org");
        SqlStatement sql =
            WithRecursive(org.As(
                Select(
                    s.StaffId.As(org.StaffId),
                    s.FirstName.As(org.FirstName),
                    s.ReportsTo.As(org.ReportsTo),
                    Cast(1, "INT").As(org.Depth),
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

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(42, sql.Parameters.Get<int>(":1"));
        Assert.Equal(" > ", sql.Parameters.Get<string>(":3"));
    }

    [Fact]
    public void RecursiveOrgChart_SqlServer_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("WITH \"org\" AS (");
        expected.Append("SELECT \"s\".staff_id staff_id, \"s\".first_name first_name, ");
        expected.Append("\"s\".reports_to reports_to, CAST(@0 AS INT) depth, \"s\".first_name path ");
        expected.Append("FROM staff \"s\" WHERE \"s\".staff_id = @1 ");
        expected.Append("UNION ALL ");
        expected.Append("SELECT \"s2\".staff_id, \"s2\".first_name, \"s2\".reports_to, ");
        expected.Append("(\"org\".depth + @2), ((\"org\".path + @3) + \"s2\".first_name) ");
        expected.Append("FROM staff \"s2\" INNER JOIN \"org\" ON \"s2\".reports_to = \"org\".staff_id) ");
        expected.Append("SELECT \"org\".staff_id, \"org\".first_name, \"org\".depth, \"org\".path ");
        expected.Append("FROM \"org\" ORDER BY \"org\".path");

        CookbookStaff s = new("s");
        CookbookStaff s2 = new("s2");
        CookbookOrgCte org = new("org");
        SqlStatement sql =
            With(org.As(
                Select(
                    s.StaffId.As(org.StaffId),
                    s.FirstName.As(org.FirstName),
                    s.ReportsTo.As(org.ReportsTo),
                    Cast(1, "INT").As(org.Depth),
                    s.FirstName.As(org.Path))
                .From(s)
                .Where(s.StaffId == 42)
                .UnionAll
                .Select(
                    s2.StaffId,
                    s2.FirstName,
                    s2.ReportsTo,
                    org.Depth + 1,
                    org.Path + " > " + s2.FirstName)
                .From(s2)
                .InnerJoin(org).On(s2.ReportsTo == org.StaffId)))
            .Select(org.StaffId, org.FirstName, org.Depth, org.Path)
            .From(org)
            .OrderBy(org.Path)
            .Build(Dbms.SqlServer);

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void FunnelWithPagination_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("WITH \"buyers\" AS (");
        expected.Append("SELECT \"c\".customer_id customer_id, \"c\".region region, COUNT(*) orders ");
        expected.Append("FROM customer \"c\" ");
        expected.Append("INNER JOIN orders \"o\" ON \"c\".customer_id = \"o\".customer_id ");
        expected.Append("GROUP BY \"c\".customer_id, \"c\".region), ");
        expected.Append("\"repeaters\" AS (");
        expected.Append("SELECT \"buyers\".region region, COUNT(*) repeat_customers ");
        expected.Append("FROM \"buyers\" WHERE \"buyers\".orders >= :0 GROUP BY \"buyers\".region) ");
        expected.Append("SELECT \"repeaters\".region, \"repeaters\".repeat_customers ");
        expected.Append("FROM \"repeaters\" WHERE \"repeaters\".repeat_customers > :1 ");
        expected.Append("ORDER BY \"repeaters\".repeat_customers DESC LIMIT :2 OFFSET :3");

        SqlStatement sql = BuildFunnel().Build();

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(10, sql.Parameters.Get<int>(":2"));
    }

    [Fact]
    public void FunnelWithPagination_SqlServer_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("WITH \"buyers\" AS (");
        expected.Append("SELECT \"c\".customer_id customer_id, \"c\".region region, COUNT(*) orders ");
        expected.Append("FROM customer \"c\" ");
        expected.Append("INNER JOIN orders \"o\" ON \"c\".customer_id = \"o\".customer_id ");
        expected.Append("GROUP BY \"c\".customer_id, \"c\".region), ");
        expected.Append("\"repeaters\" AS (");
        expected.Append("SELECT \"buyers\".region region, COUNT(*) repeat_customers ");
        expected.Append("FROM \"buyers\" WHERE \"buyers\".orders >= @0 GROUP BY \"buyers\".region) ");
        expected.Append("SELECT \"repeaters\".region, \"repeaters\".repeat_customers ");
        expected.Append("FROM \"repeaters\" WHERE \"repeaters\".repeat_customers > @1 ");
        expected.Append("ORDER BY \"repeaters\".repeat_customers DESC ");
        expected.Append("OFFSET @2 ROWS FETCH NEXT @3 ROWS ONLY");

        SqlStatement sql = BuildFunnelOffsetFetch().Build(Dbms.SqlServer);

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(0, sql.Parameters.Get<int>("@2"));
        Assert.Equal(10, sql.Parameters.Get<int>("@3"));
    }

    private static Internal.ISelectBuilderPaginated BuildFunnel()
    {
        CookbookCustomer c = new("c");
        CookbookOrder o = new("o");
        Cte buyers = new("buyers");
        Cte repeaters = new("repeaters");
        return
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
            .Limit(10).Offset(0);
    }

    private static Internal.ISelectBuilderPaginated BuildFunnelOffsetFetch()
    {
        CookbookCustomer c = new("c");
        CookbookOrder o = new("o");
        Cte buyers = new("buyers");
        Cte repeaters = new("repeaters");
        return
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
            .OffsetRows(0).FetchNext(10);
    }

    [Fact]
    public void SearchScreen_AllFiltersOn_CorrectSql()
    {
        bool useName = true, useRegion = true, useDateRange = true, sortDesc = true;
        string sortKey = "last_name";

        CookbookCustomer c = new("c");
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

        StringBuilder expected = new();
        expected.Append("SELECT \"c\".customer_id, \"c\".first_name, \"c\".last_name, \"c\".email ");
        expected.Append("FROM customer \"c\" ");
        expected.Append("WHERE (\"c\".last_name LIKE :0) AND (\"c\".region = :1) ");
        expected.Append("AND (\"c\".created_at BETWEEN :2 AND :3) ");
        expected.Append("ORDER BY \"c\".last_name DESC, \"c\".customer_id LIMIT :4 OFFSET :5");
        Assert.Equal(expected.ToString(), page.Text);
        Assert.Equal("%son%", page.Parameters.Get<string>(":0"));
        Assert.Equal(new DateTime(2025, 1, 1), page.Parameters.Get<DateTime>(":2"));

        // The same condition object also drives the companion COUNT query.
        SqlStatement count =
            Select(Count(Asterisk))
            .From(c)
            .Where(where)
            .Build();

        Assert.Equal(
            "SELECT COUNT(*) FROM customer \"c\" "
            + "WHERE (\"c\".last_name LIKE :0) AND (\"c\".region = :1) "
            + "AND (\"c\".created_at BETWEEN :2 AND :3)",
            count.Text);
    }

    [Fact]
    public void SearchScreen_AllFiltersExcluded_ThrowsArgumentException()
    {
        CookbookCustomer c = new("c");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(c.CustomerId)
            .From(c)
            .Where(
                ConditionIf(false, c.Region == "west")
                & ConditionIf(false, c.LastName.Like("x")))
            .Build());

        Assert.Equal(
            "The WHERE clause requires a condition; omit it for an unfiltered statement.",
            ex.Message);
    }

    [Fact]
    public void OptionalFilterFold_ZeroFilters_CorrectSql()
    {
        CookbookCustomer c = new("c");
        List<SqlCondition> filters = [];

        SqlCondition? acc = null;
        foreach (SqlCondition f in filters)
        {
            acc = acc is null ? f : acc & f;
        }

        SqlStatement sql = acc is null
            ? Select(c.CustomerId, c.LastName).From(c).OrderBy(c.CustomerId).Build()
            : Select(c.CustomerId, c.LastName).From(c).Where(acc).OrderBy(c.CustomerId).Build();

        Assert.Equal(
            "SELECT \"c\".customer_id, \"c\".last_name FROM customer \"c\" "
            + "ORDER BY \"c\".customer_id",
            sql.Text);
    }

    [Fact]
    public void OptionalFilterFold_TwoFilters_CorrectSql()
    {
        CookbookCustomer c = new("c");
        List<SqlCondition> filters =
        [
            c.LastName.Like("A%"),
            c.Region == "west",
        ];

        SqlCondition? acc = null;
        foreach (SqlCondition f in filters)
        {
            acc = acc is null ? f : acc & f;
        }

        SqlStatement sql = acc is null
            ? Select(c.CustomerId, c.LastName).From(c).OrderBy(c.CustomerId).Build()
            : Select(c.CustomerId, c.LastName).From(c).Where(acc).OrderBy(c.CustomerId).Build();

        Assert.Equal(
            "SELECT \"c\".customer_id, \"c\".last_name FROM customer \"c\" "
            + "WHERE (\"c\".last_name LIKE :0) AND (\"c\".region = :1) "
            + "ORDER BY \"c\".customer_id",
            sql.Text);
    }

    [Fact]
    public void FacetedFilter_CorrectSql()
    {
        List<int> regionIds = [3, 7];
        bool requireOrders = true;

        CookbookCustomer c = new("c");
        CookbookOrder o = new("o");
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

        Assert.Equal(
            "SELECT \"c\".customer_id, \"c\".last_name FROM customer \"c\" "
            + "WHERE (\"c\".customer_id IN (:0, :1)) AND (EXISTS ("
            + "SELECT \"o\".order_id FROM orders \"o\" "
            + "WHERE \"o\".customer_id = \"c\".customer_id)) "
            + "ORDER BY \"c\".last_name ASC NULLS LAST, \"c\".customer_id",
            sql.Text);
        Assert.Equal(3, sql.Parameters.Get<int>(":0"));
        Assert.Equal(7, sql.Parameters.Get<int>(":1"));
    }

    [Fact]
    public void Upsert_UpdateOnConflict_CorrectSql()
    {
        CookbookProduct u = new();
        SqlStatement sql =
            InsertInto(u, u.ProductId, u.Price)
                .Values(1, 990)
                .OnConflict(u.ProductId)
                .DoUpdateSet(u.Price == Excluded(u.Price))
                .Build();

        Assert.Equal(
            "INSERT INTO product (product_id, price) VALUES (:0, :1) "
            + "ON CONFLICT (product_id) DO UPDATE SET price = EXCLUDED.price",
            sql.Text);
    }

    [Fact]
    public void Upsert_Sqlite_WithReturning_CorrectSql()
    {
        CookbookProduct u = new();
        SqlStatement sql =
            InsertInto(u, u.ProductId, u.Price)
                .Values(1, 990)
                .OnConflict(u.ProductId)
                .DoUpdateSet(u.Price == Excluded(u.Price))
                .Returning(u.ProductId, u.Price)
                .Build(Dbms.Sqlite);

        Assert.Equal(
            "INSERT INTO product (product_id, price) VALUES (:0, :1) "
            + "ON CONFLICT (product_id) DO UPDATE SET price = excluded.price "
            + "RETURNING product_id, price",
            sql.Text);
    }

    [Fact]
    public void Upsert_MySql_CorrectSql()
    {
        CookbookProduct u = new();
        SqlStatement sql =
            InsertInto(u, u.ProductId, u.Price)
                .Values(1, 990)
                .OnDuplicateKeyUpdate(u.Price == Excluded(u.Price))
                .Build(Dbms.MySql);

        Assert.Equal(
            "INSERT INTO product (product_id, price) VALUES (?0, ?1) "
            + "AS new ON DUPLICATE KEY UPDATE price = new.price",
            sql.Text);
    }

    [Fact]
    public void Upsert_DoNothing_CorrectSql()
    {
        CookbookProduct u = new();
        SqlStatement sql =
            InsertInto(u, u.ProductId, u.Price)
                .Values(1, 990)
                .OnConflict()
                .DoNothing()
                .Build();

        Assert.Equal(
            "INSERT INTO product (product_id, price) VALUES (:0, :1) "
            + "ON CONFLICT DO NOTHING",
            sql.Text);
    }

    [Fact]
    public void InsertIgnore_MySql_CorrectSql()
    {
        CookbookProduct u = new();
        SqlStatement sql =
            InsertIgnoreInto(u, u.ProductId, u.Price)
                .Values(1, 990)
                .Build(Dbms.MySql);

        Assert.Equal(
            "INSERT IGNORE INTO product (product_id, price) VALUES (?0, ?1)",
            sql.Text);
    }

    [Fact]
    public void Upsert_Oracle_SingleRowMerge_CorrectSql()
    {
        CookbookProduct t = new("t");
        CookbookProduct cols = new();
        SqlStatement sql =
            MergeInto(t).Using(Dual).On(t.ProductId == 1)
                .WhenMatched().ThenUpdateSet(t.Price == 990)
                .WhenNotMatched().ThenInsert(cols.ProductId, cols.Price).Values(1, 990)
                .Build(Dbms.Oracle);

        Assert.Equal(
            "MERGE INTO product \"t\" USING DUAL ON (\"t\".product_id = :0) "
            + "WHEN MATCHED THEN UPDATE SET \"t\".price = :1 "
            + "WHEN NOT MATCHED THEN INSERT (product_id, price) VALUES (:2, :3)",
            sql.Text);
    }

    [Fact]
    public void InsertSelectDedup_CorrectSql()
    {
        CookbookProduct target = new();
        CookbookStagingProduct sp = new("sp");
        CookbookProduct p = new("p");
        SqlStatement sql =
            InsertInto(target, target.ProductId, target.Name, target.Price)
                .Select(sp.ProductId, sp.Name, sp.Price)
                .From(sp)
                .Where(NotExists(
                    Select(p.ProductId).From(p).Where(p.ProductId == sp.ProductId)))
                .Build();

        Assert.Equal(
            "INSERT INTO product (product_id, name, price) "
            + "SELECT \"sp\".product_id, \"sp\".name, \"sp\".price FROM staging_product \"sp\" "
            + "WHERE NOT EXISTS ("
            + "SELECT \"p\".product_id FROM product \"p\" "
            + "WHERE \"p\".product_id = \"sp\".product_id)",
            sql.Text);
    }

    [Fact]
    public void InsertReturning_CorrectSql()
    {
        CookbookOrder u = new();
        SqlStatement sql =
            InsertInto(u, u.CustomerId, u.Status, u.TotalAmount)
                .Values(7, "pending", 1980)
                .Returning(u.OrderId)
                .Build();

        Assert.Equal(
            "INSERT INTO orders (customer_id, status, total_amount) "
            + "VALUES (:0, :1, :2) RETURNING order_id",
            sql.Text);
    }

    [Fact]
    public void ReturningInto_Oracle_CorrectSql()
    {
        CookbookOrder u = new();
        SqlStatement sql =
            InsertInto(u, u.CustomerId, u.Status, u.TotalAmount)
                .Values(7, "pending", 1980)
                .Returning(u.OrderId)
                .Into(new OutputParameter("outOrderId", System.Data.DbType.Int32))
                .Build(Dbms.Oracle);

        Assert.Equal(
            "INSERT INTO orders (customer_id, status, total_amount) "
            + "VALUES (:0, :1, :2) RETURNING order_id INTO :outOrderId",
            sql.Text);
    }

    [Fact]
    public void MergeSync_SqlServer_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("MERGE INTO product \"t\" USING staging_product \"s\" ");
        expected.Append("ON (\"t\".product_id = \"s\".product_id) ");
        expected.Append("WHEN MATCHED AND \"t\".price <> \"s\".price ");
        expected.Append("THEN UPDATE SET \"t\".price = \"s\".price ");
        expected.Append("WHEN NOT MATCHED THEN INSERT (product_id, name, price) ");
        expected.Append("VALUES (\"s\".product_id, \"s\".name, \"s\".price) ");
        expected.Append("WHEN NOT MATCHED BY SOURCE THEN DELETE;");

        CookbookProduct t = new("t");
        CookbookStagingProduct s = new("s");
        CookbookProduct cols = new();
        SqlStatement sql =
            MergeInto(t).Using(s).On(t.ProductId == s.ProductId)
                .WhenMatched(t.Price != s.Price).ThenUpdateSet(t.Price == s.Price)
                .WhenNotMatched().ThenInsert(cols.ProductId, cols.Name, cols.Price)
                    .Values(s.ProductId, s.Name, s.Price)
                .WhenNotMatchedBySource().ThenDelete()
                .Build(Dbms.SqlServer);

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void MergeDeleteWhere_Oracle_CorrectSql()
    {
        CookbookProduct t = new("t");
        CookbookStagingProduct s = new("s");
        SqlStatement sql =
            MergeInto(t).Using(s).On(t.ProductId == s.ProductId)
                .WhenMatched().ThenUpdateSet(t.Price == s.Price)
                    .DeleteWhere(t.Active == false)
                .Build(Dbms.Oracle);

        Assert.Equal(
            "MERGE INTO product \"t\" USING staging_product \"s\" "
            + "ON (\"t\".product_id = \"s\".product_id) "
            + "WHEN MATCHED THEN UPDATE SET \"t\".price = \"s\".price "
            + "DELETE WHERE \"t\".active = :0",
            sql.Text);
        Assert.False(sql.Parameters.Get<bool>(":0"));
    }
}
