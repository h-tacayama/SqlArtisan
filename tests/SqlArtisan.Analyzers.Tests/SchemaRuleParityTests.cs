using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>
/// The empirical no-false-positive gate for the schema rules (SQLA0007–SQLA0012):
/// one catalog of hazard shapes, asserted silent against every rule that reads the
/// surrounding query, so a shape added here becomes a regression test for all of
/// them at once.
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

            [DbColumnMetadata(Nullable = false, HasDefault = false, TypeCategory = DbTypeCategory.Text)]
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
            SqlCondition mismatched = t.Code == 1;
            var s = Select(t.Code).From(t).Where(mismatched).Build();
            """,
            ""
        },
        {
            "var s = Update(t).Set(Assigned(t)).Build();",
            "static EqualityBasedCondition Assigned(T t) => t.Code == 1;"
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
            .Where(name => name.EndsWith("Join") || name.EndsWith("Lateral") || name.EndsWith("Apply"))
            .Distinct()
            .OrderBy(name => name, System.StringComparer.Ordinal)];

        string[] classified = [.. FluentChain.OuterJoinSteps.Concat(InnerJoinSteps)
            .OrderBy(name => name, System.StringComparer.Ordinal)];

        Assert.Equal(classified, joinSteps);
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
            "var s = Select(t.Code).From(t).InnerJoin(r).On(t.Code == r.Code).Where({|#0:r.Code.IsNull|}).Build();",
            new DiagnosticResult("SQLA0007", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("Code", "IsNull", "false"));

    [Fact]
    public Task CountNullableColumn_PlainQuery_Reports() =>
        RunReporting(
            "var s = Select({|#0:Count(t.Note)|}).From(t).Build();",
            new DiagnosticResult("SQLA0010", DiagnosticSeverity.Info)
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

    // SQLA0010 is opt-in, so the whole family is only observable with it named.
    private static string EditorConfig(string dbms) => $"""
        root = true

        [*.cs]
        sqlartisan_target_dbms = {dbms}
        dotnet_diagnostic.SQLA0010.severity = suggestion
        """;
}
