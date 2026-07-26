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
    public Task IsNull_NotNullColumn_Warns() =>
        RunReporting(
            "var c = {|#0:t.Code.IsNull|};",
            Expected("Code", "IsNull", "false"));

    [Fact]
    public Task IsNotNull_NotNullColumn_Warns() =>
        RunReporting(
            "var c = {|#0:t.Code.IsNotNull|};",
            Expected("Code", "IsNotNull", "true"));

    [Fact]
    public Task IsNull_InsideWhere_Warns() =>
        RunReporting(
            "var s = Select(t.Code).From(t).Where({|#0:t.Code.IsNull|}).Build();",
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

    // An inner join never null-supplies, so the warning stands.
    [Fact]
    public Task IsNull_NotNullColumnAfterInnerJoin_Warns() =>
        RunReporting(
            """
            T r = new T("r");
            var s = Select(t.Code).From(t).InnerJoin(r).On(t.Code == r.Code).Where({|#0:r.Code.IsNull|}).Build();
            """,
            Expected("Code", "IsNull", "false"));

    [Fact]
    public Task IsNull_NullableColumn_Silent() =>
        RunSilent("var c = t.Note.IsNull;");

    [Fact]
    public Task IsNotNull_NullableColumn_Silent() =>
        RunSilent("var c = t.Note.IsNotNull;");

    // Absence of the attribute is absence of a claim — a hand-written table class
    // must never acquire a diagnostic it never carried the facts for.
    [Fact]
    public Task IsNull_ColumnWithoutMetadata_Silent() =>
        RunSilent("var c = t.Legacy.IsNull;");

    // The tri-state that the whole design rests on: HasDefault was determined,
    // Nullable was not, so nullability claims nothing.
    [Fact]
    public Task IsNull_ColumnWithUndeterminedNullability_Silent() =>
        RunSilent("var c = t.Partial.IsNull;");

    [Fact]
    public Task IsNull_NoTargetConfigured_Silent() =>
        RunSilent("var c = t.Code.IsNull;", dbms: null);

    [Fact]
    public Task IsNull_NonColumnExpression_Silent() =>
        RunSilent("var c = Upper(t.Code).IsNull;");
}
