using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using SqlArtisan.Dapper;
using SqlArtisan.Tests;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Tests;

// docs/cookbook.md's reading recipes over rows its schema admits but a happy-path
// seed never holds: repeated names and a timestamp late on the range's last day (#686).
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
                product_id INTEGER PRIMARY KEY, category_id INTEGER NOT NULL,
                name TEXT NOT NULL, price INTEGER NOT NULL, active BOOLEAN NOT NULL);
            CREATE TABLE order_item (
                order_id INTEGER NOT NULL, product_id INTEGER NOT NULL,
                quantity INTEGER NOT NULL, unit_price INTEGER NOT NULL,
                PRIMARY KEY (order_id, product_id));
            CREATE TABLE customer (
                customer_id INTEGER PRIMARY KEY, first_name TEXT NOT NULL,
                last_name TEXT NOT NULL, email TEXT NOT NULL, region TEXT NOT NULL,
                created_at TIMESTAMP NOT NULL);
            INSERT INTO category VALUES (1, 'Electronics'), (2, 'Electronics'), (3, 'Audio');
            INSERT INTO product VALUES
                (10, 1, 'Cable', 300, 1), (11, 1, 'Cable', 300, 1), (12, 1, 'Adapter', 500, 1),
                (13, 1, 'Hub', 100, 0), (14, 2, 'Cable', 50, 1), (20, 3, 'Speaker', 1000, 1),
                (21, 3, 'Turntable', 900, 1);
            INSERT INTO order_item VALUES
                (100, 10, 1, 300), (101, 11, 1, 300), (100, 12, 1, 500), (100, 13, 1, 100),
                (102, 14, 1, 50), (100, 20, 2, 1000);
            """);
        _connection.Execute(
            "INSERT INTO customer VALUES (@Id, 'X', @LastName, 'x@example.com', 'west', @At)",
            new[]
            {
                new { Id = 1, LastName = "Johnson", At = new DateTime(2025, 1, 1) },
                new { Id = 2, LastName = "Watson", At = new DateTime(2025, 12, 31, 15, 30, 0) },
                new { Id = 3, LastName = "Nelson", At = new DateTime(2026, 1, 1) },
                new { Id = 4, LastName = "Carson", At = new DateTime(2024, 12, 31, 23, 59, 59) },
                new { Id = 5, LastName = "Smith", At = new DateTime(2025, 6, 1) },
            });
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
                (2L, "Electronics", "Cable", 50L),
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
    public void SearchScreen_DateRange_KeepsTheLastDay()
    {
        CookbookCustomer c = new("c");
        SqlCondition where = CookbookRecipes.SearchCondition(c, true, true, true);

        IEnumerable<int> ids = _connection.Query<int>(
            Select(c.CustomerId).From(c).Where(where).OrderBy(c.CustomerId));

        Assert.Equal([1, 2], ids);
    }
}
