using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>
/// Ties SQLA0304 to what it names by hand: SqlArtisan.Dapper's typed verbs, the types Dapper
/// reads without their member names, and the core's set operators being properties.
/// </summary>
public class DapperAliasParityTests
{
    // A verb is an extension on the connection; AddTypeHandler<T> runs no query (#810).
    [Fact]
    public void EveryTypedDapperVerb_IsClassified()
    {
        string[] verbs = [.. typeof(SqlArtisan.Dapper.SqlMapper)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.IsDefined(typeof(ExtensionAttribute)))
            .Where(m => m.IsGenericMethodDefinition && m.GetGenericArguments().Length == 1)
            .Select(m => m.Name)
            .Distinct()
            .OrderBy(n => n, StringComparer.Ordinal)];
        string[] classified = [.. DapperAliasRule.CheckedVerbs
            .Concat(DapperAliasRule.ExcludedVerbs)
            .OrderBy(n => n, StringComparer.Ordinal)];

        Assert.Empty(DapperAliasRule.CheckedVerbs.Intersect(DapperAliasRule.ExcludedVerbs));
        Assert.Equal(classified, verbs);
    }

    // The walk passes only through method calls, so it is a set operator's being a property
    // that keeps a later branch's root from being read as the query's.
    [Fact]
    public void EveryCoreSetOperator_IsAProperty()
    {
        string[] methods = [.. typeof(Sql).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static
                    | BindingFlags.DeclaredOnly))
            .Where(m => !m.IsSpecialName && FluentChain.SetOperators.Contains(m.Name))
            .Select(m => $"{m.DeclaringType!.Name}.{m.Name}")];

        Assert.Empty(methods);
    }

    // Dapper reads these by position whatever the alias (#716 records the probe), and the
    // rule reaches that silence through their being declared outside the compilation.
    [Fact]
    public async Task EveryTypeDapperReadsByPosition_IsSilent()
    {
        string[] statements = [.. DapperField("typeMap")
            .Concat(DapperField("typeHandlers"))
            .Select(type => $"cnn.Query<{CSharpName(type)}>("
                + "Select(t.Code.As(nameof(M.Unmatched))).From(t));")];
        string source = $$"""
            using System.Data;
            using SqlArtisan;
            using SqlArtisan.Dapper;
            using static SqlArtisan.Sql;

            sealed class T : DbTableBase
            {
                public DbColumn Code;
                public T(string alias = "") : base("t", alias) { Code = new DbColumn(this, "code"); }
            }

            class M { public string Unmatched { get; set; } }

            class C
            {
                void Run(IDbConnection cnn)
                {
                    T t = new T("t");
                    {{string.Join("\n            ", statements)}}
                }
            }
            """;

        Assert.NotEmpty(statements);
        await AnalyzerVerifier.Create(source, AnalyzerVerifier.EditorConfig("postgresql"))
            .WithDapper()
            .RunAsync();
    }

    private static IEnumerable<Type> DapperField(string name) =>
        ((IDictionary)typeof(global::Dapper.SqlMapper)
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!)
            .Keys
            .Cast<Type>();

    private static string CSharpName(Type type) =>
        type.IsArray
            ? CSharpName(type.GetElementType()!) + "[]"
            : Nullable.GetUnderlyingType(type) is { } underlying
                ? $"global::System.Nullable<{CSharpName(underlying)}>"
                : "global::" + type.FullName!.Replace('+', '.');
}
