using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>
/// The empirical no-false-positive gate for the schema rules (SQLA0200–SQLA0205):
/// one catalog of hazard shapes asserted silent against every rule that reads the
/// surrounding query, so a shape added here regresses all of them at once.
/// </summary>
/// <remarks>
/// Six of these shapes shipped as live false positives before the gate existed —
/// three NATURAL join forms and three ways of assembling a chain across
/// statements. Adding a shape is how a new hazard enters the contract.
/// </remarks>
public class SchemaRuleParityTests
{
    private static readonly Assembly Core = typeof(Sql).Assembly;

    // A join step that null-supplies no row, so a schema rule may still judge the
    // column past it.
    private static readonly string[] InnerJoinSteps =
    [
        "CrossApply", "CrossJoin", "CrossJoinLateral", "InnerJoin", "JoinLateral", "NaturalJoin"
    ];

    private static string Usage(string statements, string members = "") => $$"""
        using SqlArtisan;
        using SqlArtisan.Internal;
        using static SqlArtisan.Sql;

        class T : DbTableBase
        {
            public T(string alias = "") : base("t", alias)
            {
                Code = new DbColumn(this, "code");
                Note = new DbColumn(this, "note");
                Key = new DbColumn(this, "key");
            }

            [DbColumnMetadata(Nullable =
                false, HasDefault = false, TypeCategory = DbTypeCategory.Text)]
            public DbColumn Code { get; }

            [DbColumnMetadata(Nullable = true, HasDefault = false)]
            public DbColumn Note { get; }

            [DbColumnMetadata(Nullable = true, HasDefault = false, Indexed = true)]
            public DbColumn Key { get; }
        }

        class C
        {
            {{members}}

            void M()
            {
                T t = new T();
                T r = new T("r");
                DerivedTable d = new DerivedTable("d");
                {{statements}}
            }
        }
        """;

    // Each entry is (join step as written, the target it is supported on).
    public static TheoryData<string, string> NullSupplyingJoins => new()
    {
        { "LeftJoin(r).On(t.Code == r.Code)", "postgresql" },
        { "RightJoin(r).On(t.Code == r.Code)", "postgresql" },
        { "FullJoin(r).On(t.Code == r.Code)", "postgresql" },
        { "NaturalLeftJoin(r)", "postgresql" },
        { "NaturalRightJoin(r)", "postgresql" },
        { "NaturalFullJoin(r)", "postgresql" },
        { "LeftJoinLateral(Select(t.Code).From(t), d)", "postgresql" },
        { "OuterApply(Select(t.Code).From(t), d)", "sqlserver" },
    };

