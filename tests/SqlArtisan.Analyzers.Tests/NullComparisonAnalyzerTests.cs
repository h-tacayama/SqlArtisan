using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;

namespace SqlArtisan.Analyzers.Tests;

public class NullComparisonAnalyzerTests
{
    private static string Usage(string statements) => $$"""
        using SqlArtisan;
        using static SqlArtisan.Sql;

        sealed class T : DbTableBase
        {
            public DbColumn Id;
            public DbColumn Name;
            public T(string alias = "") : base("t", alias) { Id = new DbColumn(this, "id"); Name = new DbColumn(this, "name"); }
        }

        class C
        {
            void M()
            {
                T t = new T("t");
                {{statements}}
            }
        }
        """;

    private static async Task RunReporting(string statements, string token, string remedy)
    {
        var test = AnalyzerVerifier.Create(
            Usage(statements), AnalyzerVerifier.EditorConfig("postgresql"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0303")
            .WithLocation(0)
            .WithArguments(token, remedy));
        await test.RunAsync();
    }

    private static async Task RunSilent(string statements, string? dbms = "postgresql")
    {
        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(Usage(statements)),
            dbms is null ? null : AnalyzerVerifier.EditorConfig(dbms));
        await test.RunAsync();
    }

    [Fact]
    public Task EqualsNull_InWhere_ReportsSqla0303() =>
        RunReporting(
            "var q = Select(t.Id).From(t).Where({|#0:t.Name == Null|});",
            "=",
            "test for NULL with IsNull");

    [Fact]
    public Task NotEqualsNull_InWhere_ReportsSqla0303() =>
        RunReporting(
            "var q = Select(t.Id).From(t).Where({|#0:t.Name != Null|});",
            "<>",
            "test for NULL with IsNotNull");

    [Theory]
    [InlineData("<")]
    [InlineData("<=")]
    [InlineData(">")]
    [InlineData(">=")]
    public Task OrderingWithNull_InWhere_ReportsSqla0303(string op) =>
        RunReporting(
            $"var q = Select(t.Id).From(t).Where({{|#0:t.Id {op} Null|}});",
            op,
            "test for NULL with IsNull or IsNotNull");

    [Fact]
    public Task NullOnTheLeft_ReportsSqla0303() =>
        RunReporting(
            "var q = Select(t.Id).From(t).Where({|#0:Null == t.Name|});",
            "=",
            "test for NULL with IsNull");

    [Fact]
    public Task EqualsNull_BesideAnotherCondition_ReportsSqla0303() =>
        RunReporting(
            "var q = Select(t.Id).From(t).Where(t.Id == 1 & {|#0:t.Name == Null|});",
            "=",
            "test for NULL with IsNull");

    [Fact]
    public Task EqualsNull_InSearchedCaseWhen_ReportsSqla0303() =>
        RunReporting(
            "var q = Select(Case(When({|#0:t.Name == Null|}).Then(1))).From(t);",
            "=",
            "test for NULL with IsNull");

    [Fact]
    public Task EqualsNull_HeldAsSqlCondition_ReportsSqla0303() =>
        RunReporting("SqlCondition c = {|#0:t.Name == Null|};", "=", "test for NULL with IsNull");

    // Neither `!=` (rejected at the SET call) nor an ordering comparison can be a SET
    // assignment, so a variable holding one is reported.
    [Fact]
    public Task NotEqualsNull_HeldInVar_ReportsSqla0303() =>
        RunReporting("var c = {|#0:t.Name != Null|};", "<>", "test for NULL with IsNotNull");

    [Fact]
    public Task OrderingWithNull_HeldInVar_ReportsSqla0303() =>
        RunReporting("var c = {|#0:t.Id < Null|};", "<", "test for NULL with IsNull or IsNotNull");

    [Fact]
    public Task EqualsNull_InConditionalPassedToWhere_ReportsSqla0303() =>
        RunReporting(
            "bool flag = true; "
                + "var q = Select(t.Id).From(t).Where(flag ? {|#0:t.Name == Null|} : t.Id == 1);",
            "=",
            "test for NULL with IsNull");

    [Fact]
    public Task SimpleCaseWhenNull_ReportsSqla0303() =>
        RunReporting(
            "var q = Select(Case(t.Name, {|#0:When(Null)|}.Then(1))).From(t);",
            "WHEN NULL",
            "test for NULL with a searched When(x.IsNull)");

    [Fact]
    public Task NotInWithNull_ReportsSqla0303() =>
        RunReporting(
            "var q = Select(t.Id).From(t).Where({|#0:t.Id.NotIn(1, Null)|});",
            "NOT IN",
            "drop the NULL from the list");

    // Dropping the only NULL leaves no list, so the remedy is the NULL test.
    [Fact]
    public Task NotInWithOnlyNull_ReportsSqla0303() =>
        RunReporting(
            "var q = Select(t.Id).From(t).Where({|#0:t.Id.NotIn(Null)|});",
            "NOT IN",
            "test for NULL with IsNotNull");

