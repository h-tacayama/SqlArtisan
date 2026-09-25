using System.Reflection;
using SqlArtisan.Internal;

namespace SqlArtisan.Tests;

// The unordered windows — empty and partition-only — are the value family's alone:
// Oracle raises ORA-30485 for the ranking and offset functions
// (OracleTests.UnorderedWindow_RankingFamily_Rejected).
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
