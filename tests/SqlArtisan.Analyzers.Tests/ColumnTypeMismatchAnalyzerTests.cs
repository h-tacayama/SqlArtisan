using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace SqlArtisan.Analyzers.Tests;

public class ColumnTypeMismatchAnalyzerTests
{
    private static string Usage(string statements) => $$"""
        using System;
        using SqlArtisan;
        using SqlArtisan.Internal;
        using static SqlArtisan.Sql;

        class T : DbTableBase
        {
            public T(string alias = "") : base("t", alias)
            {
                Code = new DbColumn(this, "code");
                Amount = new DbColumn(this, "amount");
                CreatedAt = new DbColumn(this, "created_at");
                Legacy = new DbColumn(this, "legacy");
            }

            [DbColumnMetadata(ColumnType = DbColumnType.Text)]
            public DbColumn Code { get; }

            [DbColumnMetadata(ColumnType = DbColumnType.Numeric)]
            public DbColumn Amount { get; }

            [DbColumnMetadata(ColumnType = DbColumnType.Temporal)]
            public DbColumn CreatedAt { get; }

            [DbColumnMetadata(Nullable = true)]
            public DbColumn Legacy { get; }
        }

        class C
        {
            void M(object untyped)
            {
                T t = new T();
                {{statements}}
            }
        }
        """;

    private static string EditorConfig() => """
        root = true

        [*.cs]
        sqlartisan_target_dbms = postgresql
        """;

    private static Task RunReporting(string statements, string column, string was, string got) =>
        RunAsync(
            Usage(statements),
            [new DiagnosticResult("SQLA0012", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments(column, was, got)]);

    private static Task RunSilent(string statements) =>
        RunAsync(AnalyzerVerifier.Unmarked(Usage(statements)), []);

    private static Task RunAsync(string source, DiagnosticResult[] expected)
    {
        var test = AnalyzerVerifier.Create(source, EditorConfig());
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    [Fact]
    public Task Where_TextColumnComparedToBoundNumber_Warns() =>
        RunReporting(
            "var s = Select(t.Code).From(t).Where({|#0:t.Code == Bind(1500001)|}).Build();",
            "Code",
            "text",
            "numeric");

    [Fact]
    public Task Where_TextColumnComparedToNumericLiteral_Warns() =>
        RunReporting(
            "var s = Select(t.Code).From(t).Where({|#0:t.Code == 1500001|}).Build();",
            "Code",
            "text",
            "numeric");

    [Fact]
    public Task Where_NumericColumnComparedToText_Warns() =>
        RunReporting(
            "var s = Select(t.Code).From(t).Where({|#0:t.Amount == \"abc\"|}).Build();",
            "Amount",
            "numeric",
            "text");

    [Fact]
    public Task Where_TemporalColumnComparedToText_Warns() =>
        RunReporting(
            "var s = Select(t.Code).From(t).Where({|#0:t.CreatedAt == \"2024-01-01\"|}).Build();",
            "CreatedAt",
            "temporal",
            "text");

    // The column is named whichever side it sits on.
    [Fact]
    public Task Where_ColumnOnTheRight_WarnsNamingTheColumn() =>
        RunReporting(
            "var s = Select(t.Code).From(t).Where({|#0:Bind(1500001) == t.Code|}).Build();",
            "Code",
            "text",
            "numeric");

    [Fact]
    public Task Where_OrderingComparison_Warns() =>
        RunReporting(
            "var s = Select(t.Code).From(t).Where({|#0:t.Code > 5|}).Build();",
            "Code",
            "text",
            "numeric");

    [Fact]
    public Task Where_TwoColumnsOfDifferentCategories_Warns() =>
        RunReporting(
            "var s = Select(t.Code).From(t).Where({|#0:t.Code == t.Amount|}).Build();",
            "Code",
            "text",
            "numeric");

    [Fact]
    public Task Where_SameCategory_Silent() =>
        RunSilent("var s = Select(t.Code).From(t).Where(t.Code == \"abc\").Build();");

    // Width is deliberately not carried, so a decimal against a numeric column is
    // the same category and nothing to report.
    [Fact]
    public Task Where_SameCategoryDifferentWidth_Silent() =>
        RunSilent("var s = Select(t.Code).From(t).Where(t.Amount == 1.5m).Build();");

    [Fact]
    public Task Where_ColumnWithNoRecordedType_Silent() =>
        RunSilent("var s = Select(t.Code).From(t).Where(t.Legacy == 1).Build();");

    [Fact]
    public Task Where_OperandTypeNotDecidable_Silent() =>
        RunSilent("var s = Select(t.Code).From(t).Where(t.Code == untyped).Build();");

    // An explicit Cast is the author saying which type they mean.
    [Fact]
    public Task Where_ColumnWrappedInCast_Silent() =>
        RunSilent(
            "var s = Select(t.Code).From(t).Where(Cast(t.Code, \"integer\") == Bind(1)).Build();");

    [Fact]
    public Task Where_ValueWrappedInCast_Silent() =>
        RunSilent(
            "var s = Select(t.Code).From(t).Where(t.Code == Cast(Bind(1), \"text\")).Build();");

    [Fact]
    public Task Where_TwoColumnsOfTheSameCategory_Silent() =>
        RunSilent("var s = Select(t.Code).From(t).Where(t.Code == new T(\"r\").Code).Build();");
}
