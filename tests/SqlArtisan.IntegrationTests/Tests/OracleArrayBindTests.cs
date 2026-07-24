using Oracle.ManagedDataAccess.Client;
using SqlArtisan;
using SqlArtisan.ArrayBind;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;
using SqlArtisan.IntegrationTests.Schema;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Tests;

// Standalone (not IntegrationTestBase-derived): the shared suite would rerun
// per fixture; these tests prove only the SqlArtisan.ArrayBind execution path.
[Trait("Engine", "Oracle")]
public sealed class OracleArrayBindTests : IClassFixture<OracleFixture>
{
    private readonly OracleFixture _fixture;

    public OracleArrayBindTests(OracleFixture fixture) => _fixture = fixture;

    [Fact]
    public void ExecuteArrayBind_MixedTypesWithNulls_InsertsAllRows()
    {
        UsersTable u = new();
        using OracleConnection connection = (OracleConnection)_fixture.OpenConnection();
        using OracleTransaction transaction = connection.BeginTransaction();

        List<ISqlBuilder> statements =
        [
            InsertInto(u, u.Id, u.Name, u.Age, u.DepartmentId, u.CreatedAt, u.IsActive, u.Data)
                .Values(9001L, "Bulk One", 21, (short)10, new DateTime(2026, 7, 23, 12, 34, 56), 1, "{\"k\":1}"),
            InsertInto(u, u.Id, u.Name, u.Age, u.DepartmentId, u.CreatedAt, u.IsActive, u.Data)
                .Values(9002L, BindNull(), BindNull(), (short)20, BindNull(), 0, BindNull()),
            InsertInto(u, u.Id, u.Name, u.Age, u.DepartmentId, u.CreatedAt, u.IsActive, u.Data)
                .Values(9003L, "Bulk Three", 33, (short)30, new DateTime(2026, 7, 24, 0, 0, 1), BindNull(), "x"),
        ];

        int inserted = connection.ExecuteArrayBind(statements, transaction);

        Assert.Equal(3, inserted);

        IEnumerable<int> ids = connection.Query<int>(
            Select(u.Id).From(u).Where(u.Id >= 9001).OrderBy(u.Id),
            transaction);
        Assert.Equal(new[] { 9001, 9002, 9003 }, ids);

        UserRow first = connection.QuerySingle<UserRow>(
            Select(u.Id, u.Name, u.Age).From(u).Where(u.Id == 9001),
            transaction);
        Assert.Equal("Bulk One", first.Name);
        Assert.Equal(21, first.Age);

        // Read separately via a scalar query: Dapper's reflection-emitted POCO
        // deserializer fails to unbox a non-null DateTime into a DateTime?
        // property (StackExchange/Dapper#295) — unrelated to this package.
        DateTime? firstCreatedAt = connection.ExecuteScalar<DateTime?>(
            Select(u.CreatedAt).From(u).Where(u.Id == 9001),
            transaction);
        Assert.Equal(new DateTime(2026, 7, 23, 12, 34, 56), firstCreatedAt);

        UserRow second = connection.QuerySingle<UserRow>(
            Select(u.Id, u.Name, u.Age).From(u).Where(u.Id == 9002),
            transaction);
        Assert.Null(second.Name);
        Assert.Null(second.Age);

        DateTime? secondCreatedAt = connection.ExecuteScalar<DateTime?>(
            Select(u.CreatedAt).From(u).Where(u.Id == 9002),
            transaction);
        Assert.Null(secondCreatedAt);

        transaction.Rollback();

        Assert.Empty(connection.Query<int>(Select(u.Id).From(u).Where(u.Id >= 9001)));
    }

    [Fact]
    public async Task ExecuteArrayBindAsync_DecimalAmounts_InsertsAllRows()
    {
        OrdersTable o = new();
        using OracleConnection connection = (OracleConnection)_fixture.OpenConnection();
        using OracleTransaction transaction = connection.BeginTransaction();

        List<ISqlBuilder> statements =
        [
            InsertInto(o, o.Id, o.UserId, o.Amount).Values(9101, 1, 111.25m),
            InsertInto(o, o.Id, o.UserId, o.Amount).Values(9102, 2, 250.00m),
        ];

        int inserted = await connection.ExecuteArrayBindAsync(statements, transaction);

        Assert.Equal(2, inserted);

        decimal amount = connection.QuerySingle<decimal>(
            Select(o.Amount).From(o).Where(o.Id == 9101),
            transaction);
        Assert.Equal(111.25m, amount);

        transaction.Rollback();

        Assert.Empty(connection.Query<int>(Select(o.Id).From(o).Where(o.Id >= 9101)));
    }

    // Proves ExecuteArrayBind is not INSERT-only: the same array-bind path
    // carries an UPDATE statement per row just as well.
    [Fact]
    public void ExecuteArrayBind_UpdateStatements_UpdatesAllRows()
    {
        UsersTable u = new();
        using OracleConnection connection = (OracleConnection)_fixture.OpenConnection();
        using OracleTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            InsertInto(u, u.Id, u.Name, u.DepartmentId).Values(9201, "Before One", (short)1),
            transaction);
        connection.Execute(
            InsertInto(u, u.Id, u.Name, u.DepartmentId).Values(9202, "Before Two", (short)1),
            transaction);

        List<ISqlBuilder> statements =
        [
            Update(u).Set(u.Name == "After One").Where(u.Id == 9201),
            Update(u).Set(u.Name == "After Two").Where(u.Id == 9202),
        ];

        int updated = connection.ExecuteArrayBind(statements, transaction);

        Assert.Equal(2, updated);

        IEnumerable<string> names = connection.Query<string>(
            Select(u.Name).From(u).Where(u.Id >= 9201).OrderBy(u.Id),
            transaction);
        Assert.Equal(new[] { "After One", "After Two" }, names);

        transaction.Rollback();
    }

    private sealed class UserRow
    {
        public long Id { get; init; }

        public string? Name { get; init; }

        public int? Age { get; init; }
    }
}
