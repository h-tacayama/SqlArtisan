using System.Collections.Concurrent;
using System.Data;
using CsCheck;
using SqlArtisan.IntegrationTests.Infrastructure;
using SqlArtisan.IntegrationTests.Randomized;

namespace SqlArtisan.IntegrationTests.Tests;

/// <summary>
/// Runs each <c>select.cases.csv</c> row of this lane's engine: CsCheck generates queries
/// that fit the row, and every relation in <see cref="CaseChecker"/> must hold on the live
/// engine. A failure shrinks to a smaller query of the same row before it is reported.
/// </summary>
public abstract class CombinatorialSelectTestBase
{
    private const int CasesPerRow = 3;
    private const string SeedVariable = "SQLARTISAN_RANDOMIZED_SEED";
    private const string ShrinkVariable = "SQLARTISAN_RANDOMIZED_SHRINK_SECONDS";
    private const int DefaultShrinkSeconds = 20;

    private static readonly ConcurrentDictionary<Type, bool> s_shrunkLanes = new();

    private readonly IDatabaseFixture _fixture;

    protected CombinatorialSelectTestBase(IDatabaseFixture fixture)
    {
        _fixture = fixture;
        RandomSchema.EnsureCreated(fixture);
    }

    [Fact]
    public void NegativeControl_NegatedWhere_IsDetected()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        int compared = 0;
        int detected = 0;
        foreach (CaseRow row in CaseTable.Rows.Where(r => r.Dbms == _fixture.Dbms))
        {
            CaseChecker checker = new(connection, row);
            QuerySpec spec = QueryGenerators.For(row)
                .Generate(PCG.Parse(Seeds(row)[0]), null!, out _);
            if (checker.NegatedWhereDiffers(spec) is { } differs)
            {
                compared++;
                detected += differs ? 1 : 0;
            }
        }

        // Where(c) and Where(Not(c)) can still agree by chance, on two empty results.
        Assert.True(
            compared > 0 && detected * 2 >= compared,
            $"{_fixture.Dbms}: the comparison told {detected} of {compared} negated WHERE "
                + "clauses apart from the original; a broken comparison tells none apart.");
    }

    protected void RunRow(int number, string factors)
    {
        CaseRow row = CaseTable.Row(number);
        Assert.Equal(row.Summary, factors);
        Gen<QuerySpec> gen = QueryGenerators.For(row);
        using IDbConnection connection = _fixture.OpenConnection();
        CaseChecker checker = new(connection, row);
        foreach (string seed in Seeds(row))
        {
            QuerySpec spec = gen.Generate(PCG.Parse(seed), null!, out _);
            try
            {
                checker.Check(spec);
            }
            catch (RandomizedCaseException ex)
            {
                Assert.Fail($"select.cases.csv row {number} ({factors}), CsCheck seed {seed}\n"
                    + $"Reproduce: {SeedVariable}={seed} and filter to this row's test case.\n"
                    + ex.Message + Shrink(gen, checker, seed));
            }
        }
    }

    private static string[] Seeds(CaseRow row) => Environment.GetEnvironmentVariable(SeedVariable)
        is { Length: > 0 } seed
        ? [seed]
        : [.. Enumerable.Range(0, CasesPerRow)
            .Select(i => new PCG((uint)row.Number, (ulong)i).ToString())];

    // CsCheck shrinks by searching other seeds of the same generator for a smaller failing
    // query, and each candidate costs a dozen round trips, so by default only a lane's first
    // failure searches; the variable sets a budget for every failure instead.
    private string Shrink(Gen<QuerySpec> gen, CaseChecker checker, string seed)
    {
        int seconds;
        if (int.TryParse(Environment.GetEnvironmentVariable(ShrinkVariable), out int s))
        {
            seconds = s;
        }
        else if (s_shrunkLanes.TryAdd(GetType(), true))
        {
            seconds = DefaultShrinkSeconds;
        }
        else
        {
            return "\n\nSHRINK skipped: an earlier row of this lane searched; set "
                + $"{ShrinkVariable} to search here.";
        }

        if (seconds <= 0)
        {
            return "";
        }

        try
        {
            gen.Sample(
                checker.Check,
                seed: seed,
                time: seconds,
                threads: 1,
                print: q => q.ToCSharp());
            return "\n\nSHRINK: the failure did not reproduce from its seed.";
        }
        catch (CsCheckException ex)
        {
            return $"\n\nSHRUNK ({seconds}s search): {ex.Message.Split('\n')[0]}\n"
                + (ex.InnerException?.Message ?? ex.Message);
        }
    }
}

