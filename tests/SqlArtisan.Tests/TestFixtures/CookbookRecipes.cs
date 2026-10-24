using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

// Every docs/cookbook.md recipe SQLite runs, shared by CookbookTests (the emitted text)
// and the integration suite's CookbookRecipeTests (the rows on SQLite), so the page, its
// text pin and its row gate stay one chain (#686).
internal static class CookbookRecipes
{
    public static ISqlBuilder PaginatedList(int offset)
    {
        CookbookCustomer c = new("c");
        CookbookOrder o = new("o");
        return
            Select(c.CustomerId, c.FirstName, c.LastName, o.OrderId, o.TotalAmount)
            .From(c)
            .InnerJoin(o).On(c.CustomerId == o.CustomerId)
            .Where(o.Status == "shipped")
            .OrderBy(o.OrderedAt.Desc, o.OrderId)
            .Limit(20).Offset(offset);
    }

    public static ISqlBuilder CaseSortOrder()
    {
        CookbookOrder o = new("o");
        return
            Select(o.OrderId, o.CustomerId, o.Status, o.TotalAmount)
            .From(o)
            .OrderBy(
                Case(
                    When(o.Status == "pending").Then(1),
                    When(o.Status == "shipped").Then(2),
                    When(o.Status == "cancelled").Then(3),
                    Else(4)),
                o.OrderId);
    }

    public static ISqlBuilder PivotByConditionalAggregation()
    {
        CookbookOrder o = new("o");
        return
            Select(
                o.CustomerId,
                Sum(Case(When(o.Status == "shipped").Then(1), Else(0))).As("shipped_cnt"),
                Sum(Case(When(o.Status == "cancelled").Then(1), Else(0))).As("cancelled_cnt"),
                Count(Asterisk).As("total"))
            .From(o)
            .GroupBy(o.CustomerId);
    }

    public static ISqlBuilder PivotByAggregateFilter()
    {
        CookbookOrder o = new("o");
        return
            Select(
                o.CustomerId,
                Count(Asterisk).Filter(o.Status == "shipped").As("shipped_cnt"),
                Count(Asterisk).As("total"))
            .From(o)
            .GroupBy(o.CustomerId);
    }

    public static ISqlBuilder FilteredAggregateReport(bool filterByCategory, bool filterByActive)
    {
        CookbookCategory c = new("c");
        CookbookProduct p = new("p");
        CookbookOrderItem oi = new("oi");

        SqlCondition where =
            ConditionIf(filterByCategory, c.Name == "Electronics")
            & ConditionIf(filterByActive, p.Active == true);

        return
            Select(
                c.Name.As("category"),
                Count(Distinct, p.ProductId).As("products"),
                Sum(oi.Quantity * oi.UnitPrice).As("revenue"))
            .From(c)
            .InnerJoin(p).On(c.CategoryId == p.CategoryId)
            .InnerJoin(oi).On(p.ProductId == oi.ProductId)
            .Where(where)
            .GroupBy(c.CategoryId, c.Name)
            .Having(Sum(oi.Quantity * oi.UnitPrice) > 1000)
            .OrderBy(Sum(oi.Quantity * oi.UnitPrice).Desc);
    }

    public static ISqlBuilder TopNPerGroup()
    {
        CookbookCategory c = new("c");
        CookbookProduct p = new("p");
        CookbookOrderItem oi = new("oi");
        Cte ranked = new("ranked");

        return
            With(ranked.As(
                Select(
                    c.CategoryId.As(ranked.Column("category_id")),
                    c.Name.As(ranked.Column("category_name")),
                    p.Name.As(ranked.Column("product_name")),
                    Sum(oi.Quantity * oi.UnitPrice).As(ranked.Column("sales")),
                    RowNumber()
                        .Over(PartitionBy(c.CategoryId)
                            .OrderBy(Sum(oi.Quantity * oi.UnitPrice).Desc))
                        .As(ranked.Column("rn")))
                .From(c)
                .InnerJoin(p).On(c.CategoryId == p.CategoryId)
                .InnerJoin(oi).On(p.ProductId == oi.ProductId)
                .GroupBy(c.CategoryId, c.Name, p.ProductId, p.Name)))
            .Select(
                ranked.Column("category_id"),
                ranked.Column("category_name"),
                ranked.Column("product_name"),
                ranked.Column("sales"))
            .From(ranked)
            .Where(ranked.Column("rn") <= 3)
            .OrderBy(
                ranked.Column("category_name"),
                ranked.Column("category_id"),
                ranked.Column("sales").Desc);
    }

