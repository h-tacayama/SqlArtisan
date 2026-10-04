using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;

namespace SqlArtisan.Analyzers.Tests;

public class CSharpFallbackAnalyzerTests
{
    private static string Usage(string statements) => $$"""
        using SqlArtisan;
        using SqlArtisan.Internal;
        using static SqlArtisan.Sql;

        class T : DbTableBase
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

    private static async Task RunReporting(string statements, string id)
    {
        var test = AnalyzerVerifier.Create(
            Usage(statements), AnalyzerVerifier.EditorConfig("postgresql"));
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
    public Task ConstrainedTypeParameterLeftOfColumn_ReportsSqla0301() =>
        RunReporting(
            "Local(t.Id); void Local<TP>(TP e) where TP : SqlPart "
                + "{ var q = Select({|#0:e == o|}); }",
            "SQLA0301");

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

    // A pending node is no SqlPart yet, and formats as its type name all the same.
    [Fact]
    public Task PendingNodeInterpolated_ReportsSqla0302() =>
        RunReporting("var q = Select($\"n{|#0:{RowNumber()}|}\").From(t);", "SQLA0302");

    [Fact]
    public Task SqlPartConcatenatedIntoLike_ReportsSqla0302() =>
        RunReporting(
            "var q = Select(t.Id).From(t).Where(t.Name.Like(\"%\" + {|#0:p|} + \"%\"));",
            "SQLA0302");

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
