using System.Data;
using System.Threading.Tasks;
using Dapper;
using SqlArtisan;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;
using SqlArtisan.IntegrationTests.Schema;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Tests;

[Trait("Engine", "Oracle")]
public sealed class OracleTests : IntegrationTestBase, IClassFixture<OracleFixture>
{
    private readonly OracleFixture _fixture;

    public OracleTests(OracleFixture fixture) : base(fixture) => _fixture = fixture;

    [Fact]
    public void Pagination_OffsetFetch_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        IEnumerable<int> ids = connection
            .Query<int>(Select(u.Id).From(u).OrderBy(u.Id).OffsetRows(1).FetchNext(2));

        Assert.Equal(new[] { 2, 3 }, ids);
    }

    [Fact] // Regression for #165: a re-aliased CTE column now emits a bare alias,
           // so it resolves on Oracle (previously ORA-00904).
    public void Cte_AliasedColumn_Executes()
    {
        UsersTable u = new();
        Cte seniors = new("seniors");
        using IDbConnection connection = _fixture.OpenConnection();

        long count = Convert.ToInt64(connection.ExecuteScalar(
            With(seniors.As(Select(u.Id.As(seniors.Column("id"))).From(u).Where(u.Age >= 40)))
                .Select(Count(seniors.Column("id")))
                .From(seniors)));

        Assert.Equal(2, count);
    }

    // Oracle XE 21c (the Testcontainers image) has no native boolean type
    // (is_active is NUMBER(1)), and binding a C# bool there is a driver concern
    // rather than a SqlArtisan one. The four engines with a native boolean type
    // cover the round-trip.
    [Fact(Skip = "Oracle XE 21c has no native boolean type; is_active is NUMBER(1).")]
    public override void EdgeCase_Boolean_RoundTrip()
    {
    }

    [Fact(Skip = "Oracle has no multi-row VALUES; it uses INSERT ALL instead.")]
    public override void MultiRowValues_Executes()
    {
    }

    [Fact] // RETURNING ... INTO binds the affected columns into typed output
           // parameters; ExecuteReturningInto returns the populated bag so the
           // values can be read back after execution (the Oracle-specific form).
    public void ReturningInto_OnDelete_BindsOutputParameter()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        // Carol is id 3; deleting that one row returns its id and name into the
        // numeric and string output parameters respectively.
        DynamicParameters outputs = connection.ExecuteReturningInto(
            DeleteFrom(u).Where(u.Id == 3)
                .Returning(u.Id, u.Name)
                .Into(new("outId", DbType.Int32), new("outName", DbType.String, 100)),
            transaction);

        Assert.Equal(3, Convert.ToInt32(outputs.Get<object>("outId")!.ToString()));
        Assert.Equal("Carol", outputs.Get<string>("outName"));
        transaction.Rollback();
    }

    [Fact] // The async counterpart of ReturningInto: ExecuteReturningIntoAsync
           // runs the statement and returns the populated bag.
    public async Task ReturningIntoAsync_OnDelete_BindsOutputParameter()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        DynamicParameters outputs = await connection.ExecuteReturningIntoAsync(
            DeleteFrom(u).Where(u.Id == 2)
                .Returning(u.Id).Into(new OutputParameter("outId", DbType.Int32)),
            transaction);

        Assert.Equal(2, Convert.ToInt32(outputs.Get<object>("outId")!.ToString()));
        transaction.Rollback();
    }

    [Fact]
    public void Sequence_NextvalCurrval_Executes()
    {
        UsersTable u = new();
        var seq = Sequence("test_seq");
        using IDbConnection connection = _fixture.OpenConnection();

        // Oracle's pseudo-column form: test_seq.NEXTVAL / test_seq.CURRVAL.
        long next = Convert.ToInt64(connection.ExecuteScalar(
            Select(seq.Nextval).From(u).Where(u.Id == 1)));
        long current = Convert.ToInt64(connection.ExecuteScalar(
            Select(seq.Currval).From(u).Where(u.Id == 1)));

        Assert.Equal(next, current);
    }

    [Fact]
    public void Merge_UpsertViaMerge_Executes()
    {
        UsersTable t = new("t");
        UsersTable s = new("s");
        UsersTable c = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            MergeInto(t)
                .Using(s)
                .On(t.Id == s.Id)
                .WhenMatched().ThenUpdateSet(t.Name == s.Name)
                .WhenNotMatched().ThenInsert(c.Id, c.Name).Values(s.Id, s.Name),
            transaction);

        long count = Convert.ToInt64(connection.ExecuteScalar(Select(Count(c.Id)).From(c), transaction));

        Assert.Equal(5, count);
        transaction.Rollback();
    }

    [Fact]
    public void StringAggregation_Listagg_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        string concatenated = connection
            .Query<string>(Select(Listagg(u.Name, ",").WithinGroup(OrderBy(u.Name))).From(u))
            .Single();

        Assert.Contains("Alice", concatenated);
    }

    [Fact]
    public void SetOperator_Minus_Executes()
    {
        UsersTable u = new();
        OrdersTable o = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // Oracle spells EXCEPT as MINUS: users {1..5} MINUS {1,2,3,5} = {4}.
        int id = connection
            .Query<int>(Select(u.Id).From(u).Minus.Select(o.UserId).From(o))
            .Single();

        Assert.Equal(4, id);
    }
}