[Trait("Engine", "Sqlite")]
public sealed class SqliteCombinatorialSelectTests
    : CombinatorialSelectTestBase, IClassFixture<SqliteFixture>
{
    public SqliteCombinatorialSelectTests(SqliteFixture fixture) : base(fixture)
    {
    }

    public static TheoryData<int, string> Rows => CaseTable.TheoryRows(Dbms.Sqlite);

    [Theory]
    [MemberData(nameof(Rows))]
    public void CaseRow_GeneratedQueries_HoldEveryRelation(int row, string factors) =>
        RunRow(row, factors);
}

[Trait("Engine", "PostgreSql")]
public sealed class PostgreSqlCombinatorialSelectTests
    : CombinatorialSelectTestBase, IClassFixture<PostgreSqlFixture>
{
    public PostgreSqlCombinatorialSelectTests(PostgreSqlFixture fixture) : base(fixture)
    {
    }

    public static TheoryData<int, string> Rows => CaseTable.TheoryRows(Dbms.PostgreSql);

    [Theory]
    [MemberData(nameof(Rows))]
    public void CaseRow_GeneratedQueries_HoldEveryRelation(int row, string factors) =>
        RunRow(row, factors);
}

[Trait("Engine", "PostgreSql17")]
public sealed class PostgreSql17CombinatorialSelectTests
    : CombinatorialSelectTestBase, IClassFixture<PostgreSql17Fixture>
{
    public PostgreSql17CombinatorialSelectTests(PostgreSql17Fixture fixture) : base(fixture)
    {
    }

    public static TheoryData<int, string> Rows => CaseTable.TheoryRows(Dbms.PostgreSql);

    [Theory]
    [MemberData(nameof(Rows))]
    public void CaseRow_GeneratedQueries_HoldEveryRelation(int row, string factors) =>
        RunRow(row, factors);
}

[Trait("Engine", "MySql")]
public sealed class MySqlCombinatorialSelectTests
    : CombinatorialSelectTestBase, IClassFixture<MySqlFixture>
{
    public MySqlCombinatorialSelectTests(MySqlFixture fixture) : base(fixture)
    {
    }

    public static TheoryData<int, string> Rows => CaseTable.TheoryRows(Dbms.MySql);

    [Theory]
    [MemberData(nameof(Rows))]
    public void CaseRow_GeneratedQueries_HoldEveryRelation(int row, string factors) =>
        RunRow(row, factors);
}

[Trait("Engine", "SqlServer")]
public sealed class SqlServerCombinatorialSelectTests
    : CombinatorialSelectTestBase, IClassFixture<SqlServerFixture>
{
    public SqlServerCombinatorialSelectTests(SqlServerFixture fixture) : base(fixture)
    {
    }

    public static TheoryData<int, string> Rows => CaseTable.TheoryRows(Dbms.SqlServer);

    [Theory]
    [MemberData(nameof(Rows))]
    public void CaseRow_GeneratedQueries_HoldEveryRelation(int row, string factors) =>
        RunRow(row, factors);
}

[Trait("Engine", "SqlServer2025")]
public sealed class SqlServer2025CombinatorialSelectTests
    : CombinatorialSelectTestBase, IClassFixture<SqlServer2025Fixture>
{
    public SqlServer2025CombinatorialSelectTests(SqlServer2025Fixture fixture) : base(fixture)
    {
    }

    public static TheoryData<int, string> Rows => CaseTable.TheoryRows(Dbms.SqlServer);

    [Theory]
    [MemberData(nameof(Rows))]
    public void CaseRow_GeneratedQueries_HoldEveryRelation(int row, string factors) =>
        RunRow(row, factors);
}

[Trait("Engine", "Oracle")]
public sealed class OracleCombinatorialSelectTests
    : CombinatorialSelectTestBase, IClassFixture<OracleFixture>
{
    public OracleCombinatorialSelectTests(OracleFixture fixture) : base(fixture)
    {
    }

    public static TheoryData<int, string> Rows => CaseTable.TheoryRows(Dbms.Oracle);

    [Theory]
    [MemberData(nameof(Rows))]
    public void CaseRow_GeneratedQueries_HoldEveryRelation(int row, string factors) =>
        RunRow(row, factors);
}

[Trait("Engine", "Oracle23ai")]
public sealed class Oracle23aiCombinatorialSelectTests
    : CombinatorialSelectTestBase, IClassFixture<Oracle23aiFixture>
{
    public Oracle23aiCombinatorialSelectTests(Oracle23aiFixture fixture) : base(fixture)
    {
    }

    public static TheoryData<int, string> Rows => CaseTable.TheoryRows(Dbms.Oracle);

    [Theory]
    [MemberData(nameof(Rows))]
    public void CaseRow_GeneratedQueries_HoldEveryRelation(int row, string factors) =>
        RunRow(row, factors);
}
