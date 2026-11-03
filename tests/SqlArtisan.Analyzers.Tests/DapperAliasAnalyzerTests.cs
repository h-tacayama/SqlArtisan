using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

namespace SqlArtisan.Analyzers.Tests;

public class DapperAliasAnalyzerTests
{
    // An alias naming a member of CustomerSummary, which OrderSummary does not have.
    private const string Miss = "{|#0:nameof(CustomerSummary.Name)|}";

    private const string Models = """
        sealed class T : DbTableBase
        {
            public DbColumn Code;
            public DbColumn Amount;
            public T(string alias = "") : base("t", alias) { Code = new DbColumn(this, "code"); Amount = new DbColumn(this, "amount"); }
        }

        class OrderSummary { public string Code { get; set; } public decimal Total { get; set; } }
        class CustomerSummary { public string Name { get; set; } }
        class Derived : OrderSummary { }
        class ColumnAttribute : Attribute { }
        """;

    private static string Usage(string statements, string types = "") => $$"""
        using System;
        using System.Collections.Generic;
        using System.Data;
        using SqlArtisan;
        using SqlArtisan.Dapper;
        using static SqlArtisan.Sql;

        {{Models}}
        {{types}}

        class C
        {
            void M(IDbConnection cnn)
            {
                T t = new T("t");
                Cte c = new Cte("c");
                {{statements}}
            }
        }
        """;

