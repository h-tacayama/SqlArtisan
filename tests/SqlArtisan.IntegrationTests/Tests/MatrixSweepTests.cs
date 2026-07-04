using System.Data;
using System.Data.Common;
using Dapper;
using SqlArtisan.Analyzers;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;

namespace SqlArtisan.IntegrationTests.Tests;

/// <summary>
/// The dialect sweep (#93 step 2): runs every <see cref="MatrixSweepCatalog"/>
/// statement against this engine and asserts the outcome matches the analyzer's
/// <see cref="DialectMatrix"/> verdict in BOTH directions — a supported entry the
/// engine rejects means the matrix under-claims (the analyzer's "silence =
/// verified" promise is broken); an unsupported entry the engine accepts means
/// the matrix over-restricts (the analyzer warns on valid SQL — a false
/// positive in the wild). Failures aggregate so one run reports every
/// mismatched construct for the engine.
/// </summary>
public abstract class MatrixSweepTestBase
{
    private readonly IDatabaseFixture _fixture;

    protected MatrixSweepTestBase(IDatabaseFixture fixture) => _fixture = fixture;

    /// <summary>Idempotent per-engine prerequisites (full-text artifacts) the sweep statements need.</summary>
    protected virtual void PrepareEngine(IDbConnection connection)
    {
    }

    [Fact]
    public void DialectMatrix_MatchesLiveEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        PrepareEngine(connection);

        TargetDbms target = ToTarget(_fixture.Dbms);
        List<string> failures = [];
        int accepted = 0;
        int rejected = 0;
        int skipped = 0;

        foreach (SweepCase sweepCase in MatrixSweepCatalog.Cases)
        {
            if (!DialectMatrix.TryGetEntry(
                    sweepCase.Key.MemberName, sweepCase.Key.Arity, out DbmsSupport support, out _))
            {
                failures.Add($"{Label(sweepCase)}: has no matrix entry to check against");
                continue;
            }

            bool expected = support.IsSupported(target);
            if (expected && sweepCase.PositiveSkips?.ContainsKey(_fixture.Dbms) == true)
            {
                skipped++;
                continue;
            }

            if (!expected && sweepCase.NegativeSkips?.ContainsKey(_fixture.Dbms) == true)
            {
                skipped++;
                continue;
            }

            string? error = TryExecute(connection, sweepCase, out bool isEngineRejection);
            if (expected && error is not null)
            {
                failures.Add($"{Label(sweepCase)}: matrix says SUPPORTED, engine rejected: {error}");
            }
            else if (!expected && error is null)
            {
                failures.Add($"{Label(sweepCase)}: matrix says UNSUPPORTED, engine accepted");
            }
            else if (!expected && !isEngineRejection)
            {
                // An infrastructure failure (timeout, dropped connection) must not
                // count as the engine rejecting the construct's grammar.
                failures.Add($"{Label(sweepCase)}: matrix says UNSUPPORTED, but the failure was not a database rejection: {error}");
            }
            else if (expected)
            {
                accepted++;
            }
            else
            {
                rejected++;
            }
        }

        Assert.True(
            failures.Count == 0,
            $"{_fixture.Dbms} dialect sweep ({accepted} accepts, {rejected} rejects, {skipped} skips all "
                + $"as expected): {failures.Count} matrix<->engine mismatches:\n  "
                + string.Join("\n  ", failures));
    }

    private string? TryExecute(IDbConnection connection, SweepCase sweepCase, out bool isEngineRejection)
    {
        isEngineRejection = false;
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
            isEngineRejection = true;
            return ex.Message.Split('\n')[0].Trim();
        }
        catch (Exception ex)
        {
            return ex.Message.Split('\n')[0].Trim();
        }
    }

    private static string Label(SweepCase sweepCase) => sweepCase.Key.Arity is { } arity
        ? $"{sweepCase.Key.MemberName}/arity{arity}"
        : sweepCase.Key.MemberName;

    private static TargetDbms ToTarget(Dbms dbms) => dbms switch
    {
        Dbms.MySql => TargetDbms.MySql,
        Dbms.Oracle => TargetDbms.Oracle,
        Dbms.PostgreSql => TargetDbms.PostgreSql,
        Dbms.Sqlite => TargetDbms.Sqlite,
        Dbms.SqlServer => TargetDbms.SqlServer,
        _ => throw new ArgumentOutOfRangeException(nameof(dbms), dbms, message: null),
    };
}

