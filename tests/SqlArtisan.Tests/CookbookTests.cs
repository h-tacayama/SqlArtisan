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
    public void PaginatedList_CorrectSql()
    {
        CookbookCustomer c = new("c");
        CookbookOrder o = new("o");

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

        StringBuilder expected = new();
        expected.Append("SELECT \"c\".customer_id, \"c\".first_name, \"c\".last_name, ");
        expected.Append("\"o\".order_id, \"o\".total_amount ");
        expected.Append("FROM customer \"c\" ");
        expected.Append("INNER JOIN orders \"o\" ON \"c\".customer_id = \"o\".customer_id ");
        expected.Append("WHERE \"o\".status = :0 ");
        expected.Append("ORDER BY \"o\".ordered_at DESC, \"o\".order_id LIMIT :1 OFFSET :2");

        Assert.Equal(expected.ToString(), page1.Text);
        Assert.Equal(expected.ToString(), page2.Text);
        Assert.Equal(0, page1.Parameters.Get<int>(":2"));
        Assert.Equal(20, page2.Parameters.Get<int>(":2"));
    }

    [Fact]
    public void FilteredAggregateReport_CorrectSql()
    {
        bool filterByCategory = true, filterByActive = true;
        CookbookCategory c = new("c");
        CookbookProduct p = new("p");
        CookbookOrderItem oi = new("oi");

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

        StringBuilder expected = new();
        expected.Append("SELECT \"c\".name \"category\", ");
        expected.Append("COUNT(DISTINCT \"p\".product_id) \"products\", ");
        expected.Append("SUM((\"oi\".quantity * \"oi\".unit_price)) \"revenue\" ");
        expected.Append("FROM category \"c\" ");
        expected.Append("INNER JOIN product \"p\" ON \"c\".category_id = \"p\".category_id ");
        expected.Append("INNER JOIN order_item \"oi\" ON \"p\".product_id = \"oi\".product_id ");
        expected.Append("WHERE (\"c\".name = :0) AND (\"p\".active = :1) ");
        expected.Append("GROUP BY \"c\".name ");
        expected.Append("HAVING SUM((\"oi\".quantity * \"oi\".unit_price)) > :2 ");
        expected.Append("ORDER BY SUM((\"oi\".quantity * \"oi\".unit_price)) DESC");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("Electronics", sql.Parameters.Get<string>(":0"));
        Assert.True(sql.Parameters.Get<bool>(":1"));
        Assert.Equal(1000, sql.Parameters.Get<int>(":2"));
    }

    [Fact]
    public void CaseSortOrder_CorrectSql()
    {
        CookbookOrder o = new("o");
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

        StringBuilder expected = new();
        expected.Append("SELECT \"o\".order_id, \"o\".customer_id, \"o\".status, \"o\".total_amount ");
        expected.Append("FROM orders \"o\" ");
        expected.Append("ORDER BY CASE WHEN (\"o\".status = :0) THEN :1 ");
        expected.Append("WHEN (\"o\".status = :2) THEN :3 ");
        expected.Append("WHEN (\"o\".status = :4) THEN :5 ");
        expected.Append("ELSE :6 END, \"o\".order_id");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("pending", sql.Parameters.Get<string>(":0"));
        Assert.Equal(1, sql.Parameters.Get<int>(":1"));
        Assert.Equal("shipped", sql.Parameters.Get<string>(":2"));
        Assert.Equal(2, sql.Parameters.Get<int>(":3"));
        Assert.Equal("cancelled", sql.Parameters.Get<string>(":4"));
        Assert.Equal(3, sql.Parameters.Get<int>(":5"));
        Assert.Equal(4, sql.Parameters.Get<int>(":6"));
    }

    [Fact]
    public void UpsertReturning_CorrectSql()
    {
        CookbookProduct u = new();
        SqlStatement sql =
            InsertInto(u, u.ProductId, u.Price)
                .Values(1, 990)
                .OnConflict(u.ProductId)
                .DoUpdateSet(u.Price == Excluded(u.Price))
                .Returning(u.ProductId, u.Price)
                .Build();

        Assert.Equal(
            "INSERT INTO product (product_id, price) VALUES (:0, :1) "
            + "ON CONFLICT (product_id) DO UPDATE SET price = EXCLUDED.price "
            + "RETURNING product_id, price",
            sql.Text);
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
