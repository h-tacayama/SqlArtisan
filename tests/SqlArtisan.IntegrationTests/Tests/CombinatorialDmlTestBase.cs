using System.Collections.Concurrent;
using System.Data;
using CsCheck;
using SqlArtisan.IntegrationTests.Infrastructure;
using SqlArtisan.IntegrationTests.Randomized;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Tests;

/// <summary>
/// Runs each <c>insert</c>, <c>update</c> and <c>delete.cases.csv</c> row of this lane's engine:
/// every relation in <see cref="DmlChecker"/> must hold for the statements CsCheck generates,
/// each in a transaction it rolls back. A lane's first failure shrinks before it is reported.
/// </summary>
public abstract class CombinatorialDmlTestBase
{
    private const string ShrinkVariable = "SQLARTISAN_RANDOMIZED_SHRINK_SECONDS";
    private const int DefaultShrinkSeconds = 20;

    // A statement that changes no row passes every relation unseen, so a lane whose
    // generators drift toward those tests little; the generators retry them (DmlGenerators).
    private const int MinimumChangingPercent = 75;

    private static readonly ConcurrentDictionary<Type, bool> s_shrunkLanes = new();

    private readonly IDatabaseFixture _fixture;

    protected CombinatorialDmlTestBase(IDatabaseFixture fixture)
    {
        _fixture = fixture;
        RandomSchema.EnsureCreated(fixture);
    }

    [Fact]
    public void NegativeControl_NegatedWhere_IsDetected()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        DmlChecker checker = new(connection, _fixture.Dbms);
        int compared = 0;
        int detected = 0;
        foreach (DmlSpec spec in FirstSeedSpecs())
        {
            if (DmlPlanner.NegateWhere(spec) is not { } negated)
            {
                continue;
            }

            DmlOutcome kept = checker.Run(spec);
            DmlOutcome flipped = checker.Run(negated);
            if (kept.Users is null || flipped.Users is null)
            {
                Assert.Fail(kept.Show("KEPT") + flipped.Show("NEGATED"));
            }

            compared++;
            detected += DmlOutcome.Same(kept.Users!, flipped.Users!) ? 0 : 1;
        }

