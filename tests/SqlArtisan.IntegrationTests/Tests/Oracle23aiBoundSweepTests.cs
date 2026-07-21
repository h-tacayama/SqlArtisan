using System.Data;
using System.Data.Common;
using Dapper;
using SqlArtisan.Analyzers;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;

namespace SqlArtisan.IntegrationTests.Tests;

/// <summary>
/// Live proof for the analyzer's Oracle version bounds (#263): every matrix
/// entry with an Oracle bound (<see cref="DialectMatrix.AllBounds"/>) must be
/// accepted by a live Oracle 23ai engine — the version-refined verdict a
/// declared <c>sqlartisan_target_version = 23</c> resolves to, which can
/// differ from the plain matrix bool the ordinary 21c
/// <see cref="MatrixSweepTestBase"/> lane checks (e.g. WITH RECURSIVE is
/// bool-false at 21c but bound-true at 23). Expectations derive from
/// <see cref="DialectMatrix.AllBounds"/> directly, so a future Oracle bound is
/// pulled into this lane automatically.
/// </summary>
[Trait("Engine", "Oracle23ai")]
public sealed class Oracle23aiBoundSweepTests : IClassFixture<Oracle23aiFixture>
{
    private readonly Oracle23aiFixture _fixture;

    public Oracle23aiBoundSweepTests(Oracle23aiFixture fixture) => _fixture = fixture;

    [Fact]
    public void OracleVersionBounds_AreAcceptedAt23()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        List<string> failures = [];
        int checkedCount = 0;

        foreach ((MatrixKey key, VersionBounds bounds) in DialectMatrix.AllBounds)
        {
            if (bounds.Oracle is null)
            {
                continue;
            }

            SweepCase? sweepCase = MatrixSweepCatalog.Cases.FirstOrDefault(c => c.Key.Equals(key));
            if (sweepCase is null)
            {
                failures.Add($"{Label(key)}: has an Oracle bound but no sweep case to prove it live");
                continue;
            }

            checkedCount++;
            string? error = TryExecute(connection, sweepCase);
            if (error is not null)
            {
                failures.Add($"{Label(key)}: bound claims Oracle 23 accepts this, but the engine rejected it: {error}");
            }
        }

        Assert.True(
            failures.Count == 0,
            $"Oracle 23ai bound proof ({checkedCount} checked): {failures.Count} mismatch(es):\n  "
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
    }

    private static string Label(MatrixKey key) => key.Arity is { } arity ? $"{key.MemberName}/arity{arity}" : key.MemberName;
}
