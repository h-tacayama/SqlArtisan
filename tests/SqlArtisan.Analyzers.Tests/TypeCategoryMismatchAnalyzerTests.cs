using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace SqlArtisan.Analyzers.Tests;

public class TypeCategoryMismatchAnalyzerTests
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
                Active = new DbColumn(this, "active");
                Legacy = new DbColumn(this, "legacy");
            }

            [DbColumnMetadata(TypeCategory = DbTypeCategory.Text)]
            public DbColumn Code { get; }

            [DbColumnMetadata(TypeCategory = DbTypeCategory.Numeric)]
            public DbColumn Amount { get; }

            [DbColumnMetadata(TypeCategory = DbTypeCategory.Temporal)]
            public DbColumn CreatedAt { get; }

            [DbColumnMetadata(TypeCategory = DbTypeCategory.Boolean)]
            public DbColumn Active { get; }

            [DbColumnMetadata(Nullable = true)]
            public DbColumn Legacy { get; }
        }

        class Dto
        {
            public string Name { get; set; } = "";

            public decimal? Total { get; set; }
        }

        class C
        {
            void M(object untyped, Dto dto)
            {
                T t = new T();
                {{statements}}
            }
        }
        """;

    private static string EditorConfig(string dbms = "postgresql") => $"""
        root = true

        [*.cs]
        sqlartisan_syntax_{dbms} = any
        """;

    private static Task RunReporting(
        string statements, string column, string was, string got, string dbms = "postgresql") =>
        RunAsync(
            Usage(statements),
            [new DiagnosticResult("SQLA0205", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments(column, was, got)],
            dbms);

    private static Task RunSilent(string statements, string dbms = "postgresql") =>
        RunAsync(AnalyzerVerifier.Unmarked(Usage(statements)), [], dbms);

    // Every rule here stays silent until a target dialect is named, even though
    // this verdict does not depend on one.
    [Fact]
    public Task Where_NoTargetDialectConfigured_Silent()
    {
        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(
                Usage("var s = Select(t.Code).From(t).Where(t.Code == Bind(1)).Build();")),
            editorConfig: null);
        return test.RunAsync();
    }

    private static Task RunAsync(string source, DiagnosticResult[] expected, string dbms = "postgresql")
    {
        var test = AnalyzerVerifier.Create(source, EditorConfig(dbms));
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

    // T-SQL has no boolean literal, so `bit = 1` is the only spelling it offers;
    // MySQL's BOOLEAN is TINYINT(1), so the mirror shape is idiomatic there.
    [Fact]
    public Task Where_BooleanColumnComparedToNumber_Silent() =>
        RunSilent("var s = Select(t.Code).From(t).Where(t.Active == 1).Build();");

    [Fact]
    public Task Where_NumericColumnComparedToBool_Silent() =>
        RunSilent("var s = Select(t.Code).From(t).Where(t.Amount == true).Build();");

    // A truth value against text is still a mismatch on every engine.
    [Fact]
    public Task Where_BooleanColumnComparedToText_Warns() =>
        RunReporting(
            "var s = Select(t.Code).From(t).Where({|#0:t.Active == \"yes\"|}).Build();",
            "Active",
            "boolean",
            "text");

    // Neither clause is visible from a condition built apart from it, so the rule
    // declines to guess which one it will become.
    [Fact]
    public Task Where_ConditionHeldInAVariable_Silent() =>
        RunSilent(
            "SqlCondition c = t.Code == 1;"
                + " var s = Select(t.Code).From(t).Where(c).Build();");

    [Fact]
    public Task Set_AssignmentHeldInAVariable_Silent() =>
        RunSilent(
            "EqualityCondition a = t.Code == 1;"
                + " var s = Update(t).Set(a).Build();");

    [Fact]
    public Task ThenUpdateSet_AssignmentOfAnotherCategory_Silent() =>
        RunSilent(
            "T r = new T(\"r\");"
                + " var s = MergeInto(t).Using(r).On(t.Code == r.Code)"
                + ".WhenMatched().ThenUpdateSet(t.Code == 1).Build();");

    // SET spells its assignment with ==, and an assignment has no side to cast.
    [Fact]
    public Task Set_AssignmentOfAnotherCategory_Silent() =>
        RunSilent("var s = Update(t).Set(t.Code == 1).Where(t.Amount > 0).Build();");

    [Fact]
    public Task DoUpdateSet_AssignmentOfAnotherCategory_Silent() =>
        RunSilent(
            "var s = InsertInto(t, t.Code).Values(\"x\").OnConflict(t.Code)"
                + ".DoUpdateSet(t.CreatedAt == \"2024-01-01\").Build();");

    [Fact]
    public Task OnDuplicateKeyUpdate_AssignmentOfAnotherCategory_Silent() =>
        RunSilent(
            "var s = InsertInto(t, t.Code).Values(\"x\")"
                + ".OnDuplicateKeyUpdate(t.CreatedAt == \"2024-01-01\").Build();",
            dbms: "mysql");

    // A property with no recorded category is judged by its C# type, as a field
    // or local already was.
    [Fact]
    public Task Where_ColumnComparedToPlainProperty_Warns() =>
        RunReporting(
            "var s = Select(t.Code).From(t).Where({|#0:t.Amount == dto.Name|}).Build();",
            "Amount",
            "numeric",
            "text");

    [Fact]
    public Task Where_ColumnComparedToNullableValue_Warns() =>
        RunReporting(
            "var s = Select(t.Code).From(t).Where({|#0:t.Code == dto.Total|}).Build();",
            "Code",
            "text",
            "numeric");
}