/// <summary>
/// Engine-independent gates for the sweep catalog itself: every matrix entry is
/// either swept or explicitly excluded with a reason, and the catalog contains
/// no stale keys the matrix no longer has. Runs without a database.
/// </summary>
public sealed class MatrixSweepCatalogTests
{
    [Fact]
    public void Catalog_CoversEveryMatrixEntry()
    {
        HashSet<MatrixKey> covered = MatrixSweepCatalog.Cases.Select(c => c.Key).ToHashSet();
        Assert.Equal(MatrixSweepCatalog.Cases.Count, covered.Count); // no duplicate case keys

        List<string> missing = DialectMatrix.AllKeys
            .Where(key => !covered.Contains(key) && !MatrixSweepCatalog.ExcludedEntries.ContainsKey(key))
            .Select(key => key.Arity is { } arity ? $"{key.MemberName}/arity{arity}" : key.MemberName)
            .ToList();

        Assert.True(
            missing.Count == 0,
            $"{missing.Count} matrix entries have neither a sweep case nor a documented exclusion:\n  "
                + string.Join("\n  ", missing));
    }

    [Fact]
    public void Catalog_HasNoKeysOutsideTheMatrix()
    {
        HashSet<MatrixKey> matrixKeys = DialectMatrix.AllKeys.ToHashSet();

        List<string> stale = MatrixSweepCatalog.Cases.Select(c => c.Key)
            .Concat(MatrixSweepCatalog.ExcludedEntries.Keys)
            .Where(key => !matrixKeys.Contains(key))
            .Select(key => key.Arity is { } arity ? $"{key.MemberName}/arity{arity}" : key.MemberName)
            .ToList();

        Assert.True(
            stale.Count == 0,
            $"{stale.Count} sweep keys have no matrix entry (stale after a matrix edit?):\n  "
                + string.Join("\n  ", stale));
    }
}

[Trait("Engine", "Sqlite")]
public sealed class SqliteMatrixSweepTests : MatrixSweepTestBase, IClassFixture<SqliteFixture>
{
    public SqliteMatrixSweepTests(SqliteFixture fixture) : base(fixture)
    {
    }

    protected override void PrepareEngine(IDbConnection connection)
    {
        // The FTS5 virtual table behind the SQLite Match(...) shape.
        try
        {
            connection.Execute("CREATE VIRTUAL TABLE sweep_fts USING fts5(name)");
            connection.Execute("INSERT INTO sweep_fts(name) VALUES ('alice database')");
        }
        catch
        {
            // Already created by an earlier run against the same database.
        }
    }
}

[Trait("Engine", "PostgreSql")]
public sealed class PostgreSqlMatrixSweepTests : MatrixSweepTestBase, IClassFixture<PostgreSqlFixture>
{
    public PostgreSqlMatrixSweepTests(PostgreSqlFixture fixture) : base(fixture)
    {
    }
}

[Trait("Engine", "MySql")]
public sealed class MySqlMatrixSweepTests : MatrixSweepTestBase, IClassFixture<MySqlFixture>
{
    public MySqlMatrixSweepTests(MySqlFixture fixture) : base(fixture)
    {
    }

    protected override void PrepareEngine(IDbConnection connection)
    {
        // MATCH ... AGAINST needs a FULLTEXT index matching the searched columns.
        try
        {
            connection.Execute("CREATE FULLTEXT INDEX sweep_ft_name ON users(name)");
        }
        catch
        {
            // Already created by an earlier run against the same database.
        }
    }
}

[Trait("Engine", "SqlServer")]
public sealed class SqlServerMatrixSweepTests : MatrixSweepTestBase, IClassFixture<SqlServerFixture>
{
    public SqlServerMatrixSweepTests(SqlServerFixture fixture) : base(fixture)
    {
    }
}

[Trait("Engine", "Oracle")]
public sealed class OracleMatrixSweepTests : MatrixSweepTestBase, IClassFixture<OracleFixture>
{
    public OracleMatrixSweepTests(OracleFixture fixture) : base(fixture)
    {
    }
}
