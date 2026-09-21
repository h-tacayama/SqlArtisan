using System.Reflection;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

// The INSERT VALUES resolver and the expression resolver read the same value
// kinds (release audit pass 8: a scalar subquery was the gap); null is the one
// divergence — a VALUES element inserts SQL NULL, an expression position throws.
public class ResolverParityTests
{
    private static readonly MethodInfo s_insert = Resolver(
        "SqlArtisan.Internal.InsertValueResolver");
    private static readonly MethodInfo s_expression = Resolver(
        "SqlArtisan.Internal.ExpressionResolver");

    public static TheoryData<string> Values => new()
    {
        "column", "subquery", "int", "string", "datetime", "bindvalue",
    };

    [Theory]
    [MemberData(nameof(Values))]
    public void EveryResolvableKind_ResolvesToTheSameNodeType(string kind)
    {
        object value = Value(kind);

        Assert.Equal(
            s_expression.Invoke(null, [value])!.GetType(),
            s_insert.Invoke(null, [value])!.GetType());
    }

    [Fact]
    public void UnresolvableValue_BothThrowTheSameMessageFamily()
    {
        object value = new();

        string insert = Assert.IsType<ArgumentException>(
            Assert.Throws<TargetInvocationException>(() =>
                s_insert.Invoke(null, [value])).InnerException).Message;
        string expression = Assert.IsType<ArgumentException>(
            Assert.Throws<TargetInvocationException>(() =>
                s_expression.Invoke(null, [value])).InnerException).Message;

        Assert.StartsWith("Invalid type for InsertValue: ", insert);
        Assert.StartsWith("Invalid type for SqlExpression: ", expression);
    }

    private static object Value(string kind)
    {
        TestTable t = new("t");
        return kind switch
        {
            "column" => t.Code,
            "subquery" => Select(Max(t.Code)).From(t),
            "int" => 42,
            "string" => "x",
            "datetime" => new DateTime(2026, 1, 1),
            _ => Bind(7),
        };
    }

    private static MethodInfo Resolver(string typeName) =>
        typeof(Sql).Assembly.GetType(typeName)!
            .GetMethod("Resolve", BindingFlags.NonPublic | BindingFlags.Static, [typeof(object)])!;
}
