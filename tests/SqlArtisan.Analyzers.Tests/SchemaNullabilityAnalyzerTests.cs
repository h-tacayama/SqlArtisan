using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;

namespace SqlArtisan.Analyzers.Tests;

public class SchemaNullabilityAnalyzerTests
{
    // Mirrors what SqlArtisan.TableClassGen emits: the facts it determined are
    // named arguments, and a fact it could not determine is simply absent.
    private static string Usage(string statements) => $$"""
        using SqlArtisan;
        using SqlArtisan.Internal;
        using static SqlArtisan.Sql;

        class T : DbTableBase
        {
            public T(string alias = "") : base("t", alias)
            {
                Code = new DbColumn(this, "code");
                Note = new DbColumn(this, "note");
                Legacy = new DbColumn(this, "legacy");
                Partial = new DbColumn(this, "partial");
            }

            [DbColumnMetadata(Nullable = false, HasDefault = false)]
            public DbColumn Code { get; }

            [DbColumnMetadata(Nullable = true, HasDefault = false)]
            public DbColumn Note { get; }

            public DbColumn Legacy { get; }

            [DbColumnMetadata(HasDefault = true)]
            public DbColumn Partial { get; }
        }

        class C
        {
            static ISelectBuilderFrom Joined(T t, T r) =>
                Select(t.Code).From(t).LeftJoin(r).On(t.Code == r.Code);

            void M()
            {
                T t = new T();
                {{statements}}
            }
        }
        """;

    private static Task RunReporting(string statements, params DiagnosticResult[] expected) =>
        RunAsync(Usage(statements), AnalyzerVerifier.EditorConfig("postgresql"), expected);

    private static Task RunSilent(string statements, string? dbms = "postgresql") =>
        RunAsync(
            AnalyzerVerifier.Unmarked(Usage(statements)),
            dbms is null ? null : AnalyzerVerifier.EditorConfig(dbms),
            []);

