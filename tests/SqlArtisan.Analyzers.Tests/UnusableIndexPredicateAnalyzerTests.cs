using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace SqlArtisan.Analyzers.Tests;

public class UnusableIndexPredicateAnalyzerTests
{
    private static string Usage(string statements) => $$"""
        using SqlArtisan;
        using SqlArtisan.Internal;
        using static SqlArtisan.Sql;

        class T : DbTableBase
        {
            public T(string alias = "") : base("t", alias)
            {
                Id = new DbColumn(this, "id");
                Name = new DbColumn(this, "name");
                Plain = new DbColumn(this, "plain");
                Expr = new DbColumn(this, "expr");
            }

            [DbColumnMetadata(Nullable = false, HasDefault = true, Indexed = true)]
            public DbColumn Id { get; }

            [DbColumnMetadata(Nullable = true, HasDefault = false, Indexed = true)]
            public DbColumn Name { get; }

            [DbColumnMetadata(Nullable = true, HasDefault = false, Indexed = false)]
            public DbColumn Plain { get; }

            [DbColumnMetadata(Nullable = true, HasDefault = false)]
            public DbColumn Expr { get; }
        }

        class C
        {
            void M()
            {
                T t = new T();
                T r = new T("r");
                {{statements}}
            }
        }
        """;

