using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>
/// Pins the core-API facts the SQLA0102 context rules key on (the ADR 0009
/// name contract): if the API drifts, these fail loudly instead of the rules
/// silently dying or — worse — losing their no-false-positive soundness.
/// </summary>
public class ContextRuleContractTests
{
    private static readonly Assembly Core = typeof(Sql).Assembly;

    [Theory]
    [InlineData(typeof(SqlExpression), "In")]
    [InlineData(typeof(SqlExpression), "NotIn")]
    [InlineData(typeof(Sql), "Any")]
    [InlineData(typeof(Sql), "All")]
    [InlineData(typeof(Sql), "Some")]
    public void QuantifiedHost_HasExactlyOneSingleSubqueryOverload(Type host, string methodName)
    {
        MethodInfo[] subqueryOverloads = [.. host
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(m => m.Name == methodName
                && m.GetParameters().Any(p => p.ParameterType == typeof(ISubquery)))];

        MethodInfo overload = Assert.Single(subqueryOverloads);
        Assert.Single(overload.GetParameters());
    }

    [Fact]
    public void WithRollup_IsReachableOnlyFromTheGroupByStage()
    {
        // The rule's absence proof: a chain whose call after GroupBy is not
        // WithRollup can never acquire WITH ROLLUP later.
        List<string> declaringInterfaces = [.. Core.GetExportedTypes()
            .Where(t => t.IsInterface)
            .Where(t => t.GetMethods().Concat(t.GetInterfaces().SelectMany(i => i.GetMethods()))
                .Any(m => m.Name == "WithRollup"))
            .Select(t => t.Name)];

        Assert.Equal(["ISelectBuilderGroupBy"], declaringInterfaces);
    }

    [Fact]
    public void Over_IsReachableOnlyFromThePercentileWithinGroupStage()
    {
        // The rule's absence proof: .Over() hangs off the node WithinGroup(...)
        // returns, so a percentile consumed without it can never acquire one.
        Type pending = Assert.Single(
            Core.GetExportedTypes().Where(t => t.Name == "PercentileContFunction"));
        MethodInfo withinGroup = Assert.Single(
            pending.GetMethods().Where(m => m.Name == "WithinGroup"));

        Assert.Equal("PercentileFunction", withinGroup.ReturnType.Name);
        Assert.NotEmpty(withinGroup.ReturnType.GetMethods().Where(m => m.Name == "Over"));
    }

    [Theory]
    [InlineData("Inserted")]
    [InlineData("Deleted")]
    public void OutputPseudoTable_ReachesAClauseOnlyAsAnExpressionArgument(string factory)
    {
        // The rule reads the name of the invocation hosting the argument, which is
        // sound only while these stay plain expressions with no clause step of their own.
        MethodInfo method = Assert.Single(
            typeof(Sql).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == factory));

        Assert.True(typeof(SqlExpression).IsAssignableFrom(method.ReturnType));
        Assert.Empty(method.ReturnType.GetMethods().Where(m => m.Name == "Output"));
    }

    [Theory]
    [InlineData("Limit")]
    [InlineData("Grouping")]
    [InlineData("PercentileCont")]
    [InlineData("PercentileDisc")]
    [InlineData("Inserted")]
    [InlineData("Deleted")]
    public void TriggerMember_ExistsInCoreApi(string methodName)
    {
        bool exists = Core.GetExportedTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .Any(m => m.Name == methodName);

        Assert.True(exists, $"'{methodName}' is a SQLA0102 trigger but no longer exists in the core API.");
    }
}
