using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;

namespace SqlArtisan.Analyzers.Tests;

public class CSharpFallbackAnalyzerTests
{
    private static string Usage(string statements) => $$"""
        using SqlArtisan;
        using SqlArtisan.Internal;
        using static SqlArtisan.Sql;

        sealed class T : DbTableBase
        {
            public DbColumn Id;
            public DbColumn Name;
            public T(string alias = "") : base("t", alias) { Id = new DbColumn(this, "id"); Name = new DbColumn(this, "name"); }
        }

        class C
        {
            void M(object o, SqlPart p, string name)
            {
                T t = new T("t");
                {{statements}}
            }
        }
        """;

    private static async Task RunReporting(
        string statements,
        string id,
        string dbms = "postgresql")
    {
        var test = AnalyzerVerifier.Create(Usage(statements), AnalyzerVerifier.EditorConfig(dbms));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning(id).WithLocation(0));
        await test.RunAsync();
    }

    private static async Task RunSilent(string statements, string? dbms = "postgresql")
    {
        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(Usage(statements)),
            dbms is null ? null : AnalyzerVerifier.EditorConfig(dbms));
        await test.RunAsync();
    }

    // --- SQLA0301 ---

    [Fact]
    public Task ObjectLeftOfColumn_InSelect_ReportsSqla0301() =>
        RunReporting("var q = Select({|#0:o == t.Id|}).From(t);", "SQLA0301");

    [Fact]
    public Task SqlPartLeftOfColumn_NotEquals_ReportsSqla0301() =>
        RunReporting("var q = Select({|#0:p != t.Name|}).From(t);", "SQLA0301");

    [Fact]
    public Task ObjectLeftOfColumn_InParamsTail_ReportsSqla0301() =>
        RunReporting("var q = Select(t.Id, {|#0:o == t.Id|}).From(t);", "SQLA0301");

    [Fact]
    public Task SqlPartConstrainedTypeParameterLeftOfObject_ReportsSqla0301() =>
        RunReporting(
            "Local(t.Id); void Local<TP>(TP e) where TP : SqlPart "
                + "{ var q = Select({|#0:e == o|}); }",
            "SQLA0301");

    [Fact]
    public Task ReferenceEqualityAsSqlArtisanOperand_ReportsSqla0301() =>
        RunReporting("var q = Select(t.Id).From(t).Where(t.Id == ({|#0:o == t.Id|}));", "SQLA0301");

    // ConditionIf's `when` takes the C# test on purpose.
    [Fact]
    public Task ReferenceCheckIntoABoolParameter_StaysSilent() =>
        RunSilent("""
            SqlCondition extra = null;
            var q = Select(t.Id).From(t).Where(t.Id == 1 & ConditionIf(extra != null, extra));
            var r = Select(t.Id).From(t).Where(ConditionIf(condition: t.Id == 1, when: p == null));
            """);

    // Nothing proves either operand holds a query object.
    [Fact]
    public Task ObjectsOrClassConstrainedTypeParameter_StaysSilent() =>
        RunSilent(
            "Local(o); void Local<TC>(TC e) where TC : class "
                + "{ var q = Select(e == o, o == name); }");

    [Fact]
    public Task ColumnLeft_BindsSqlArtisansOperator_StaysSilent() =>
        RunSilent("var q = Select(t.Id).From(t).Where(t.Id == o);");

    // Correct C#: the comparison never reaches a SqlArtisan argument.
    [Fact]
    public Task ReferenceCheckOutsideAQuery_StaysSilent() =>
        RunSilent("bool same = o == t.Id; if ((object)t.Id == t.Name) { }");

    [Fact]
    public Task NoTargetConfigured_StaysSilent() =>
        RunSilent("var q = Select(o == t.Id).From(t);", dbms: null);

    // --- SQLA0302 ---

    [Fact]
    public Task ColumnInterpolatedIntoLike_ReportsSqla0302() =>
        RunReporting(
            "var q = Select(t.Id).From(t).Where(t.Name.Like($\"%{|#0:{t.Name}|}%\"));",
            "SQLA0302");

    [Fact]
    public Task ColumnInterpolatedIntoSelect_ReportsSqla0302() =>
        RunReporting("var q = Select($\"{|#0:{t.Id}|}\").From(t);", "SQLA0302");

    // Listagg before WithinGroup is no SqlPart yet, and formats as its type name all the same.
    [Fact]
    public Task PendingNodeInterpolated_ReportsSqla0302() =>
        RunReporting(
            "var q = Select($\"n{|#0:{Listagg(t.Name, \",\")}|}\").From(t);",
            "SQLA0302",
            dbms: "oracle");

    [Fact]
    public Task SequenceInterpolated_ReportsSqla0302() =>
        RunReporting(
            "DbSequence s = Sequence(\"s\"); var q = Select(t.Id).From(t)"
                + ".Where(t.Name.Like($\"{|#0:{s}|}\"));",
            "SQLA0302",
            dbms: "oracle");

    [Fact]
    public Task InterpolationAsSqlArtisanOperand_ReportsSqla0302() =>
        RunReporting(
            "var q = Select(t.Id).From(t).Where(t.Name == $\"{|#0:{t.Id}|}\");", "SQLA0302");

    [Fact]
    public Task ConcatenationAsSqlArtisanOperand_ReportsSqla0302() =>
        RunReporting(
            "var q = Select(t.Name + (\"x\" + {|#0:Select(t.Name).From(t)|})).From(t);",
            "SQLA0302");

    // Generated table classes are sealed, so nothing can override ToString behind one.
    [Fact]
    public Task SealedTableClassConcatenatedIntoLike_ReportsSqla0302() =>
        RunReporting(
            "var q = Select(t.Id).From(t).Where(t.Name.Like(\"%\" + {|#0:t|} + \"%\"));",
            "SQLA0302");

    // A DbTableBase- or SqlPart-typed value may hold a user table class overriding ToString.
    [Fact]
    public Task ExtensibleBaseTypedValue_StaysSilent() =>
        RunSilent("""
            DbTableBase tb = t;
            var q = Select(t.Id).From(t).Where(t.Name.Like("%" + p + $"{tb}"));
            Local(t); void Local<TB>(TB e) where TB : DbTableBase
            { var r = Select(t.Id).From(t).Where(t.Name.Like($"{e}")); }
            """);

    [Fact]
    public Task SubqueryConcatenatedIntoLike_ReportsSqla0302() =>
        RunReporting(
            "var q = Select(t.Id).From(t)"
                + ".Where(t.Name.Like(\"x\" + {|#0:Select(t.Name).From(t)|}));",
            "SQLA0302");

    [Fact]
    public Task PendingNodeConcatenatedIntoSelect_ReportsSqla0302() =>
        RunReporting("var q = Select({|#0:RowNumber()|} + \"\").From(t);", "SQLA0302");

    [Fact]
    public Task ColumnConcatenated_BindsSqlArtisansOperator_StaysSilent() =>
        RunSilent("var q = Select(\"n\" + t.Name, t.Name + \"n\").From(t);");

    // Nothing proves an object holds a query object.
    [Fact]
    public Task ObjectConcatenatedIntoLike_StaysSilent() =>
        RunSilent("var q = Select(t.Id).From(t).Where(t.Name.Like(\"x\" + o));");

    [Fact]
    public Task StringInterpolatedIntoLike_StaysSilent() =>
        RunSilent("var q = Select(t.Id).From(t).Where(t.Name.Like($\"%{name}%\"));");

    // A log line is correct code, and a built statement's ToString is its SQL text.
    [Fact]
    public Task InterpolationOutsideAQueryOrOfAStatement_StaysSilent() =>
        RunSilent("""
            string log = $"{t.Id}";
            SqlStatement sql = Select(t.Id).From(t).Build();
            var q = Select(t.Id).From(t).Where(t.Name.Like($"{sql}"));
            """);
}
