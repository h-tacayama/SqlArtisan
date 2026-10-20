using System.Reflection;

namespace SqlArtisan.Tests;

// The unordered windows — empty and partition-only — are the value family's alone. The
// ranking and offset functions are offered none, since OrderBy(Null) reads the same rows
// on PostgreSQL and SQLite, which run one (AnalyticFunction's remarks, #664).
public class WindowOverShapeTests
{
    [Fact]
    public void AnalyticFunction_DeclaresNoUnorderedOver()
    {
        MethodInfo[] declared = typeof(AnalyticFunction)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        Assert.All(
            declared,
            method => Assert.DoesNotContain(
                method.GetParameters(),
                parameter => parameter.ParameterType == typeof(PartitionByClause)));
        Assert.All(declared, method => Assert.NotEmpty(method.GetParameters()));
    }

    [Fact]
    public void ValueAnalyticFunction_DeclaresTheUnorderedOver()
    {
        MethodInfo? over = typeof(ValueAnalyticFunction)
            .GetMethod("Over", [typeof(PartitionByClause)]);

        Assert.NotNull(over);
        Assert.Equal(typeof(ValueAnalyticFunction), over.DeclaringType);
    }

    [Fact]
    public void ValueAnalyticFunction_DeclaresTheEmptyOver()
    {
        MethodInfo? over = typeof(ValueAnalyticFunction).GetMethod("Over", Type.EmptyTypes);

        Assert.NotNull(over);
        Assert.Equal(typeof(ValueAnalyticFunction), over.DeclaringType);
    }
}
