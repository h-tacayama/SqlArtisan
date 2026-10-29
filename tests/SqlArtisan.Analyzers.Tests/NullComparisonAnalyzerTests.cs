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
            "var q = Select(t.Id).From(t).Where({|#0:t.Name == Null|});", "=", "IsNull");

    [Fact]
    public Task NotEqualsNull_InWhere_ReportsSqla0303() =>
        RunReporting(
            "var q = Select(t.Id).From(t).Where({|#0:t.Name != Null|});", "<>", "IsNotNull");

    [Theory]
    [InlineData("<")]
    [InlineData("<=")]
    [InlineData(">")]
    [InlineData(">=")]
    public Task OrderingWithNull_InWhere_ReportsSqla0303(string op) =>
        RunReporting(
            $"var q = Select(t.Id).From(t).Where({{|#0:t.Id {op} Null|}});",
            op,
            "IsNull or IsNotNull");

    [Fact]
    public Task NullOnTheLeft_ReportsSqla0303() =>
        RunReporting(
            "var q = Select(t.Id).From(t).Where({|#0:Null == t.Name|});", "=", "IsNull");

    [Fact]
    public Task EqualsNull_BesideAnotherCondition_ReportsSqla0303() =>
        RunReporting(
            "var q = Select(t.Id).From(t).Where(t.Id == 1 & {|#0:t.Name == Null|});",
            "=",
            "IsNull");

    [Fact]
    public Task EqualsNull_InSearchedCaseWhen_ReportsSqla0303() =>
        RunReporting(
            "var q = Select(Case(When({|#0:t.Name == Null|}).Then(1))).From(t);", "=", "IsNull");

    [Fact]
    public Task EqualsNull_HeldAsSqlCondition_ReportsSqla0303() =>
        RunReporting("SqlCondition c = {|#0:t.Name == Null|};", "=", "IsNull");

    // Neither `!=` (rejected at Build()) nor an ordering comparison can be a SET
    // assignment, so a variable holding one is reported.
    [Fact]
    public Task NotEqualsNull_HeldInVar_ReportsSqla0303() =>
        RunReporting("var c = {|#0:t.Name != Null|};", "<>", "IsNotNull");

    [Fact]
    public Task OrderingWithNull_HeldInVar_ReportsSqla0303() =>
        RunReporting("var c = {|#0:t.Id < Null|};", "<", "IsNull or IsNotNull");

    [Fact]
    public Task EqualsNull_InConditionalPassedToWhere_ReportsSqla0303() =>
        RunReporting(
            "bool flag = true; "
                + "var q = Select(t.Id).From(t).Where(flag ? {|#0:t.Name == Null|} : t.Id == 1);",
            "=",
            "IsNull");

    [Fact]
    public Task SimpleCaseWhenNull_ReportsSqla0303() =>
        RunReporting(
            "var q = Select(Case(t.Name, {|#0:When(Null)|}.Then(1))).From(t);",
            "WHEN NULL",
            "a searched When(x.IsNull)");

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