    // A chain the reporting statement cannot see the whole of: the join that
    // decides the answer — or the predicate that would silence the rule — is one
    // statement, one method, or one field away.
    public static TheoryData<string, string> HiddenChains => new()
    {
        {
            """
            var prefix = Select(t.Code).From(t).LeftJoin(r).On(t.Code == r.Code);
            var s = prefix.Where(r.Code.IsNull).Build();
            """,
            ""
        },
        {
            """
            SqlCondition c = r.Code.IsNull;
            var s = Select(t.Code).From(t).LeftJoin(r).On(t.Code == r.Code).Where(c).Build();
            """,
            ""
        },
        {
            "var s = Joined(t, r).Where(r.Code.IsNull).Build();",
            """
            static ISelectBuilderFrom Joined(T t, T r) =>
                Select(t.Code).From(t).LeftJoin(r).On(t.Code == r.Code);
            """
        },
        {
            "var s = Select(t.Code).From(t).LeftJoin(r).On(t.Code == r.Code).Where(Held).Build();",
            "static SqlCondition Held = new T(\"r\").Code.IsNull;"
        },
        {
            """
            var counted = Count(r.Note);
            var s = Select(t.Code, counted).From(t).LeftJoin(r).On(t.Code == r.Code).GroupBy(t.Code).Build();
            """,
            ""
        },
        {
            """
            SqlCondition wrapped = Upper(t.Key) == "X";
            var s = Select(t.Code).From(t).Where(wrapped).Build();
            """,
            ""
        },
        {
            """
            var s = InsertInto(t, t.Code, t.Key).Values("x", "y").OnConflict(t.Key).DoUpdateSet(t.Key == Excluded(t.Key)).Where(Excluded(t.Key) != t.Key).Build();
            """,
            ""
        },
        {
            """
            var s = Select(t.Code).From(t).InnerJoin(r).Using(t.Key).Where(t.Code == "a").Build();
            """,
            ""
        },
        {
            """
            SubqueryDerivedTable x = Select(t.Key).From(t).AsTable("x");
            var s = Select(x.Column(t.Key)).From(x).Where(x.Column(t.Key) == "k").Build();
            """,
            ""
        },
        // A compound subquery is inside its enclosing statement: the outer join
        // that silences the predicate sits above the set operator, not beside it.
        {
            """
            var s = Select(t.Code).From(t).LeftJoin(r).On(t.Code == r.Code).Where(Exists(Select(d.Column("c")).From(d).Where(r.Code.IsNull).Union.Select(d.Column("c")).From(d))).Build();
            """,
            ""
        },
        // The branch before a set operator keeps its own joins: the compound's
        // remaining chain is walked through, not skipped as a nested statement.
        {
            """
            var s = Select(t.Code, Count(r.Note)).From(t).LeftJoin(r).On(t.Code == r.Code).Where(r.Code.IsNull).GroupBy(t.Code).Union.Select(t.Code, Count(t.Code)).From(t).GroupBy(t.Code).Build();
            """,
            ""
        },
        {
            """
            var s = Select(t.Code).From(t).Union.Select(t.Code).From(t).LeftJoin(r).On(t.Code == r.Code).Where(r.Code.IsNull).Union.Select(t.Code).From(t).Build();
            """,
            ""
        },
        // A CASE branch marker carries the column without wrapping it.
        {
            """
            var s = Select(t.Code).From(t).Where(Case(When(t.Code == "a").Then("1"), Else(t.Key)) == "z").Build();
            """,
            ""
        },
        {
            """
            SqlCondition mismatched = t.Code == 1;
            var s = Select(t.Code).From(t).Where(mismatched).Build();
            """,
            ""
        },
        {
            "var s = Update(t).Set(Assigned(t)).Build();",
            "static EqualityCondition Assigned(T t) => t.Code == 1;"
        },
        // A static factory wrapping the predicate must not re-open a chain or a
        // condition the statement still cannot see.
        {
            """
            var prefix = Select(t.Code).From(t).LeftJoin(r).On(t.Code == r.Code);
            var s = prefix.Where(Not(t.Code.IsNull)).Build();
            """,
            ""
        },
        {
            """
            SqlCondition c = r.Code.IsNull;
            var s = Select(t.Code).From(t).Where(ConditionIf(true, c)).Build();
            """,
            ""
        },
        {
            """
            SqlCondition filter = r.Note.IsNotNull;
            var s = Select(t.Code).From(t).Where(t.Code.NotIn(Select(r.Note).From(r).Where(filter))).Build();
            """,
            ""
        },
        {
            """
            DbColumn c = r.Note;
            var s = Select(t.Code).From(t).Where(t.Code.NotIn(Select(r.Note).From(r).Where(c.IsNotNull))).Build();
            """,
            ""
        },
        {
            "var s = Select(t.Code).From(t).Where(t.Code.NotIn(Select(r.Note).From(r)"
                + ".Where(Col.IsNotNull))).Build();",
            "static DbColumn Col => new T(\"r\").Note;"
        },
        // A helper outside SqlArtisan may place its argument anywhere, so the
        // chain visible above the call is not the one the argument ends in.
        {
            "var s = Select(t.Code).From(t).Where(Wrap(t.Code.IsNull)).Build();",
            "static SqlCondition Wrap(SqlCondition c) => c;"
        },
        {
            "var s = Select(Wrap(Count(t.Note))).From(t).Build();",
            "static SqlExpression Wrap(SqlExpression e) => e;"
        },
        {
            "var s = Select(t.Code).From(t).Where(Wrap(t.Code == 1)).Build();",
            "static SqlCondition Wrap(SqlCondition c) => c;"
        },
        {
            "var s = Select(t.Code).From(t).Where(Wrap(Upper(t.Key) == \"X\")).Build();",
            "static SqlCondition Wrap(SqlCondition c) => c;"
        },
    };