    private static Task RunAsync(string source, string? editorConfig, DiagnosticResult[] expected)
    {
        var test = AnalyzerVerifier.Create(source, editorConfig);
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    private static DiagnosticResult Expected(string column, string predicate, string constant) =>
        new DiagnosticResult("SQLA0007", Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments(column, predicate, constant);

    [Fact]
    public Task IsNull_InsideWhere_Warns() =>
        RunReporting(
            "var s = Select(t.Code).From(t).Where({|#0:t.Code.IsNull|}).Build();",
            Expected("Code", "IsNull", "false"));

    [Fact]
    public Task IsNotNull_InsideWhere_Warns() =>
        RunReporting(
            "var s = Select(t.Code).From(t).Where({|#0:t.Code.IsNotNull|}).Build();",
            Expected("Code", "IsNotNull", "true"));

    // A static factory only wraps the predicate in argument position; the chain
    // it feeds is the one this statement builds.
    [Fact]
    public Task IsNull_WrappedInConditionIf_Warns() =>
        RunReporting(
            "var s = Select(t.Code).From(t).Where(ConditionIf(true, {|#0:t.Code.IsNull|})).Build();",
            Expected("Code", "IsNull", "false"));

    [Fact]
    public Task IsNull_WrappedInNot_Warns() =>
        RunReporting(
            "var s = Select(t.Code).From(t).Where(Not({|#0:t.Code.IsNull|})).Build();",
            Expected("Code", "IsNull", "false"));

    // The LEFT JOIN anti-join: past an outer join the NOT NULL column is
    // null-supplied, so the predicate is exactly not constant there.
    [Fact]
    public Task IsNull_NotNullColumnAfterLeftJoin_Silent() =>
        RunSilent("""
            T r = new T("r");
            var s = Select(t.Code).From(t).LeftJoin(r).On(t.Code == r.Code).Where(r.Code.IsNull).Build();
            """);

    [Fact]
    public Task IsNotNull_NotNullColumnAfterLeftJoin_Silent() =>
        RunSilent("""
            T r = new T("r");
            var s = Select(t.Code).From(t).LeftJoin(r).On(t.Code == r.Code).Where(r.Code.IsNotNull).Build();
            """);

    // The NATURAL forms null-supply just the same, and need no split chain to
    // defeat a suppression keyed on the explicit spellings alone.
    [Fact]
    public Task IsNull_NotNullColumnAfterNaturalLeftJoin_Silent() =>
        RunSilent("""
            T r = new T("r");
            var s = Select(t.Code).From(t).NaturalLeftJoin(r).Where(r.Code.IsNull).Build();
            """);

    [Fact]
    public Task IsNull_NotNullColumnAfterNaturalRightJoin_Silent() =>
        RunSilent("""
            T r = new T("r");
            var s = Select(t.Code).From(t).NaturalRightJoin(r).Where(r.Code.IsNull).Build();
            """);

    [Fact]
    public Task IsNull_NotNullColumnAfterNaturalFullJoin_Silent() =>
        RunSilent("""
            T r = new T("r");
            var s = Select(t.Code).From(t).NaturalFullJoin(r).Where(r.Code.IsNull).Build();
            """);

    [Fact]
    public Task IsNull_NotNullColumnAfterOuterApply_Silent() =>
        RunSilent(
            """
            DerivedTable d = new DerivedTable("d");
            var s = Select(t.Code).From(t).OuterApply(Select(t.Code).From(t), d).Where(t.Code.IsNull).Build();
            """,
            dbms: "sqlserver");

    // NATURAL JOIN is an inner join and null-supplies nothing.
    [Fact]
    public Task IsNull_NotNullColumnAfterNaturalJoin_Warns() =>
        RunReporting(
            """
            T r = new T("r");
            var s = Select(t.Code).From(t).NaturalJoin(r).Where({|#0:r.Code.IsNull|}).Build();
            """,
            Expected("Code", "IsNull", "false"));

    // An inner join never null-supplies, so the warning stands.
    [Fact]
    public Task IsNull_NotNullColumnAfterInnerJoin_Warns() =>
        RunReporting(
            """
            T r = new T("r");
            var s = Select(t.Code).From(t).InnerJoin(r).On(t.Code == r.Code).Where({|#0:r.Code.IsNull|}).Build();
            """,
            Expected("Code", "IsNull", "false"));

    // The join lives in another statement, so this statement cannot show whether
    // the column is null-supplied — the shape that made the outer-join
    // suppression miss when it was scoped to one statement.
    [Fact]
    public Task IsNull_ChainHeldInLocal_Silent() =>
        RunSilent("""
            T r = new T("r");
            var prefix = Select(t.Code).From(t).LeftJoin(r).On(t.Code == r.Code);
            var s = prefix.Where(r.Code.IsNull).Build();
            """);

    [Fact]
    public Task IsNull_ConditionHeldInLocal_Silent() =>
        RunSilent("""
            T r = new T("r");
            SqlCondition c = r.Code.IsNull;
            var s = Select(t.Code).From(t).LeftJoin(r).On(t.Code == r.Code).Where(c).Build();
            """);

    [Fact]
    public Task IsNull_ChainFromHelperMethod_Silent() =>
        RunSilent("""
            T r = new T("r");
            var s = Joined(t, r).Where(r.Code.IsNull).Build();
            """);

    // Outside a query there is no join context to read at all.
    [Fact]
    public Task IsNull_OutsideAStatement_Silent() =>
        RunSilent("var c = t.Code.IsNull;");

    // Every fact-driven silence below is asserted in a statement that would
    // otherwise report, so it is the fact doing the silencing.
    [Fact]
    public Task IsNull_NullableColumn_Silent() =>
        RunSilent("var s = Select(t.Code).From(t).Where(t.Note.IsNull).Build();");

    [Fact]
    public Task IsNotNull_NullableColumn_Silent() =>
        RunSilent("var s = Select(t.Code).From(t).Where(t.Note.IsNotNull).Build();");

    // Absence of the attribute is absence of a claim — a hand-written table class
    // must never acquire a diagnostic it never carried the facts for.
    [Fact]
    public Task IsNull_ColumnWithoutMetadata_Silent() =>
        RunSilent("var s = Select(t.Code).From(t).Where(t.Legacy.IsNull).Build();");

    // The tri-state that the whole design rests on: HasDefault was determined,
    // Nullable was not, so nullability claims nothing.
    [Fact]
    public Task IsNull_ColumnWithUndeterminedNullability_Silent() =>
        RunSilent("var s = Select(t.Code).From(t).Where(t.Partial.IsNull).Build();");

    [Fact]
    public Task IsNull_NoTargetConfigured_Silent() =>
        RunSilent("var s = Select(t.Code).From(t).Where(t.Code.IsNull).Build();", dbms: null);

    [Fact]
    public Task IsNull_NonColumnExpression_Silent() =>
        RunSilent("var s = Select(t.Code).From(t).Where(Upper(t.Code).IsNull).Build();");
}
