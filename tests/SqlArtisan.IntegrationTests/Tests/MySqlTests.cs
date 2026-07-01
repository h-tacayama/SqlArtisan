using System.Data;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;
using SqlArtisan.IntegrationTests.Schema;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Tests;

[Trait("Engine", "MySql")]
public sealed class MySqlTests : IntegrationTestBase, IClassFixture<MySqlFixture>
{
    private readonly MySqlFixture _fixture;

    public MySqlTests(MySqlFixture fixture) : base(fixture) => _fixture = fixture;

    [Fact]
    public void Pagination_LimitOffset_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        IEnumerable<int> ids = connection
            .Query<int>(Select(u.Id).From(u).OrderBy(u.Id).Limit(2).Offset(1));

        Assert.Equal(new[] { 2, 3 }, ids);
    }

    [Fact]
    public void Upsert_OnDuplicateKeyUpdate_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            InsertInto(u, u.Id, u.Name)
                .Values(1, "AliceUpdated")
                .OnDuplicateKeyUpdate(u.Name == Excluded(u.Name)),
            transaction);

        string name = connection
            .Query<string>(Select(u.Name).From(u).Where(u.Id == 1), transaction)
            .Single();

        Assert.Equal("AliceUpdated", name);
        transaction.Rollback();
    }

    [Fact]
    public void StringAggregation_GroupConcat_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        string concatenated = connection
            .Query<string>(Select(GroupConcat(u.Name)).From(u))
            .Single();

        Assert.Contains("Alice", concatenated);
    }

    [Fact]
    public void StringAggregation_GroupConcatSeparator_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // GROUP_CONCAT(name SEPARATOR ' | ') — the SEPARATOR clause is inlined.
        string concatenated = connection
            .Query<string>(Select(GroupConcat(u.Name, Separator(" | "))).From(u))
            .Single();

        Assert.Contains(" | ", concatenated);
    }

    [Fact]
    public void JsonExtract_ReadsScalar()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // JSON_EXTRACT(data, '$.name') — the path is inlined as a literal. MySQL
        // returns the scalar as a quoted JSON string (e.g. "Alice"), so the value
        // is asserted with Contains rather than an exact match. (MySQL's ->/->>
        // operators require a *literal* path, which the parameterized operator API
        // does not emit, so they are covered by the SQLite/PostgreSQL lanes.)
        string name = connection
            .Query<string>(Select(JsonExtract(u.Data, "$.name")).From(u).Where(u.Id == 1))
            .Single();

        Assert.Contains("Alice", name);
    }
}