        // Where(c) and Where(Not(c)) can still agree by chance, on two empty matches.
        Assert.True(
            compared > 0 && detected * 2 >= compared,
            $"{_fixture.Dbms}: the state told {detected} of {compared} negated WHERE clauses "
                + "apart from the original; a broken comparison tells none apart.");
    }

    [Fact]
    public void MutationControl_ReferenceLeavingOneRow_IsDetected()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        DmlChecker checker = new(connection, _fixture.Dbms);
        int compared = 0;
        int detected = 0;
        foreach (DmlSpec spec in FirstSeedSpecs())
        {
            DmlExpectation mutated;
            try
            {
                mutated = DmlReference.Evaluate(spec, mutated: true);
            }
            catch (ReferenceUnavailableException)
            {
                continue;
            }

            DmlOutcome actual = checker.Run(spec);
            Assert.True(actual.Users is not null, actual.Show("BASE"));
            compared++;
            detected += DmlOutcome.Same(mutated.Users, actual.Users!) ? 0 : 1;
        }

        Assert.True(
            compared > 0 && detected * 2 >= compared,
            $"{_fixture.Dbms}: the state comparison caught {detected} of {compared} references "
                + "that skip the lowest matched row; a broken comparison catches none.");
    }

    [Fact]
    public void GeneratedStatements_MostlyChangeRows()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        DmlChecker checker = new(connection, _fixture.Dbms);
        List<DmlSpec> specs = [.. FirstSeedSpecs()];
        int changing = specs.Count(spec => checker.Run(spec).ChangedRows);

        Assert.True(
            changing * 100 >= specs.Count * MinimumChangingPercent,
            $"{_fixture.Dbms}: {changing} of {specs.Count} generated statements changed a row, "
                + $"under the {MinimumChangingPercent}% floor.");
    }

    protected void RunRow(string model, int number, string factors)
    {
        DmlCaseRow row = DmlCaseTable.Row(model, number);
        Assert.Equal(row.Summary, factors);
        Gen<DmlSpec> gen = DmlGenerators.For(row);
        using IDbConnection connection = _fixture.OpenConnection();
        DmlChecker checker = new(connection, row.Dbms);
        foreach (string seed in CaseSeeds.For(row.Number))
        {
            DmlSpec spec = gen.Generate(PCG.Parse(seed), null!, out _);
            try
            {
                checker.Check(spec);
            }
            catch (RandomizedCaseException ex)
            {
                Assert.Fail($"{model}.cases.csv row {number} ({factors}), CsCheck seed {seed}\n"
                    + $"Reproduce: {CaseSeeds.Variable}={seed} and filter to this row's test case."
                    + "\n" + ex.Message + Shrink(gen, checker, seed));
            }
        }
    }

    /// <summary>Runs <paramref name="statement"/> in a rolled-back transaction and returns
    /// <paramref name="read"/>'s rows, read inside it.</summary>
    private protected (Outcome Statement, List<string> Read) RunAndRead(
        PlannedStatement statement,
        PlannedStatement read)
    {
        using IDbConnection connection = _fixture.OpenConnection();
        StatementRunner runner = new(connection, _fixture.Dbms);
        return runner.RunInRolledBackTransaction(transaction =>
            (runner.Run(statement, transaction), runner.Run(read, transaction).Rows!));
    }

    private IEnumerable<DmlSpec> FirstSeedSpecs() => DmlCaseTable.Rows
        .Where(r => r.Dbms == _fixture.Dbms)
        .Select(r => DmlGenerators.For(r)
            .Generate(PCG.Parse(CaseSeeds.For(r.Number)[0]), null!, out _));

    // As CombinatorialSelectTestBase.Shrink: only a lane's first failure searches by default.
    private string Shrink(Gen<DmlSpec> gen, DmlChecker checker, string seed)
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
public sealed class SqliteCombinatorialDmlTests
    : CombinatorialDmlTestBase, IClassFixture<SqliteFixture>
{
    public SqliteCombinatorialDmlTests(SqliteFixture fixture) : base(fixture)
    {
    }

    // Seed ages 30, 25, 30: the starting rows give running sums 30, 55, 85; SQLite 3.50.4
    // reads the rows it has already updated, which DmlGenerators never builds.
    [Fact]
    public void UpdateSet_Sqlite_CorrelatedSubqueryOverTarget_ReadsUpdatedRows()
    {
        RandomUsersTable u = new("u");
        RandomUsersTable u2 = new("u2");
        (Outcome update, List<string> ages) = RunAndRead(
            new PlannedStatement(
                "UPDATE",
                "Update(u).Set(u.Age == Select(Sum(u2.Age)).From(u2).Where(u2.Id <= u.Id))"
                    + ".Where(u.Id <= 3)",
                () => Update(u)
                    .Set(u.Age == Select(Sum(u2.Age)).From(u2).Where(u2.Id <= u.Id))
                    .Where(u.Id <= 3)),
            new PlannedStatement(
                "READ",
                "Select(u.Id, u.Age).From(u).Where(u.Id <= 3).OrderBy(u.Id)",
                () => Select(u.Id, u.Age).From(u).Where(u.Id <= 3).OrderBy(u.Id)));

        Assert.Null(update.Error);
        Assert.Equal(["1|30", "2|55", "3|115"], ages);
    }

    // The 28 seed rows: the starting rows give 28 three times; SQLite 3.50.4 counts the rows
    // left after each deletion.
    [Fact]
    public void DeleteReturning_Sqlite_SubqueryOverTarget_ReadsDeletedRows()
    {
        RandomUsersTable u = new("u");
        RandomUsersTable u2 = new("u2");
        PlannedStatement delete = new(
            "DELETE",
            "DeleteFrom(u).Where(u.Id <= 3).Returning(u.Id, Select(Count(Asterisk)).From(u2))",
            () => DeleteFrom(u).Where(u.Id <= 3).Returning(u.Id, Select(Count(Asterisk)).From(u2)));
        (Outcome returned, _) = RunAndRead(delete, delete);

        Assert.Null(returned.Error);
        Assert.Equal(["1|27", "2|26", "3|25"], returned.Rows!.Order(StringComparer.Ordinal));
    }

    // The 28 seed rows: the starting rows give 28 three times; SQLite 3.50.4 counts the rows
    // inserted so far.
    [Fact]
    public void InsertReturning_Sqlite_SubqueryOverTarget_ReadsInsertedRows()
    {
        RandomUsersTable t = new("");
        RandomUsersTable u2 = new("u2");
        PlannedStatement insert = new(
            "INSERT",
            "InsertInto(t, t.Id).Values(600).Values(601).Values(602)"
                + ".Returning(t.Id, Select(Count(Asterisk)).From(u2))",
            () => InsertInto(t, t.Id)
                .Values(600)
                .Values(601)
                .Values(602)
                .Returning(t.Id, Select(Count(Asterisk)).From(u2)));
        (Outcome returned, _) = RunAndRead(insert, insert);

        Assert.Null(returned.Error);
        Assert.Equal(["600|29", "601|30", "602|31"], returned.Rows!.Order(StringComparer.Ordinal));
    }

    // Seed ages 30, 25, 30 sum to 85; SQLite 3.50.4 sums the ages already set to 100.
    [Fact]
    public void UpdateReturning_Sqlite_SubqueryOverTarget_ReadsUpdatedRows()
    {
        RandomUsersTable u = new("u");
        RandomUsersTable u2 = new("u2");
        PlannedStatement update = new(
            "UPDATE",
            "Update(u).Set(u.Age == 100).Where(u.Id <= 3)"
                + ".Returning(u.Id, Select(Sum(u2.Age)).From(u2).Where(u2.Id <= 3))",
            () => Update(u)
                .Set(u.Age == 100)
                .Where(u.Id <= 3)
                .Returning(u.Id, Select(Sum(u2.Age)).From(u2).Where(u2.Id <= 3)));
        (Outcome returned, _) = RunAndRead(update, update);

        Assert.Null(returned.Error);
        Assert.Equal(["1|155", "2|230", "3|300"], returned.Rows!.Order(StringComparer.Ordinal));
    }

    public static TheoryData<string, int, string> Rows => DmlCaseTable.TheoryRows(Dbms.Sqlite);

    [Theory]
    [MemberData(nameof(Rows))]
    public void CaseRow_GeneratedStatements_HoldEveryRelation(
        string model,
        int row,
        string factors) => RunRow(model, row, factors);
}

