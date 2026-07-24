using Oracle.ManagedDataAccess.Client;
using SqlArtisan;
using SqlArtisan.BulkCopy;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;
using SqlArtisan.IntegrationTests.Schema;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Tests;

// Standalone (not IntegrationTestBase-derived): the shared suite would rerun
// per fixture; these tests prove only the SqlArtisan.BulkCopy array-bind path.
[Trait("Engine", "Oracle")]
public sealed class OracleBulkInsertTests : IClassFixture<OracleFixture>
{
    private readonly OracleFixture _fixture;

    public OracleBulkInsertTests(OracleFixture fixture) => _fixture = fixture;

    [Fact]
    public void BulkInsert_MixedTypesWithNulls_InsertsAllRows()
    {
        UsersTable u = new();
        using OracleConnection connection = (OracleConnection)_fixture.OpenConnection();
        using OracleTransaction transaction = connection.BeginTransaction();

        UserRow[] rows =
        [
            new()
            {
                Id = 9001,
                Name = "Bulk One",
                Age = 21,
                DepartmentId = 10,
                CreatedAt = new DateTime(2026, 7, 23, 12, 34, 56),
                IsActive = 1,
                Data = "{\"k\":1}",
            },
            new()
            {
                Id = 9002,
                Name = null,
                Age = null,
                DepartmentId = 20,
                CreatedAt = null,
                IsActive = 0,
                Data = null,
            },
            new()
            {
                Id = 9003,
                Name = "Bulk Three",
                Age = 33,
                DepartmentId = 30,
                CreatedAt = new DateTime(2026, 7, 24, 0, 0, 1),
                IsActive = null,
                Data = "x",
            },
        ];

        int inserted = connection.BulkInsert(u, rows, transaction);

        Assert.Equal(3, inserted);

        // DIAGNOSTIC (#90): bypass Dapper's DateTime? materialization and read the raw
        // created_at value directly to tell apart a write-side vs read-side failure —
        // three different array-bind representations have failed identically so far.
        object? rawCreatedAt = connection.ExecuteScalar(
            Select(u.CreatedAt).From(u).Where(u.Id == 9001),
            transaction);
        Assert.Fail(
            $"DIAGNOSTIC rawCreatedAt='{rawCreatedAt}' "
                + $"type={rawCreatedAt?.GetType().FullName ?? "null"} "
                + $"isDBNull={rawCreatedAt is DBNull}");

        IEnumerable<int> ids = connection.Query<int>(
            Select(u.Id).From(u).Where(u.Id >= 9001).OrderBy(u.Id),
            transaction);
        Assert.Equal(new[] { 9001, 9002, 9003 }, ids);

        UserRow first = connection.QuerySingle<UserRow>(
            Select(u.Id, u.Name, u.Age, u.CreatedAt).From(u).Where(u.Id == 9001),
            transaction);
        Assert.Equal("Bulk One", first.Name);
        Assert.Equal(21, first.Age);
        Assert.Equal(new DateTime(2026, 7, 23, 12, 34, 56), first.CreatedAt);

        UserRow second = connection.QuerySingle<UserRow>(
            Select(u.Id, u.Name, u.Age, u.CreatedAt).From(u).Where(u.Id == 9002),
            transaction);
        Assert.Null(second.Name);
        Assert.Null(second.Age);
        Assert.Null(second.CreatedAt);

        transaction.Rollback();

        Assert.Empty(connection.Query<int>(Select(u.Id).From(u).Where(u.Id >= 9001)));
    }

    [Fact]
    public async Task BulkInsertAsync_DecimalAmounts_InsertsAllRows()
    {
        OrdersTable o = new();
        using OracleConnection connection = (OracleConnection)_fixture.OpenConnection();
        using OracleTransaction transaction = connection.BeginTransaction();

        OrderRow[] rows =
        [
            new() { Id = 9101, UserId = 1, Amount = 111.25m },
            new() { Id = 9102, UserId = 2, Amount = 250.00m },
        ];

        int inserted = await connection.BulkInsertAsync(o, rows, transaction);

        Assert.Equal(2, inserted);

        decimal amount = connection.QuerySingle<decimal>(
            Select(o.Amount).From(o).Where(o.Id == 9101),
            transaction);
        Assert.Equal(111.25m, amount);

        transaction.Rollback();

        Assert.Empty(connection.Query<int>(Select(o.Id).From(o).Where(o.Id >= 9101)));
    }

    private sealed class UserRow
    {
        public long Id { get; init; }

        public string? Name { get; init; }

        public int? Age { get; init; }

        public short DepartmentId { get; init; }

        public DateTime? CreatedAt { get; init; }

        public int? IsActive { get; init; }

        public string? Data { get; init; }
    }

    private sealed class OrderRow
    {
        public int Id { get; init; }

        public int UserId { get; init; }

        public decimal Amount { get; init; }
    }
}
