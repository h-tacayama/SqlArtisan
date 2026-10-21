using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

// The reading recipes of docs/cookbook.md, shared by CookbookTests (the emitted text)
// and the integration suite's CookbookRecipeTests (the rows on SQLite), so the page,
// its text pin and its row gate stay one chain (#686).
internal static class CookbookRecipes
{
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

    public static SqlCondition SearchCondition(
        CookbookCustomer c, bool useName, bool useRegion, bool useDateRange) =>
        ConditionIf(useName, c.LastName.Like("%son%"))
        & ConditionIf(useRegion, c.Region == "west")
        & ConditionIf(
            useDateRange,
            c.CreatedAt >= new DateTime(2025, 1, 1) & c.CreatedAt < new DateTime(2026, 1, 1));
}
