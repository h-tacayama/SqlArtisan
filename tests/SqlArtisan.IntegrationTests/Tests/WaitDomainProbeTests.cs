using System.Data;
using System.Diagnostics;
using Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;

namespace SqlArtisan.IntegrationTests.Tests;

/// <summary>
/// TEMPORARY probe for #483 — measures which <c>FOR UPDATE WAIT n</c> values a
/// live Oracle engine accepts, deciding ADR 0012 condition 1 for a negative
/// <c>Sql.Wait(seconds)</c>. Each probe deliberately fails so its report reaches
/// the CI log; delete this file once both Oracle lanes have reported.
/// </summary>
internal static class WaitDomainProbe
{
    internal static string Run(IDatabaseFixture fixture)
    {
        List<string> report = [];
        using IDbConnection connection = fixture.OpenConnection();

        foreach (int seconds in new[] { 3, 0, -1, -2, int.MaxValue, int.MinValue })
        {
            report.Add($"WAIT {seconds} -> {Probe(fixture, connection, seconds)}");
        }

        report.Add($"WAIT -1 on a locked row -> {LockedRowProbe(fixture)}");
        return string.Join("\n  ", report);
    }

    private static string Probe(IDatabaseFixture fixture, IDbConnection connection, int seconds)
    {
        using IDbTransaction transaction = connection.BeginTransaction();
        try
        {
            connection.ExecuteScalar(
                $"SELECT age FROM users WHERE id = 1 FOR UPDATE WAIT {seconds}",
                transaction: transaction);
            return "ACCEPTED";
        }
        catch (Exception exception)
        {
            return $"REJECTED: {FirstLine(exception)}";
        }
        finally
        {
            transaction.Rollback();
        }
    }

    /// <summary>
    /// Distinguishes the three tolerant readings the issue raises: an immediate
    /// error (NOWAIT-like), a bounded wait, or no response at all (unbounded).
    /// </summary>
    private static string LockedRowProbe(IDatabaseFixture fixture)
    {
        using IDbConnection locker = fixture.OpenConnection();
        using IDbTransaction lockTransaction = locker.BeginTransaction();
        locker.ExecuteScalar(
            "SELECT age FROM users WHERE id = 1 FOR UPDATE",
            transaction: lockTransaction);

        Stopwatch stopwatch = Stopwatch.StartNew();
        Task<string> waiter = Task.Run(() =>
        {
            using IDbConnection connection = fixture.OpenConnection();
            try
            {
                connection.ExecuteScalar(
                    "SELECT age FROM users WHERE id = 1 FOR UPDATE WAIT -1",
                    commandTimeout: 10);
                return "ACCEPTED (returned the row)";
            }
            catch (Exception exception)
            {
                return $"REJECTED: {FirstLine(exception)}";
            }
        });

        string outcome = waiter.Wait(TimeSpan.FromSeconds(60))
            ? $"{waiter.Result} after {stopwatch.Elapsed.TotalSeconds:F1}s"
            : "no response within 60s (unbounded wait)";

        lockTransaction.Rollback();
        return outcome;
    }

    private static string FirstLine(Exception exception) =>
        exception.Message.Split('\n')[0].Trim();
}

[Trait("Engine", "Oracle")]
public sealed class WaitDomainProbeOracleTests : IClassFixture<OracleFixture>
{
    private readonly OracleFixture _fixture;

    public WaitDomainProbeOracleTests(OracleFixture fixture) => _fixture = fixture;

    [Fact]
    public void WaitDomain_Probe() =>
        Assert.Fail($"#483 probe — Oracle XE 21c:\n  {WaitDomainProbe.Run(_fixture)}");
}

[Trait("Engine", "Oracle23ai")]
public sealed class WaitDomainProbeOracle23aiTests : IClassFixture<Oracle23aiFixture>
{
    private readonly Oracle23aiFixture _fixture;

    public WaitDomainProbeOracle23aiTests(Oracle23aiFixture fixture) => _fixture = fixture;

    [Fact]
    public void WaitDomain_Probe() =>
        Assert.Fail($"#483 probe — Oracle Free 23ai:\n  {WaitDomainProbe.Run(_fixture)}");
}
