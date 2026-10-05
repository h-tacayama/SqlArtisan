using System.Data;
using System.Data.Common;
using Dapper;
using SqlArtisan.Analyzers;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;

namespace SqlArtisan.IntegrationTests.Tests;

/// <summary>
/// Live proof for the analyzer's SQL Server version bounds (#614): every entry in
/// <see cref="DialectMatrix.AllBounds"/> with a SQL Server bound must be accepted by a live
/// SQL Server 2025 engine, which the 2022 <see cref="MatrixSweepTestBase"/> lane cannot vouch for.
/// </summary>
[Trait("Engine", "SqlServer2025")]
public sealed class SqlServer2025BoundSweepTests : IClassFixture<SqlServer2025Fixture>
{
    private readonly SqlServer2025Fixture _fixture;

    public SqlServer2025BoundSweepTests(SqlServer2025Fixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SqlServerVersionBounds_AreAcceptedAt2025()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        List<string> failures = [];
        int checkedCount = 0;

        foreach ((MatrixKey key, VersionBounds bounds) in DialectMatrix.AllBounds)
        {
            if (bounds.SqlServer is null)
            {
                continue;
            }

            SweepCase? sweepCase = MatrixSweepCatalog.Cases.FirstOrDefault(c => c.Key.Equals(key));
            if (sweepCase is null)
            {
                failures.Add(
                    $"{Label(key)}: has a SQL Server bound but no sweep case to prove it live");
                continue;
            }

            checkedCount++;
            string? error = TryExecute(connection, sweepCase);
            if (error is not null)
            {
                failures.Add($"{Label(key)}: bound claims SQL Server 2025 accepts this, but the "
                    + $"engine rejected it: {error}");
            }
        }

        Assert.True(
            failures.Count == 0,
            $"SQL Server 2025 bound proof ({checkedCount} checked): {failures.Count} "
                + "mismatch(es):\n  "
                + string.Join("\n  ", failures));
    }

    private string? TryExecute(IDbConnection connection, SweepCase sweepCase)
    {
        try
        {
            if (sweepCase.Mutating)
            {
                using IDbTransaction transaction = connection.BeginTransaction();
                try
                {
                    connection.Execute(sweepCase.Build(_fixture.Dbms), transaction);
                }
                finally
                {
                    transaction.Rollback();
                }
            }
            else
            {
                connection.ExecuteScalar(sweepCase.Build(_fixture.Dbms));
            }

            return null;
        }
        catch (DbException ex)
        {
            return ex.Message.Split('\n')[0].Trim();
        }
        catch (Exception ex)
        {
            return ex.Message.Split('\n')[0].Trim();
        }
    }

    private static string Label(MatrixKey key) =>
        key.Arity is { } arity ? $"{key.MemberName}/arity{arity}" : key.MemberName;
}