    private static Task RunReporting(string statements, string column, string shape) =>
        RunAsync(
            Usage(statements),
            [new DiagnosticResult("SQLA0204", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments(column, shape)]);

    private static Task RunSilent(string statements, string? dbms = "postgresql") =>
        RunAsync(AnalyzerVerifier.Unmarked(Usage(statements)), [], dbms);

    private static Task RunAsync(
        string source,
        DiagnosticResult[] expected,
        string? dbms = "postgresql")
    {
        var test = AnalyzerVerifier.Create(
            source,
            dbms is null ? null : AnalyzerVerifier.EditorConfig(dbms));
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    [Fact]
    public Task Where_IndexedColumnWrappedInFunction_Warns() =>
        RunReporting(
            """var s = Select(t.Id).From(t).Where({|#0:Upper(t.Name)|} == "X").Build();""",
            "Name",
            "wrapped in Upper");

    [Fact]
    public Task On_IndexedColumnWrappedInFunction_Warns() =>
        RunReporting(
            """
            var s = Select(t.Id).From(t).InnerJoin(r).On({|#0:Upper(t.Name)|} == r.Name).Build();
            """,
            "Name",
            "wrapped in Upper");

    [Fact]
    public Task Where_IndexedColumnUnderLeadingWildcardLike_Warns() =>
        RunReporting(
            """var s = Select(t.Id).From(t).Where({|#0:t.Name.Like("%x")|}).Build();""",
            "Name",
            "matched with a leading-wildcard pattern");

    [Fact]
    public Task Where_IndexedColumnUnderLeadingWildcardNotLike_Warns() =>
        RunReporting(
            """var s = Select(t.Id).From(t).Where({|#0:t.Name.NotLike("%x")|}).Build();""",
            "Name",
            "matched with a leading-wildcard pattern");

    // The wrap reaches WHERE through a predicate-building step on the wrapped
    // expression itself — the step is part of the predicate, not its clause.
    [Fact]
    public Task Where_WrappedIndexedColumnUnderLike_Warns() =>
        RunReporting(
            """var s = Select(t.Id).From(t).Where({|#0:Upper(t.Name)|}.Like("X%")).Build();""",
            "Name",
            "wrapped in Upper");

    [Fact]
    public Task Where_WrappedIndexedColumnUnderNotLike_Warns() =>
        RunReporting(
            """var s = Select(t.Id).From(t).Where({|#0:Upper(t.Name)|}.NotLike("X%")).Build();""",
            "Name",
            "wrapped in Upper");

    [Fact]
    public Task Where_WrappedIndexedColumnUnderBetween_Warns() =>
        RunReporting(
            """var s = Select(t.Id).From(t).Where({|#0:Upper(t.Name)|}.Between("A", "B")).Build();""",
            "Name",
            "wrapped in Upper");

    [Fact]
    public Task Where_WrappedIndexedColumnUnderIn_Warns() =>
        RunReporting(
            """var s = Select(t.Id).From(t).Where({|#0:Upper(t.Name)|}.In("A", "B")).Build();""",
            "Name",
            "wrapped in Upper");

    [Fact]
    public Task On_WrappedIndexedColumnUnderLike_Warns() =>
        RunReporting(
            """
            var s = Select(t.Id).From(t).InnerJoin(r).On({|#0:Upper(t.Name)|}.Like("X%")).Build();
            """,
            "Name",
            "wrapped in Upper");

    // A trailing wildcard leaves a prefix an index can range over; whether the
    // planner takes it is its own business.
    [Fact]
    public Task Where_TrailingWildcardLike_Silent() =>
        RunSilent("""var s = Select(t.Id).From(t).Where(t.Name.Like("x%")).Build();""");

    [Fact]
    public Task Where_PatternNotVisible_Silent() =>
        RunSilent("""var s = Select(t.Id).From(t).Where(t.Name.Like(Bind("%x"))).Build();""");

    // Outside a filter no index is at stake.
    [Fact]
    public Task Select_IndexedColumnWrappedInFunction_Silent() =>
        RunSilent("var s = Select(Upper(t.Name)).From(t).Build();");

    [Fact]
    public Task OrderBy_IndexedColumnWrappedInFunction_Silent() =>
        RunSilent("var s = Select(t.Id).From(t).OrderBy(Upper(t.Name)).Build();");

    // Aggregates in HAVING run after any index has done its work.
    [Fact]
    public Task Having_AggregateOverIndexedColumn_Silent() =>
        RunSilent(
            "var s = Select(t.Id, Count(t.Id)).From(t).GroupBy(t.Id).Having(Count(t.Id) "
                + "> 0).Build();");

    // A statement head inside a filter is not a wrapping function.
    [Fact]
    public Task Where_IndexedColumnInSubquery_Silent() =>
        RunSilent(
            "var s = Select(t.Id).From(t).Where(Exists(Select(r.Id).From(r).Where(r.Id == "
                + "t.Id))).Build();");

    [Fact]
    public Task Where_BareIndexedColumn_Silent() =>
        RunSilent("""var s = Select(t.Id).From(t).Where(t.Name == "x").Build();""");

    [Fact]
    public Task Where_UnindexedColumnWrappedInFunction_Silent() =>
        RunSilent("""var s = Select(t.Id).From(t).Where(Upper(t.Plain) == "X").Build();""");

    // The tri-state again: an index expression named this column, so the generator
    // recorded nothing and the rule claims nothing.
    [Fact]
    public Task Where_ColumnWithUndeterminedIndexing_Silent() =>
        RunSilent("""var s = Select(t.Id).From(t).Where(Upper(t.Expr) == "X").Build();""");

    // The condition is built apart from its clause, so nothing here shows it will
    // ever reach a WHERE.
    [Fact]
    public Task Where_ConditionHeldInLocal_Silent() =>
        RunSilent("""
            SqlCondition held = Upper(t.Name) == "X";
            var s = Select(t.Id).From(t).Where(held).Build();
            """);

    // The shape that shipped as a live false positive: a predicate-returning
    // function is exempt (FluentChain.IsCondition), not a wrapped column.
    [Fact]
    public Task Where_FullTextContains_Silent() =>
        RunSilent(
            """var s = Select(t.Id).From(t).Where(Contains(t.Name, "smith")).Build(Dbms.SqlServer);""",
            dbms: "sqlserver");

    [Fact]
    public Task Where_FullTextFreetext_Silent() =>
        RunSilent(
            """var s = Select(t.Id).From(t).Where(Freetext(t.Name, "smith")).Build(Dbms.SqlServer);""",
            dbms: "sqlserver");

    // ToTsvector is an expression wrap and stays reported deliberately: the cure is
    // a GIN expression index, which the generator then records as unknown.
    [Fact]
    public Task Where_TsMatchOverToTsvector_Warns() =>
        RunReporting(
            """var s = Select(t.Id).From(t).Where(TsMatch({|#0:ToTsvector(t.Name)|}, ToTsquery("x"))).Build();""",
            "Name",
            "wrapped in ToTsvector");

    [Fact]
    public Task Where_NoTargetConfigured_Silent() =>
        RunSilent(
            """var s = Select(t.Id).From(t).Where(Upper(t.Name) == "X").Build();""",
            dbms: null);

    // A pseudo-row factory references the proposed value of the column; it
    // wraps nothing, so the documented conditional-update idiom stays silent.
    [Fact]
    public Task Where_ExcludedIndexedColumnInUpsert_Silent() =>
        RunSilent(
            """var s = InsertInto(t, t.Name).Values("x").OnConflict(t.Name).DoUpdateSet(t.Name == Excluded(t.Name)).Where(Excluded(t.Name) != t.Name).Build();""");

    // A call that yields a builder stage or a column handle references the
    // column; only a call yielding an expression wraps it.
    [Fact]
    public Task Using_IndexedColumn_ThenWhere_Silent() =>
        RunSilent("""var s = Select(t.Id).From(t).InnerJoin(r).Using(t.Name).Where(t.Plain == "a").Build();""");

    [Fact]
    public Task Where_DerivedTableColumnHandle_Silent() =>
        RunSilent("""SubqueryDerivedTable d = Select(t.Name).From(t).AsTable("d"); var s = Select(d.Column(t.Name)).From(d).Where(d.Column(t.Name) == "x").Build();""");

    // A CASE branch marker carries the column without wrapping it in a function;
    // THEN and ELSE stay silent alike (the simple-CASE operand is the reported shape).
    [Fact]
    public Task Where_IndexedColumnInCaseElse_Silent() =>
        RunSilent(
            """var s = Select(t.Id).From(t).Where(Case(When(t.Plain == "a").Then("1"), Else(t.Name)) == "z").Build();""");

    [Fact]
    public Task Where_IndexedColumnInCaseThen_Silent() =>
        RunSilent(
            """var s = Select(t.Id).From(t).Where(Case(When(t.Plain == "a").Then(t.Name), Else("z")) == "z").Build();""");

    // The simple-CASE operand transforms the column the way Cast does.
    [Fact]
    public Task Where_IndexedColumnAsSimpleCaseOperand_Warns() =>
        RunReporting(
            """var s = Select(t.Id).From(t)"""
                + """.Where({|#0:Case(t.Name, When("a").Then("1"))|} == "z").Build();""",
            "Name",
            "wrapped in Case");

    // A predicate handed through a helper is a foreign invocation the rule
    // cannot read (release audit pass 8 pinned the IsForeignInvocation guard).
    [Fact]
    public Task Where_PredicateFromHelperMethod_Silent() =>
        RunSilent(
            """
            static SqlCondition Wrap(SqlCondition c) => c;
            var s = Select(t.Id).From(t).Where(Wrap(Upper(t.Name) == "x")).Build();
            """);

    // A pseudo-row factory references the column's affected value and wraps
    // nothing; only the context rule (SQLA0102, outside OUTPUT) speaks here.
    [Theory]
    [InlineData("Inserted")]
    [InlineData("Deleted")]
    public Task Where_PseudoRowReference_ReportsOnlyTheContextRule(string pseudoRow) =>
        RunAsync(
            Usage($$"""var s = Update(t).Set(t.Plain == "x")"""
                + $$""".Where({|#0:{{pseudoRow}}(t.Name)|} == "x").Build(Dbms.SqlServer);"""),
            [new DiagnosticResult("SQLA0102", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments(pseudoRow, "outside an OUTPUT clause", "SQL Server")],
            "sqlserver");
}