[Trait("Engine", "PostgreSql")]
public sealed class PostgreSqlCombinatorialDmlTests
    : CombinatorialDmlTestBase, IClassFixture<PostgreSqlFixture>
{
    public PostgreSqlCombinatorialDmlTests(PostgreSqlFixture fixture) : base(fixture)
    {
    }

    public static TheoryData<string, int, string> Rows =>
        DmlCaseTable.TheoryRows(Dbms.PostgreSql);

    [Theory]
    [MemberData(nameof(Rows))]
    public void CaseRow_GeneratedStatements_HoldEveryRelation(
        string model,
        int row,
        string factors) => RunRow(model, row, factors);
}

[Trait("Engine", "PostgreSql17")]
public sealed class PostgreSql17CombinatorialDmlTests
    : CombinatorialDmlTestBase, IClassFixture<PostgreSql17Fixture>
{
    public PostgreSql17CombinatorialDmlTests(PostgreSql17Fixture fixture) : base(fixture)
    {
    }

    public static TheoryData<string, int, string> Rows =>
        DmlCaseTable.TheoryRows(Dbms.PostgreSql);

    [Theory]
    [MemberData(nameof(Rows))]
    public void CaseRow_GeneratedStatements_HoldEveryRelation(
        string model,
        int row,
        string factors) => RunRow(model, row, factors);
}

