using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>
/// Pins the core-API facts the SQLA0003 context rules key on (the ADR 0009
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

    [Theory]
    [InlineData("Limit")]
    [InlineData("Grouping")]
    public void TriggerMember_ExistsInCoreApi(string methodName)
    {
        bool exists = Core.GetExportedTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .Any(m => m.Name == methodName);

        Assert.True(exists, $"'{methodName}' is a SQLA0003 trigger but no longer exists in the core API.");
    }
}