    private static async Task RunReporting(
        string statements,
        string types = "",
        string model = "OrderSummary")
    {
        var test = AnalyzerVerifier.Create(
            Usage(Expand(statements), types),
            AnalyzerVerifier.EditorConfig("postgresql")).WithDapper();
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0304")
            .WithLocation(0)
            .WithArguments("Name", model));
        await test.RunAsync();
    }

    private static async Task RunSilent(
        string statements,
        string types = "",
        string? dbms = "postgresql")
    {
        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(Usage(Expand(statements), types)),
            dbms is null ? null : AnalyzerVerifier.EditorConfig(dbms)).WithDapper();
        await test.RunAsync();
    }

    // SEL is a root select whose one alias misses; HIT is an alias that names a member.
    private static string Expand(string statements) => statements
        .Replace("SEL", "Select(t.Code.As(MISS)).From(t)")
        .Replace("MISS", Miss)
        .Replace("HIT", "nameof(OrderSummary.Code)");

    [Theory]
    [InlineData("Query")]
    [InlineData("QueryAsync")]
    [InlineData("QueryFirst")]
    [InlineData("QueryFirstAsync")]
    [InlineData("QueryFirstOrDefault")]
    [InlineData("QueryFirstOrDefaultAsync")]
    [InlineData("QuerySingle")]
    [InlineData("QuerySingleAsync")]
    [InlineData("QuerySingleOrDefault")]
    [InlineData("QuerySingleOrDefaultAsync")]
    public Task Verb_AliasNamingNoMember_ReportsSqla0304(string verb) =>
        RunReporting($"var r = cnn.{verb}<OrderSummary>(Select(t.Code.As({Miss})).From(t));");

    [Fact]
    public Task Alias_NamingAMember_Silent() =>
        RunSilent("var r = cnn.Query<OrderSummary>(Select(t.Code.As(HIT)).From(t));");

    [Fact]
    public Task LongerChain_ReportsSqla0304() =>
        RunReporting($"""
            var r = cnn.Query<OrderSummary>(
                Select(t.Code.As({Miss}), Sum(t.Amount).As(nameof(OrderSummary.Total)))
                .From(t)
                .Where(t.Code == "x")
                .GroupBy(t.Code)
                .OrderBy(t.Code));
            """);

    [Fact]
    public Task InstanceNameOf_ReportsSqla0304() =>
        RunReporting("""
            CustomerSummary cs;
            var r = cnn.Query<OrderSummary>(Select(t.Code.As({|#0:nameof(cs.Name)|})).From(t));
            """);

    [Fact]
    public Task SubqueryItemAlias_ReportsSqla0304() =>
        RunReporting($"""
            var r = cnn.Query<OrderSummary>(
                Select(t.Code.As(nameof(OrderSummary.Code)), Select(Max(t.Amount)).From(t).As({Miss}))
                .From(t));
            """);

    [Fact]
    public Task WithSelect_ReportsSqla0304() =>
        RunReporting($"""
            var r = cnn.Query<OrderSummary>(
                With(c.As(Select(t.Code).From(t))).Select(c.Column(t.Code).As({Miss})).From(c));
            """);

    [Fact]
    public Task ChainThroughTwoLocals_ReportsSqla0304() =>
        RunReporting($"""
            ISelectBuilderFrom b = Select(t.Code.As({Miss})).From(t);
            ISqlBuilder q = b.Where(t.Code == "x").OrderBy(t.Code);
            var r = cnn.Query<OrderSummary>(q);
            """);

    [Fact]
    public Task DerivedModel_ReportsSqla0304() =>
        RunReporting(
            $"var r = cnn.Query<Derived>(Select(t.Code.As({Miss})).From(t));",
            model: "Derived");

    // Each match form Dapper may use, against the reporting cases above.
    [Theory]
    [InlineData("class M { public string NAME { get; set; } }")]
    [InlineData("class M { public string N_a_me { get; set; } }")]
    [InlineData("class Base { public string Name; } class M : Base { }")]
    [InlineData("class M { private string name; public override string ToString() => name; }")]
    [InlineData("class M { public M(string name) { } }")]
    public Task Alias_MatchingAMemberDapperCouldFill_Silent(string model) =>
        RunSilent($"var r = cnn.Query<M>(Select(t.Code.As({Miss})).From(t));", model);

    [Theory]
    [InlineData("abstract class M { }")]
    [InlineData("interface M { }")]
    [InlineData("enum M { A }")]
    [InlineData("[Column] class M { }")]
    [InlineData("class M { [Column] public string X { get; set; } }")]
    [InlineData("class M { public M([Column] string x) { } }")]
    [InlineData("[Column] class B { } class M : B { }")]
    public Task Model_ShapeTheAliasDoesNotDecide_Silent(string model) =>
        RunSilent($"var r = cnn.Query<M>(Select(t.Code.As({Miss})).From(t));", model);

    [Theory]
    [InlineData("object")]
    [InlineData("dynamic")]
    [InlineData("(string Name, int X)")]
    [InlineData("OrderSummary[]")]
    [InlineData("int")]
    [InlineData("string")]
    [InlineData("DateOnly")]
    [InlineData("S?")]
    public Task Model_ReadWithoutItsMemberNames_Silent(string model) =>
        RunSilent(
            $"var r = cnn.Query<{model}>(Select(t.Code.As({Miss})).From(t));",
            "struct S { public int X; }");

    [Fact]
    public Task Model_TypeParameter_Silent() =>
        RunSilent(string.Empty, $$"""
            class G
            {
                void M<TM>(IDbConnection cnn)
                {
                    T t = new T("t");
                    var r = cnn.Query<TM>(Select(t.Code.As({{Miss}})).From(t));
                }
            }
            """);

    [Fact]
    public async Task Model_InAReferencedProject_Silent()
    {
        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(Usage(
                $"var r = cnn.Query<RemoteSummary>(Select(t.Code.As({Miss})).From(t));")),
            "public class RemoteSummary { public string Code { get; set; } }",
            AnalyzerVerifier.EditorConfig("postgresql")).WithDapper();
        await test.RunAsync();
    }

    [Fact]
    public async Task Model_InAReferencedAssembly_Silent()
    {
        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(Usage(
                $"var r = cnn.Query<RemoteSummary>(Select(t.Code.As({Miss})).From(t));")),
            AnalyzerVerifier.EditorConfig("postgresql")).WithDapper();
        test.TestState.AdditionalReferences.Add(await Emit(
            "Models",
            "public class RemoteSummary { public string Code { get; set; } }"));
        await test.RunAsync();
    }

    [Fact]
    public async Task Model_DerivingFromAReferencedType_Silent()
    {
        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(Usage(
                $"var r = cnn.Query<Local>(Select(t.Code.As({Miss})).From(t));",
                "class Local : RemoteBase { }")),
            AnalyzerVerifier.EditorConfig("postgresql")).WithDapper();
        test.TestState.AdditionalReferences.Add(await Emit(
            "Models",
            "public class RemoteBase { public string Code { get; set; } }"));
        await test.RunAsync();
    }

    [Fact]
    public async Task DapperFluentMapReferenced_Silent()
    {
        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(Usage(
                $"var r = cnn.Query<OrderSummary>(Select(t.Code.As({Miss})).From(t));")),
            AnalyzerVerifier.EditorConfig("postgresql")).WithDapper();
        test.TestState.AdditionalReferences.Add(await Emit(
            "Dapper.FluentMap",
            "namespace Dapper.FluentMap { public static class FluentMapper { } }"));
        await test.RunAsync();
    }

    // Each way the query can reach the call without the rule seeing its root fixed,
    // against ChainThroughTwoLocals_ReportsSqla0304.
    [Theory]
    [InlineData("ISqlBuilder q = SEL; q = Select(t.Code).From(t);")]
    [InlineData("ISqlBuilder q = SEL; Action a = () => q = Select(t.Code).From(t);")]
    [InlineData("ISqlBuilder q = SEL; Helpers.Ref(ref q);")]
    [InlineData("ISqlBuilder q = SEL; Helpers.Out(out q);")]
    [InlineData("ISqlBuilder q = SEL; int n; (q, n) = (Helpers.Other(), 1);")]
    [InlineData("ISqlBuilder q; q = SEL;")]
    [InlineData("bool f = true; ISqlBuilder q = f ? SEL : Select(t.Code).From(t);")]
    [InlineData("ISqlBuilder o = null; ISqlBuilder q = o ?? SEL;")]
    [InlineData("int k = 1; ISqlBuilder q = k switch { _ => SEL };")]
    [InlineData("Holders.Field = SEL; ISqlBuilder q = Holders.Field;")]
    [InlineData("ISqlBuilder q = Holders.Property;")]
    public Task Chain_NotFollowed_Silent(string declaration) =>
        RunSilent(declaration + " var r = cnn.Query<OrderSummary>(q);", Holders);

    // A write through a ref alias or a ref conditional reaches q under another name.
    [Theory]
    [InlineData("ref ISqlBuilder a = ref q; a = Select(t.Code).From(t);")]
    [InlineData("ref ISqlBuilder a = ref (f ? ref q : ref x); a = Select(t.Code).From(t);")]
    [InlineData("(f ? ref q : ref x) = Select(t.Code).From(t);")]
    [InlineData("Helpers.Ref(ref f ? ref q : ref x);")]
    public Task Chain_WrittenThroughARef_Silent(string write) =>
        RunSilent(
            "ISqlBuilder q = SEL; ISqlBuilder x = null; bool f = true; "
                + write
                + " var r = cnn.Query<OrderSummary>(q);",
            Holders);

    [Fact]
    public Task Chain_FromHelperMethod_Silent() =>
        RunSilent("var r = cnn.Query<OrderSummary>(Holders.Method());", Holders);

    private const string Holders = $$"""
        static class Helpers
        {
            public static void Ref(ref ISqlBuilder b) { }
            public static void Out(out ISqlBuilder b) { b = null; }
            public static ISqlBuilder Other() => null;
        }

        static class Holders
        {
            public static ISqlBuilder Field;
            public static ISqlBuilder Property => Select(new T().Code.As({{Miss}})).From(new T());
            public static ISqlBuilder Method() => Select(new T().Code.As({{Miss}})).From(new T());
        }
        """;

    // The rule reads only the aliases that name the result columns of the root select.
    [Theory]
    [InlineData("SEL.UnionAll.Select(t.Code).From(t)")]
    [InlineData("InsertInto(t, t.Code).With(c.As(Select(t.Code).From(t))).SEL")]
    [InlineData("Select(t.Code.As(HIT)).From(t).Where(Exists(SEL))")]
    [InlineData("InsertInto(t, t.Code).Values(\"x\").Returning(t.Code.As(MISS))")]
    [InlineData("Select(new object[] { t.Code.As(MISS) }).From(t)")]
    public Task Alias_NotARootResultColumn_Silent(string query) =>
        RunSilent($"var r = cnn.Query<OrderSummary>({query});");

    [Theory]
    [InlineData("string Name = \"Name\";", "t.Code.As(nameof(Name))")]
    [InlineData("", "t.Code.As(\"Name\")")]
    [InlineData("", "t.Code.As(t.Amount)")]
    [InlineData("ExpressionAlias a = t.Code.As(MISS);", "a")]
    public Task Alias_NotAMemberNameOf_Silent(string declaration, string item) =>
        RunSilent($"{declaration} var r = cnn.Query<OrderSummary>(Select({item}).From(t));");

    [Theory]
    [InlineData("var r = cnn.Query(SEL);")]
    [InlineData("var r = cnn.ExecuteScalar<string>(SEL);")]
    public Task Verb_NotATypedQuery_Silent(string statements) =>
        RunSilent(statements);

    [Fact]
    public Task NoDialectConfigured_Silent() =>
        RunSilent(
            $"var r = cnn.Query<OrderSummary>(Select(t.Code.As({Miss})).From(t));",
            dbms: null);

    // A look-alike declared in source is not SqlArtisan.Dapper's verb.
    [Fact]
    public async Task SqlArtisanDapperNotReferenced_Silent()
    {
        string source = $$"""
            using System;
            using System.Collections.Generic;
            using System.Data;
            using SqlArtisan;
            using SqlArtisan.Dapper;
            using static SqlArtisan.Sql;

            namespace SqlArtisan.Dapper
            {
                static class SqlMapper
                {
                    public static IEnumerable<TM> Query<TM>(this IDbConnection c, ISqlBuilder sqlBuilder) => null;
                }
            }

            {{Models}}

            class C
            {
                void M(IDbConnection cnn)
                {
                    T t = new T("t");
                    var r = cnn.Query<OrderSummary>(Select(t.Code.As(nameof(CustomerSummary.Name))).From(t));
                }
            }
            """;
        var test = AnalyzerVerifier.Create(source, AnalyzerVerifier.EditorConfig("postgresql"));
        await test.RunAsync();
    }

    private static async Task<MetadataReference> Emit(string assemblyName, string source)
    {
        System.Collections.Immutable.ImmutableArray<MetadataReference> references =
            await AnalyzerVerifier.Net80.ResolveAsync(
            LanguageNames.CSharp, default);
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using MemoryStream image = new();
        Microsoft.CodeAnalysis.Emit.EmitResult result = compilation.Emit(image);
        Assert.True(
            result.Success,
            string.Join("\n", result.Diagnostics.Select(d => d.ToString())));
        return MetadataReference.CreateFromImage(image.ToArray());
    }
}