[Trait("Engine", "MySql")]
public sealed class MySqlCombinatorialDmlTests
    : CombinatorialDmlTestBase, IClassFixture<MySqlFixture>
{
    private static readonly PlannedStatement s_one = new(
        "READ",
        "Select(1)",
        () => Select(1));

    public MySqlCombinatorialDmlTests(MySqlFixture fixture) : base(fixture)
    {
    }

    // Seed row 1 has age 30: the starting row stores 30 in dept_id; MySQL 8.0 evaluates SET
    // left to right, so dept_id reads the 31 just written, which DmlGenerators never builds.
    [Fact]
    public void UpdateSet_MySql_LaterAssignment_ReadsEarlierAssignedValue()
    {
        RandomUsersTable u = new("u");
        (Outcome update, List<string> row) = RunAndRead(
            new PlannedStatement(
                "UPDATE",
                "Update(u).Set(u.Age == u.Age + 1, u.DeptId == u.Age).Where(u.Id == 1)",
                () => Update(u).Set(u.Age == u.Age + 1, u.DeptId == u.Age).Where(u.Id == 1)),
            new PlannedStatement(
                "READ",
                "Select(u.Id, u.Age, u.DeptId).From(u).Where(u.Id == 1)",
                () => Select(u.Id, u.Age, u.DeptId).From(u).Where(u.Id == 1)));

        Assert.Null(update.Error);
        Assert.Equal(["1|31|31"], row);
    }

    // ER_UPDATE_TABLE_USED (1093) on 8.0.46, aliased or not, so DmlGenerators' UPDATE and
    // DELETE subqueries read rt_depts on MySQL.
    [Fact]
    public void UpdateWhere_MySql_SubqueryOverTarget_IsRejectedByTheEngine()
    {
        RandomUsersTable u = new("u");
        RandomUsersTable u2 = new("u2");
        PlannedStatement update = new(
            "UPDATE",
            "Update(u).Set(u.Age == 1).Where(u.Id.In(Select(u2.Id).From(u2)))",
            () => Update(u).Set(u.Age == 1).Where(u.Id.In(Select(u2.Id).From(u2))));
        (Outcome rejected, _) = RunAndRead(update, s_one);

        Assert.Contains("You can't specify target table", rejected.Error);
    }

    // An INSERT's VALUES subquery is rejected naming the target bare and runs over an aliased
    // rt_users (8.0.46), so DmlGenerators keeps MySQL's inner tables aliased.
    [Fact]
    public void InsertValues_MySql_SubqueryNamingTargetBare_IsRejectedByTheEngine()
    {
        RandomUsersTable t = new("");
        RandomUsersTable u2 = new("u2");
        PlannedStatement bare = new(
            "BARE",
            "InsertInto(t, t.Id, t.Age).Values(600, Select(Max(t.Age)).From(t))",
            () => InsertInto(t, t.Id, t.Age).Values(600, Select(Max(t.Age)).From(t)));
        PlannedStatement aliased = new(
            "ALIASED",
            "InsertInto(t, t.Id, t.Age).Values(600, Select(Max(u2.Age)).From(u2))",
            () => InsertInto(t, t.Id, t.Age).Values(600, Select(Max(u2.Age)).From(u2)));
        (Outcome rejected, _) = RunAndRead(bare, s_one);
        (Outcome accepted, List<string> row) = RunAndRead(
            aliased,
            new PlannedStatement(
                "READ",
                "Select(t.Age).From(t).Where(t.Id == 600)",
                () => Select(t.Age).From(t).Where(t.Id == 600)));

        Assert.Contains("You can't specify target table", rejected.Error);
        Assert.Null(accepted.Error);
        Assert.Equal(["99"], row);
    }

    // Three seed rows match: the starting rows give 3 twice; 8.0.46 counts the row inserted
    // before, so DmlGenerators' multi-row VALUES subqueries read rt_depts on MySQL.
    [Fact]
    public void InsertValues_MySql_MultiRowSubqueryOverTarget_ReadsEarlierRows()
    {
        RandomUsersTable t = new("");
        RandomUsersTable u2 = new("u2");
        (Outcome insert, List<string> ages) = RunAndRead(
            new PlannedStatement(
                "INSERT",
                "InsertInto(t, t.Id, t.Age).Values(600, Counted()).Values(601, Counted())",
                () => InsertInto(t, t.Id, t.Age).Values(600, Counted()).Values(601, Counted())),
            new PlannedStatement(
                "READ",
                "Select(t.Id, t.Age).From(t).Where(t.Id >= 600).OrderBy(t.Id)",
                () => Select(t.Id, t.Age).From(t).Where(t.Id >= 600).OrderBy(t.Id)));

        Assert.Null(insert.Error);
        Assert.Equal(["600|3", "601|4"], ages);

        ISubquery Counted() =>
            Select(Count(Asterisk)).From(u2).Where((u2.Id <= 3) | (u2.Id >= 600));
    }

    // The NULL name group (three rows) fails the HAVING and no pair is 'Xu', so NOT IN is 1 and
    // IN 0, as the derived table reads them; 8.0.46 reads NULL for both, so QueryGenerators
    // gives MySQL's IN and NOT IN subqueries no HAVING.
    [Fact]
    public void InSubquery_MySql_HavingDroppingNullGroup_ReadsNull()
    {
        RandomUsersTable u2 = new("u2");
        SubqueryDerivedTable q = Pairs().AsTable("q");
        PlannedStatement read = new(
            "READ",
            "Select(Truth(Bind(\"Xu\").NotIn(Pairs())), Truth(Bind(\"Xu\").In(Pairs())), "
                + "Truth(Bind(\"Xu\").NotIn(Derived())), Truth(Bind(\"Xu\").In(Derived())))",
            () => Select(
                Truth(Bind("Xu").NotIn(Pairs())),
                Truth(Bind("Xu").In(Pairs())),
                Truth(Bind("Xu").NotIn(Derived())),
                Truth(Bind("Xu").In(Derived()))));
        (Outcome outcome, _) = RunAndRead(read, read);

        Assert.Null(outcome.Error);
        Assert.Equal(["NULL|NULL|1|0"], outcome.Rows);

        ISubquery Pairs() =>
            Select(u2.Name).From(u2).GroupBy(u2.Name).Having(Count(Asterisk) == 2);
        ISubquery Derived() => Select(q.Column("name")).From(q);
        static SqlExpression Truth(SqlCondition c) =>
            Case(When(c).Then(1), When(Not(c)).Then(0));
    }

    public static TheoryData<string, int, string> Rows => DmlCaseTable.TheoryRows(Dbms.MySql);

    [Theory]
    [MemberData(nameof(Rows))]
    public void CaseRow_GeneratedStatements_HoldEveryRelation(
        string model,
        int row,
        string factors) => RunRow(model, row, factors);
}