    [Fact]
    public Task BetweenWithBothBoundsNull_ReportsSqla0303() =>
        RunReporting(
            "var q = Select(t.Id).From(t).Where({|#0:t.Id.Between(Null, BindNull())|});",
            "BETWEEN",
            "test for NULL with IsNull");

    [Fact]
    public Task InWithOnlyNull_ReportsSqla0303() =>
        RunReporting(
            "var q = Select(t.Id).From(t).Where({|#0:t.Id.In(Null)|});",
            "IN",
            "test for NULL with IsNull");

    [Theory]
    [InlineData("Null, 10")]
    [InlineData("1, Null")]
    public Task BetweenWithNullBound_ReportsSqla0303(string bounds) =>
        RunReporting(
            $"var q = Select(t.Id).From(t).Where({{|#0:t.Id.Between({bounds})|}});",
            "BETWEEN",
            "compare the other bound with >= or <=");

    [Theory]
    [InlineData("Like", "LIKE")]
    [InlineData("NotLike", "NOT LIKE")]
    public Task LikeNull_ReportsSqla0303(string member, string token) =>
        RunReporting(
            $"var q = Select(t.Id).From(t).Where({{|#0:t.Name.{member}(Null)|}});",
            token,
            "test for NULL with IsNull or IsNotNull");

    [Theory]
    [InlineData("==", "=", "test for NULL with IsNull")]
    [InlineData("!=", "<>", "test for NULL with IsNotNull")]
    [InlineData("<", "<", "test for NULL with IsNull or IsNotNull")]
    public Task ComparisonWithBindNull_ReportsSqla0303(string op, string token, string remedy) =>
        RunReporting(
            $"var q = Select(t.Id).From(t).Where({{|#0:t.Id {op} BindNull()|}});", token, remedy);

    // Oracle rejects an untyped bound NULL there rather than skipping the branch.
    [Fact]
    public Task SimpleCaseWhenBindNull_StaysSilent() =>
        RunSilent("var q = Select(Case(t.Name, When(BindNull()).Then(1))).From(t);");

    // One other value can still match.
    [Fact]
    public Task InWithNullBesideAValue_StaysSilent() =>
        RunSilent("var q = Select(t.Id).From(t).Where(t.Id.In(1, Null));");

    // The other bound still decides, so the predicate can be true.
    [Fact]
    public Task NotBetweenWithNullBound_StaysSilent() =>
        RunSilent("var q = Select(t.Id).From(t).Where(t.Id.NotBetween(Null, 10));");

    [Fact]
    public Task NotInWithNullInCollectionExpression_ReportsSqla0303() =>
        RunReporting(
            "var q = Select(t.Id).From(t).Where({|#0:t.Id.NotIn([1, Null])|});",
            "NOT IN",
            "drop the NULL from the list");

    // The collection overload takes a list built elsewhere, which is not read.
    [Fact]
    public Task InWithNullInCollectionOverload_StaysSilent() =>
        RunSilent(
            "var q = Select(t.Id).From(t).Where("
                + "t.Id.In(new System.Collections.Generic.List<object> { Null }));");

    // A list built elsewhere is not read.
    [Fact]
    public Task NotInWithArrayVariable_StaysSilent() =>
        RunSilent(
            "object[] ids = { 1, Null }; var q = Select(t.Id).From(t).Where(t.Id.NotIn(ids));");

    // A typed bound NULL beside a column of another type is rejected on PostgreSQL.
    [Fact]
    public Task TypedBindNull_StaysSilent() =>
        RunSilent(
            "var q = Select(t.Id).From(t).Where(t.Id == BindNull(System.Data.DbType.String));");

    [Fact]
    public Task BindNullAsSetAssignment_StaysSilent() =>
        RunSilent("var q = Update(t).Set(t.Name == BindNull()).Where(t.Id == 1);");

    // `==` in SET is an assignment: SET name = NULL writes the NULL.
    [Fact]
    public Task EqualsNull_AsSetAssignment_StaysSilent() =>
        RunSilent("var q = Update(t).Set(t.Name == Null).Where(t.Id == 1);");

    [Fact]
    public Task EqualsNull_InConditionalPassedToSet_StaysSilent() =>
        RunSilent(
            "bool flag = true; "
                + "var q = Update(t).Set(flag ? t.Name == Null : t.Name == 1).Where(t.Id == 1);");

    // An EqualityCondition held in a variable may still become an assignment.
    [Fact]
    public Task EqualsNull_HeldAsEqualityCondition_StaysSilent() =>
        RunSilent("var e = t.Name == Null;");

    [Fact]
    public Task IsNull_InWhere_StaysSilent() =>
        RunSilent("var q = Select(t.Id).From(t).Where(t.Name.IsNull);");

    [Fact]
    public Task SimpleCaseWhenValue_ThenNull_StaysSilent() =>
        RunSilent("var q = Select(Case(t.Name, When(1).Then(Null))).From(t);");

    [Fact]
    public Task EqualsNull_NoDialectConfigured_StaysSilent() =>
        RunSilent("var q = Select(t.Id).From(t).Where(t.Name == Null);", dbms: null);
}