    /// <summary>
    /// The gate that catches a join step shipping unclassified: every join the
    /// core exposes is either null-supplying or deliberately listed as inner.
    /// </summary>
    [Fact]
    public void EveryCoreJoinStep_IsClassified()
    {
        string[] joinSteps = [.. Core.GetExportedTypes()
            .Where(t => t.IsInterface)
            .SelectMany(t => t.GetMethods())
            .Select(m => m.Name)
            .Where(
                name => name.EndsWith("Join") || name.EndsWith("Lateral") || name.EndsWith("Apply"))
            .Distinct()
            .OrderBy(name => name, System.StringComparer.Ordinal)];

        string[] classified = [.. FluentChain.OuterJoinSteps.Concat(InnerJoinSteps)
            .OrderBy(name => name, System.StringComparer.Ordinal)];

        Assert.Equal(classified, joinSteps);
    }

    [Fact]
    public void EveryCoreSetOperator_IsClassified()
    {
        string[] setOperators = [.. Core.GetExportedTypes()
            .Where(t => t.IsInterface)
            .SelectMany(t => t.GetProperties())
            .Where(p => p.PropertyType.Name == "ISelectBuilderSetOperator")
            .Select(p => p.Name)
            .Distinct()
            .OrderBy(name => name, System.StringComparer.Ordinal)];

        string[] classified = [.. FluentChain.SetOperators
            .OrderBy(name => name, System.StringComparer.Ordinal)];

        Assert.Equal(classified, setOperators);
    }

    // A factory handing back a pseudo-row column reference (EXCLUDED, INSERTED,
    // DELETED) wraps nothing; the index rule must not read it as a function.
    [Fact]
    public void EveryCorePseudoRowFactory_IsClassified()
    {
        string[] factories = [.. typeof(Sql).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.ReturnType.Name.EndsWith("Column") && m.ReturnType != typeof(DbColumn))
            .Select(m => m.Name)
            .Distinct()
            .OrderBy(name => name, System.StringComparer.Ordinal)];

        string[] classified = [.. UnusableIndexPredicateRule.PseudoRowReferences
            .OrderBy(name => name, System.StringComparer.Ordinal)];

