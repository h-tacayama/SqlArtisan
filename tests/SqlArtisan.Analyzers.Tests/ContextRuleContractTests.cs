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
    [InlineData("Interval")]
    [InlineData("IntervalLiteral")]
    [InlineData("DateAdd")]
    [InlineData("DateSub")]
    [InlineData("From")]
    [InlineData("Using")]
    [InlineData("InnerJoin")]
    [InlineData("LeftJoin")]
    [InlineData("RightJoin")]
    [InlineData("ForUpdate")]
    [InlineData("GroupBy")]
    [InlineData("FetchFirst")]
    [InlineData("FetchNext")]
    [InlineData("OffsetRows")]
    public void TriggerMember_ExistsInCoreApi(string methodName)
    {
        bool exists = Core.GetExportedTypes()
            .SelectMany(t =>
                t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .Any(m => m.Name == methodName);

        Assert.True(
            exists,
            $"'{methodName}' is a SQLA0102 trigger but no longer exists in the core API.");
    }

    // The DML-shape rules read the declaring interface and nothing else, so the
    // set of interfaces declaring a trigger name is the whole soundness argument:
    // a new one would silently join, or escape, a shape's verdict.
    [Theory]
    [InlineData("From", "IDeleteBuilderDelete", "ISelectBuilderSelect", "IUpdateBuilderSet")]
    [InlineData(
        "Using",
        "IDeleteBuilderDelete",
        "IDeleteBuilderFromJoinOn",
        "IMergeBuilderTarget",
        "ISelectBuilderJoin",
        "IUpdateBuilderFromJoinOn",
        "IUpdateBuilderJoinOn")]
    [InlineData(
        "InnerJoin",
        "IDeleteBuilderFrom",
        "IJoinOperator",
        "IUpdateBuilderFrom",
        "IUpdateBuilderJoined",
        "IUpdateBuilderUpdate")]
    [InlineData(
        "LeftJoin",
        "IDeleteBuilderFrom",
        "IJoinOperator",
        "IUpdateBuilderFrom",
        "IUpdateBuilderJoined",
        "IUpdateBuilderUpdate")]
    [InlineData(
        "RightJoin",
        "IDeleteBuilderFrom",
        "IJoinOperator",
        "IUpdateBuilderFrom",
        "IUpdateBuilderJoined",
        "IUpdateBuilderUpdate")]
    public void DmlShapeTrigger_IsDeclaredOnExactlyTheseInterfaces(
        string methodName, params string[] expected)
    {
        List<string> declaring = [.. Core.GetExportedTypes()
            .Where(t => t.IsInterface)
            .Where(t => t.GetMethods().Any(m => m.DeclaringType == t && m.Name == methodName))
            .Select(t => t.Name)
            .OrderBy(name => name, StringComparer.Ordinal)];

        Assert.Equal(expected, declaring);
    }

    // DELETE ... USING is identified by its declaring interface alone, which is
    // sound only while that interface carries no second Using overload.
    [Fact]
    public void DeleteUsing_IsTheOnlyUsingOverloadOnItsStage()
    {
        Type stage = Assert.Single(
            Core.GetExportedTypes().Where(t => t.Name == "IDeleteBuilderDelete"));

        MethodInfo overload = Assert.Single(stage.GetMethods().Where(m => m.Name == "Using"));
        ParameterInfo parameter = Assert.Single(overload.GetParameters());

        Assert.Equal(typeof(TableReference[]), parameter.ParameterType);
    }

    [Theory]
    [InlineData("IUpdateBuilderUpdate")]
    [InlineData("IUpdateBuilderJoined")]
    public void UpdateDirectJoinStage_DeclaresExactlyTheClassifiedJoinSteps(string stage)
    {
        Type type = Assert.Single(Core.GetExportedTypes().Where(t => t.Name == stage));

        List<string> joins = [.. type.GetMethods()
            .Where(m => m.DeclaringType == type)
            .Where(m => m.Name.EndsWith("Join", StringComparison.Ordinal))
            .Select(m => m.Name)
            .OrderBy(name => name, StringComparer.Ordinal)];

        Assert.Equal(["InnerJoin", "LeftJoin", "RightJoin"], joins);
    }

    // The FOR UPDATE rule reads the receiver chain for a GroupBy, which finds one
    // only because the grouped stages reach ForUpdate by chaining further steps
    // rather than declaring it themselves.
    [Theory]
    [InlineData("ISelectBuilderGroupBy")]
    [InlineData("ISelectBuilderHaving")]
    public void GroupedStage_ReachesForUpdateOnlyByChaining(string stage)
    {
        Type type = Assert.Single(Core.GetExportedTypes().Where(t => t.Name == stage));

        Assert.Empty(type.GetMethods().Where(m => m.Name == "ForUpdate"));
        Assert.Equal(
            "ISelectBuilderOrderBy",
            Assert.Single(type.GetMethods().Where(m => m.Name == "OrderBy")).ReturnType.Name);
    }
}