[Trait("Engine", "SqlServer")]
public sealed class SqlServerCombinatorialDmlTests
    : CombinatorialDmlTestBase, IClassFixture<SqlServerFixture>
{
    public SqlServerCombinatorialDmlTests(SqlServerFixture fixture) : base(fixture)
    {
    }

    public static TheoryData<string, int, string> Rows =>
        DmlCaseTable.TheoryRows(Dbms.SqlServer);

    [Theory]
    [MemberData(nameof(Rows))]
    public void CaseRow_GeneratedStatements_HoldEveryRelation(
        string model,
        int row,
        string factors) => RunRow(model, row, factors);
}

[Trait("Engine", "SqlServer2025")]
public sealed class SqlServer2025CombinatorialDmlTests
    : CombinatorialDmlTestBase, IClassFixture<SqlServer2025Fixture>
{
    public SqlServer2025CombinatorialDmlTests(SqlServer2025Fixture fixture) : base(fixture)
    {
    }

    public static TheoryData<string, int, string> Rows =>
        DmlCaseTable.TheoryRows(Dbms.SqlServer);

    [Theory]
    [MemberData(nameof(Rows))]
    public void CaseRow_GeneratedStatements_HoldEveryRelation(
        string model,
        int row,
        string factors) => RunRow(model, row, factors);
}

[Trait("Engine", "Oracle")]
public sealed class OracleCombinatorialDmlTests
    : CombinatorialDmlTestBase, IClassFixture<OracleFixture>
{
    public OracleCombinatorialDmlTests(OracleFixture fixture) : base(fixture)
    {
    }

    public static TheoryData<string, int, string> Rows => DmlCaseTable.TheoryRows(Dbms.Oracle);

    [Theory]
    [MemberData(nameof(Rows))]
    public void CaseRow_GeneratedStatements_HoldEveryRelation(
        string model,
        int row,
        string factors) => RunRow(model, row, factors);
}

[Trait("Engine", "Oracle23ai")]
public sealed class Oracle23aiCombinatorialDmlTests
    : CombinatorialDmlTestBase, IClassFixture<Oracle23aiFixture>
{
    public Oracle23aiCombinatorialDmlTests(Oracle23aiFixture fixture) : base(fixture)
    {
    }

    public static TheoryData<string, int, string> Rows => DmlCaseTable.TheoryRows(Dbms.Oracle);

    [Theory]
    [MemberData(nameof(Rows))]
    public void CaseRow_GeneratedStatements_HoldEveryRelation(
        string model,
        int row,
        string factors) => RunRow(model, row, factors);
}
