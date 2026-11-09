using System.Data;
using System.Data.Common;
using Dapper;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;

namespace SqlArtisan.IntegrationTests.Tests;

/// <summary>
/// Engine facts true at PostgreSQL 17 but not at the 16 baseline lane, each the live gate
/// for a record whose rejecting half the 16 lane (<see cref="PostgreSqlTests"/>) asserts,
/// and the 16 lane's DISTINCT-window facts re-run (#662).
/// </summary>
[Trait("Engine", "PostgreSql17")]
public sealed class PostgreSql17Tests : IClassFixture<PostgreSql17Fixture>
{
    private readonly PostgreSql17Fixture _fixture;

    public PostgreSql17Tests(PostgreSql17Fixture fixture)
    {
        _fixture = fixture;
    }

    // #582: public-api-design.md lists MERGE ... RETURNING (PostgreSQL 17) as not yet
    // offered; this is the engine side of that record.
    [Fact]
    public void MergeReturning_Executes()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            "MERGE INTO users AS t USING users AS s ON t.id = s.id "
                + "WHEN MATCHED THEN UPDATE SET name = s.name RETURNING t.id",
            transaction: transaction);
        transaction.Rollback();
    }

    // SQLA0102's twin (#582): WHEN NOT MATCHED BY SOURCE takes the 16 lane's rule for
    // the other two clauses — no branch after an unconditioned one of its clause.
    [Fact]
    public void MergeRepeatedBySourceBranch_NeedsAConditionOnTheEarlierBranch()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            "MERGE INTO users AS t USING (SELECT 1 AS id) AS s ON t.id = s.id "
                + "WHEN NOT MATCHED BY SOURCE AND t.id < 0 THEN DELETE "
                + "WHEN NOT MATCHED BY SOURCE THEN UPDATE SET name = t.name",
            transaction: transaction);

        Assert.ThrowsAny<DbException>(() => connection.Execute(
            "MERGE INTO users AS t USING (SELECT 1 AS id) AS s ON t.id = s.id "
                + "WHEN NOT MATCHED BY SOURCE THEN UPDATE SET name = t.name "
                + "WHEN NOT MATCHED BY SOURCE AND t.id < 0 THEN DELETE",
            transaction: transaction));
        transaction.Rollback();
    }

    // The 16 lane's #662 DISTINCT-window facts, re-run at 17: whether the engine
    // runs a window on a DISTINCT aggregate decides whether the library may reject the
    // pairing as valid on no dialect (ADR 0007).
    [Theory]
    [MemberData(
        nameof(DistinctAggregateWindows.Every),
        MemberType = typeof(DistinctAggregateWindows))]
    public void DistinctAggregateWindow_IsRejectedByTheEngine(string sql)
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() => connection.Execute(sql));
    }

    [Theory]
    [MemberData(
        nameof(DistinctAggregateWindows.FilteredEvery),
        MemberType = typeof(DistinctAggregateWindows))]
    public void FilteredDistinctAggregateWindow_IsRejectedByTheEngine(string sql)
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() => connection.Execute(sql));
    }

    [Theory]
    [MemberData(
        nameof(DistinctAggregateWindows.FilteredWithoutWindow),
        MemberType = typeof(DistinctAggregateWindows))]
    public void FilteredDistinctAggregate_IsAcceptedByTheEngine(string sql)
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Execute(sql);
    }

    // SQLA0102's twin (#800): the 16 lane's rejection of a qualified column in an
    // ORDER BY after UNION [ALL], re-run here, beside the position and alias that sort.
    [Theory]
    [InlineData("table", false)]
    [InlineData("table", true)]
    [InlineData("cte", false)]
    [InlineData("cte", true)]
    [InlineData("derived", false)]
    [InlineData("derived", true)]
    public void CompoundOrderBy_QualifiedColumn_IsRejected(string source, bool union)
    {
        using IDbConnection connection = _fixture.OpenConnection();

        DbException selected = Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            IntegrationTestBase.CompoundOverSource(source, union, "selected")).ToList());
        DbException unselected = Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            IntegrationTestBase.CompoundOverSource(source, union, "unselected")).ToList());

        Assert.Contains("missing FROM-clause entry", selected.Message);
        Assert.Contains("missing FROM-clause entry", unselected.Message);
        foreach (string partialKey in new[] { "onebranch", "shifted" })
        {
            DbException partial = Assert.ThrowsAny<DbException>(() => connection.Query<int>(
                IntegrationTestBase.CompoundOverSource(source, union, partialKey)).ToList());
            Assert.Contains("missing FROM-clause entry", partial.Message);
        }
        Assert.Equal(
            new[] { 4, 1 },
            connection.Query<int>(
                IntegrationTestBase.CompoundOverSource(source, union, "position")));
        Assert.Equal(
            new[] { 4, 1 },
            connection.Query<int>(IntegrationTestBase.CompoundOverSource(source, union, "alias")));
    }

    // SQLA0102 reports a key of the outer query after UNION too (#800); the position runs.
    [Fact]
    public void CompoundOrderBy_OuterQueryColumn_IsRejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() => connection.Query(
            IntegrationTestBase.CompoundOrderedByOuterColumn("outer", false)).ToList());
        Assert.Single(connection.Query(IntegrationTestBase.CompoundOrderedByOuterColumn("position", false)));
    }
}