        Assert.Equal(classified, factories);
    }

    // A core member taking a bare DbColumn and yielding an expression is what the
    // index rule reads as a wrapping (a stage or DbColumn result is a reference); the
    // set must stay the three pseudo-row factories, or a new member ships a false positive.
    [Fact]
    public void EveryCoreBareColumnMemberYieldingAnExpression_IsAPseudoRowFactory()
    {
        string[] wrappings = [.. Core.GetExportedTypes()
            .SelectMany(t => t.GetMethods(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
            .Where(m => m.GetParameters().Any(p => p.ParameterType == typeof(DbColumn))
                && typeof(SqlExpression).IsAssignableFrom(m.ReturnType)
                && m.ReturnType != typeof(DbColumn)
                && !typeof(SqlCondition).IsAssignableFrom(m.ReturnType))
            .Select(m => m.Name)
            .Distinct()
            .OrderBy(name => name, System.StringComparer.Ordinal)];

        Assert.Equal(
            UnusableIndexPredicateRule.PseudoRowReferences
                .OrderBy(n => n, System.StringComparer.Ordinal),
            wrappings);
    }

    // Every object-argument Sql factory yielding a non-function, non-operator
    // expression is classified here — carrier, wrapping, or never-a-column. One
    // taking a typed SqlExpression (All/Any/Some) is outside this population.
    [Fact]
    public void EveryCoreObjectArgumentExpressionFactory_IsClassified()
    {
        string[] unclassified = [.. typeof(Sql).GetMethods(
            BindingFlags.Public | BindingFlags.Static)
            .Where(m => typeof(SqlExpression).IsAssignableFrom(m.ReturnType)
                && !typeof(SqlCondition).IsAssignableFrom(m.ReturnType)
                && !m.ReturnType.Name.EndsWith("Function", System.StringComparison.Ordinal)
                && !m.ReturnType.Name.EndsWith("Operator", System.StringComparison.Ordinal)
                && m.GetParameters().Any(p => p.ParameterType == typeof(object)))
            .Select(m => m.Name)
            .Distinct()
            .OrderBy(name => name, System.StringComparer.Ordinal)];

        // A simple CASE operand and CAST transform the column exactly as a function
        // does; Bind and Interval take a value, never a column reference.
        string[] wrappings = ["Case", "Cast"];
        string[] nonColumn = ["Bind", "Interval"];

        Assert.Equal(
            UnusableIndexPredicateRule.ExpressionCarriers
                .Concat(wrappings)
                .Concat(nonColumn)
                .OrderBy(n => n, System.StringComparer.Ordinal),
            unclassified);
    }

    [Fact]
    public void EveryCoreAssignmentStep_IsClassified()
    {
        string[] steps = [.. Core.GetExportedTypes()
            .Where(t => t.IsInterface)
            .SelectMany(t => t.GetMethods())
            .Where(m => m.GetParameters().Any(p => p.ParameterType == typeof(EqualityCondition[])))
            .Select(m => m.Name)
            .Distinct()
            .OrderBy(name => name, System.StringComparer.Ordinal)];

        string[] classified = [.. TypeCategoryMismatchRule.AssignmentSteps
            .OrderBy(name => name, System.StringComparer.Ordinal)];

        Assert.Equal(classified, steps);
    }

    // Roslyn orders Arguments as written, so a positional index misreads a
    // named call: index 0 is safe only on a call checked to carry one
    // argument, and a higher index never is — the rules look up by name.
    [Fact]
    public void NoRule_IndexesASecondInvocationArgumentByPosition()
    {
        string root = FindRepoRoot();
        string analyzers = Path.Combine(root, "src", "SqlArtisan.Analyzers");
        System.Text.RegularExpressions.Regex positional = new(@"\bArguments\[[1-9]");
        string[] offenders = [.. Directory.EnumerateFiles(analyzers, "*.cs")
            .Where(f => positional.IsMatch(File.ReadAllText(f)))
            .Select(f => Path.GetFileName(f))
            .OrderBy(n => n, System.StringComparer.Ordinal)];

        Assert.Empty(offenders);
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SqlArtisan.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir.FullName;
    }

    [Theory]
    [MemberData(nameof(NullSupplyingJoins))]
    public Task ConstantNullPredicate_PastNullSupplyingJoin_Silent(string join, string dbms) =>
        RunSilent($"var s = Select(t.Code).From(t).{join}.Where(t.Code.IsNull).Build();", dbms);

    [Theory]
    [MemberData(nameof(NullSupplyingJoins))]
    public Task CountNullableColumn_PastNullSupplyingJoin_Silent(string join, string dbms) =>
        RunSilent(
            $"var s = Select(t.Code, Count(t.Note)).From(t).{join}.GroupBy(t.Code).Build();",
            dbms);

    [Theory]
    [MemberData(nameof(HiddenChains))]
    public Task SchemaRule_ChainNotVisibleInStatement_Silent(string statements, string members) =>
        RunSilent(statements, "postgresql", members);

    // The suppressions must not swallow everything: an inner join null-supplies
    // nothing, so the rules still report past one.
    [Fact]
    public Task ConstantNullPredicate_PastInnerJoin_Reports() =>
        RunReporting(
            "var s = Select(t.Code).From(t).InnerJoin(r).On(t.Code == "
                + "r.Code).Where({|#0:r.Code.IsNull|}).Build();",
            new DiagnosticResult("SQLA0200", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("Code", "IsNull", "false"));

    [Fact]
    public Task CountNullableColumn_PlainQuery_Reports() =>
        RunReporting(
            "var s = Select({|#0:Count(t.Note)|}).From(t).Build();",
            new DiagnosticResult("SQLA0203", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("Note"));

    private static Task RunSilent(string statements, string dbms, string members = "")
    {
        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(Usage(statements, members)),
            EditorConfig(dbms));
        return test.RunAsync();
    }

    private static Task RunReporting(string statements, DiagnosticResult expected)
    {
        var test = AnalyzerVerifier.Create(Usage(statements), EditorConfig("postgresql"));
        test.ExpectedDiagnostics.Add(expected);
        return test.RunAsync();
    }

    // SQLA0203 is opt-in, so the whole family is only observable with it named.
    private static string EditorConfig(string dbms) => $"""
        root = true

        [*.cs]
        sqlartisan_syntax_{dbms} = any
        dotnet_diagnostic.SQLA0203.severity = suggestion
        """;
}
