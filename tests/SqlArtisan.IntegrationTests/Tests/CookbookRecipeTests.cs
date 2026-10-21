using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using SqlArtisan.Dapper;
using SqlArtisan.Tests;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Tests;

// Every docs/cookbook.md recipe SQLite runs, over rows the schema admits but a
// happy-path seed never holds (#686).
[Trait("Engine", "Sqlite")]
public sealed class CookbookRecipeTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public CookbookRecipeTests()
    {
        _connection.Open();
        _connection.Execute(
            """
            CREATE TABLE category (category_id INTEGER PRIMARY KEY, name TEXT NOT NULL);
            CREATE TABLE product (
                product_id INTEGER PRIMARY KEY, category_id INTEGER NOT NULL DEFAULT 0,
                name TEXT NOT NULL DEFAULT '', price INTEGER NOT NULL,
                active BOOLEAN NOT NULL DEFAULT 1);
            CREATE TABLE order_item (
                order_id INTEGER NOT NULL, product_id INTEGER NOT NULL,
                quantity INTEGER NOT NULL, unit_price INTEGER NOT NULL,
                PRIMARY KEY (order_id, product_id));
            CREATE TABLE customer (
                customer_id INT PRIMARY KEY, first_name TEXT NOT NULL,
                last_name TEXT NOT NULL, email TEXT NOT NULL, region TEXT NOT NULL,
                created_at TIMESTAMP NOT NULL);
            CREATE TABLE orders (
                order_id INT PRIMARY KEY, customer_id INTEGER NOT NULL,
                ordered_at TIMESTAMP NOT NULL, status TEXT NOT NULL,
                total_amount INTEGER NOT NULL);
            INSERT INTO category VALUES (1, 'Electronics'), (2, 'Electronics'), (3, 'Audio');
            INSERT INTO product VALUES
                (10, 1, 'Cable', 300, 1), (11, 1, 'Cable', 300, 1), (12, 1, 'Adapter', 500, 1),
                (13, 1, 'Hub', 100, 0), (14, 2, 'Cable', 400, 1), (20, 3, 'Speaker', 1000, 1),
                (21, 3, 'Turntable', 900, 1);
            INSERT INTO order_item VALUES
                (100, 10, 1, 300), (101, 11, 1, 300), (100, 12, 1, 500), (100, 13, 1, 100),
                (102, 14, 1, 400), (100, 20, 2, 1000);
            """);
        // INT PRIMARY KEY is no rowid alias, and rows go in in descending key order, so a
        // scan returns tied rows against key order and only a recipe's key tie-break sorts them.
        _connection.Execute(
            "INSERT INTO customer VALUES (@Id, 'X', @LastName, 'x@example.com', 'west', @At)",
            new[]
            {
                new { Id = 6, LastName = "Watson", At = new DateTime(2025, 3, 1) },
                new { Id = 5, LastName = "Smith", At = new DateTime(2025, 6, 1) },
                new { Id = 4, LastName = "Carson", At = new DateTime(2024, 12, 31, 23, 59, 59) },
                new { Id = 3, LastName = "Nelson", At = new DateTime(2026, 1, 1) },
                new { Id = 2, LastName = "Watson", At = new DateTime(2025, 12, 31, 15, 30, 0) },
                new { Id = 1, LastName = "Johnson", At = new DateTime(2025, 1, 1) },
            });

        // Every order shares one timestamp; customer 2 holds the other statuses, and
        // customer 3 holds no order.
        (int Id, int CustomerId, string Status)[] orders =
        [
            .. Enumerable.Range(1, 25).Select(id => (id, 1, "shipped")),
            (26, 2, "cancelled"),
            (27, 2, "pending"),
            (28, 2, "returned"),
            (29, 2, "pending"),
        ];
        _connection.Execute(
            "INSERT INTO orders VALUES (@Id, @CustomerId, @At, @Status, 10)",
            orders.Reverse().Select(o => new
            {
                o.Id,
                o.CustomerId,
                At = new DateTime(2025, 5, 1),
                o.Status,
            }));
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public void FilteredAggregateReport_SameNamedCategories_StayApart()
    {
        (string Category, long Products, long Revenue)[] rows =
        [
            .. _connection.Query<(string, long, long)>(
                CookbookRecipes.FilteredAggregateReport(true, true)),
        ];

        Assert.Equal([("Electronics", 3L, 1100L)], rows);
    }

    [Fact]
    public void TopNPerGroup_SameNamedProductsAndCategories_RankApart()
    {
        (long CategoryId, string Category, string Product, long Sales)[] rows =
        [
            .. _connection.Query<(long, string, string, long)>(CookbookRecipes.TopNPerGroup()),
        ];

        Assert.Equal(
            [
                (3L, "Audio", "Speaker", 2000L),
                (1L, "Electronics", "Adapter", 500L),
                (1L, "Electronics", "Cable", 300L),
                (1L, "Electronics", "Cable", 300L),
                (2L, "Electronics", "Cable", 400L),
            ],
            rows);
    }

    [Fact]
    public void AntiJoin_EverySpelling_ReturnsTheProductsNeverOrdered()
    {
        foreach (ISqlBuilder recipe in new[]
        {
            CookbookRecipes.AntiJoinNotExists(),
            CookbookRecipes.AntiJoinLeftJoinIsNull(),
            CookbookRecipes.AntiJoinNotIn(),
        })
        {
            Assert.Equal(["Turntable"], _connection.Query<string>(recipe));
        }
    }

    [Fact]
    public void PaginatedList_TiedTimestamps_PagesDoNotOverlap()
    {
        (int CustomerId, string FirstName, string LastName, int OrderId, int Total)[] page1 =
        [
            .. _connection.Query<(int, string, string, int, int)>(
                CookbookRecipes.PaginatedList(0)),
        ];
        (int CustomerId, string FirstName, string LastName, int OrderId, int Total)[] page2 =
        [
            .. _connection.Query<(int, string, string, int, int)>(
                CookbookRecipes.PaginatedList(20)),
        ];

        Assert.Equal(20, page1.Length);
        Assert.Equal(
            Enumerable.Range(1, 25),
            page1.Concat(page2).Select(row => row.OrderId));
    }

    [Fact]
    public void CaseSortOrder_RepeatedStatuses_SortByRankThenKey()
    {
        IEnumerable<int> ids = _connection
            .Query<(int OrderId, int CustomerId, string Status, int Total)>(
                CookbookRecipes.CaseSortOrder())
            .Select(row => row.OrderId);

        Assert.Equal([27, 29, .. Enumerable.Range(1, 25), 26, 28], ids);
    }

    [Fact]
    public void Pivot_EverySpelling_OneRowPerCustomerWithOrders()
    {
        (long CustomerId, long Shipped, long Cancelled, long Total)[] conditional =
        [
            .. _connection
                .Query<(long, long, long, long)>(CookbookRecipes.PivotByConditionalAggregation())
                .OrderBy(row => row.Item1),
        ];
        (long CustomerId, long Shipped, long Total)[] filtered =
        [
            .. _connection
                .Query<(long, long, long)>(CookbookRecipes.PivotByAggregateFilter())
                .OrderBy(row => row.Item1),
        ];

        Assert.Equal([(1L, 25L, 0L, 25L), (2L, 0L, 1L, 4L)], conditional);
        Assert.Equal([(1L, 25L, 25L), (2L, 0L, 4L)], filtered);
    }

    [Fact]
    public void SearchScreen_RepeatedSortKey_OrdersByKeyWithin()
    {
        CookbookCustomer c = new("c");
        SqlCondition where = CookbookRecipes.SearchCondition(c, true, true, true);

        IEnumerable<int> ids = _connection
            .Query<(int CustomerId, string FirstName, string LastName, string Email)>(
                CookbookRecipes.SearchPage(c, where, "last_name", true, 0))
            .Select(row => row.CustomerId);

        Assert.Equal([2, 6, 1], ids);
    }

    [Fact]
    public void Upsert_DoUpdate_InsertsThenUpdatesOneRow()
    {
        Assert.Equal(1, _connection.Execute(CookbookRecipes.UpsertDoUpdate()));
        _connection.Execute("UPDATE product SET price = 100 WHERE product_id = 1");

        Assert.Equal(1, _connection.Execute(CookbookRecipes.UpsertDoUpdate()));

        Assert.Equal(
            [990L],
            _connection.Query<long>("SELECT price FROM product WHERE product_id = 1"));
    }

    [Fact]
    public void Upsert_DoUpdateReturning_ReadsBackTheAffectedRow()
    {
        _connection.Execute("INSERT INTO product (product_id, price) VALUES (1, 100)");

        (long ProductId, long Price) row = _connection
            .QuerySingle<(long, long)>(CookbookRecipes.UpsertDoUpdateReturning());

        Assert.Equal((1L, 990L), row);
    }

    [Fact]
    public void Upsert_DoNothing_SkipsAnExistingRow()
    {
        _connection.Execute("INSERT INTO product (product_id, price) VALUES (1, 100)");

        Assert.Equal(0, _connection.Execute(CookbookRecipes.UpsertDoNothing()));
        Assert.Equal(
            [100L],
            _connection.Query<long>("SELECT price FROM product WHERE product_id = 1"));
    }

    [Fact]
    public void SearchScreen_DateRange_KeepsTheLastDay()
    {
        CookbookCustomer c = new("c");
        SqlCondition where = CookbookRecipes.SearchCondition(c, true, true, true);

        long count = _connection.ExecuteScalar<long>(CookbookRecipes.SearchCount(c, where));

        Assert.Equal(3, count);
    }
}