    public static ISqlBuilder AntiJoinNotExists()
    {
        CookbookProduct p = new("p");
        CookbookOrderItem oi = new("oi");
        return
            Select(p.Name).From(p)
                .Where(NotExists(
                    Select(oi.OrderId).From(oi).Where(oi.ProductId == p.ProductId)));
    }

    public static ISqlBuilder AntiJoinLeftJoinIsNull()
    {
        CookbookProduct p = new("p");
        CookbookOrderItem oi = new("oi");
        return
            Select(p.Name).From(p)
                .LeftJoin(oi).On(p.ProductId == oi.ProductId)
                .Where(oi.OrderId.IsNull);
    }

    public static ISqlBuilder AntiJoinNotIn()
    {
        CookbookProduct p = new("p");
        CookbookOrderItem oi = new("oi");
        return
            Select(p.Name).From(p)
                .Where(p.ProductId.NotIn(Select(oi.ProductId).From(oi)));
    }

    public static ISqlBuilder SearchCount(CookbookCustomer c, SqlCondition where) =>
        Select(Count(Asterisk))
        .From(c)
        .Where(where);

    public static ISqlBuilder UpsertDoUpdate()
    {
        CookbookProduct u = new();
        return
            InsertInto(u, u.ProductId, u.Price)
                .Values(1, 990)
                .OnConflict(u.ProductId)
                .DoUpdateSet(u.Price == Excluded(u.Price));
    }

    public static ISqlBuilder UpsertDoUpdateReturning()
    {
        CookbookProduct u = new();
        return
            InsertInto(u, u.ProductId, u.Price)
                .Values(1, 990)
                .OnConflict(u.ProductId)
                .DoUpdateSet(u.Price == Excluded(u.Price))
                .Returning(u.ProductId, u.Price);
    }

    public static ISqlBuilder UpsertDoNothing()
    {
        CookbookProduct u = new();
        return
            InsertInto(u, u.ProductId, u.Price)
                .Values(1, 990)
                .OnConflict()
                .DoNothing();
    }

    public static ISqlBuilder SearchPage(
        CookbookCustomer c,
        SqlCondition where,
        string sortKey,
        bool sortDesc,
        int offset)
    {
        DbColumn sortCol = sortKey switch
        {
            "last_name" => c.LastName,
            "created_at" => c.CreatedAt,
            _ => c.CustomerId,
        };
        object orderItem = sortDesc ? sortCol.Desc : sortCol;
        object[] orderBy = sortKey is "last_name" or "created_at"
            ? [orderItem, c.CustomerId]
            : [orderItem];

        return
            Select(c.CustomerId, c.FirstName, c.LastName, c.Email)
            .From(c)
            .Where(where)
            .OrderBy(orderBy)
            .Limit(20).Offset(offset);
    }

    public static SqlCondition SearchCondition(
        CookbookCustomer c,
        bool useName,
        bool useRegion,
        bool useDateRange) =>
        ConditionIf(useName, c.LastName.Like("%son%"))
        & ConditionIf(useRegion, c.Region == "west")
        & ConditionIf(
            useDateRange,
            c.CreatedAt >= new DateTime(2025, 1, 1) & c.CreatedAt < new DateTime(2026, 1, 1));
}
