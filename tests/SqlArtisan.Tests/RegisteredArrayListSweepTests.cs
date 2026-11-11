using System.Reflection;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

// A registered array type passed as a whole params list could be the list or one value, so
// every list position rejects it, a key position with its own message (#810). In, NotIn and
// the lists of rows declare the list reading in their types.
[Collection("NonParallelTests")]
public class RegisteredArrayListSweepTests : IDisposable
{
    private const string Ambiguous = "could be the list or one value";

    private static readonly string[] KeyPositionMessages =
    [
        "Invalid type for GroupByItem: System.String",
        "Invalid type for OrderByItem: System.String",
        "Invalid type for PartitionByItem: System.String",
        "Invalid type for DistinctOnItem: System.String",
    ];

    // The builder stages' own list members, each held by a focused test in
    // ValueConverterTests or by its key-position resolver, as the static factories are below.
    private static readonly string[] InstanceListMembers =
    [
        "IDeleteBuilderDeleteOutput.Output",
        "IInsertBuilderColumns.Values",
        "IInsertBuilderColumnsOutput.Output",
        "IInsertBuilderConflictAction.Returning",
        "IInsertBuilderOutputColumns.Values",
        "IInsertBuilderTable.Values",
        "IInsertBuilderValues.Values",
        "IInsertIgnoreBuilderColumns.Values",
        "IInsertIgnoreBuilderTable.Values",
        "IInsertIgnoreBuilderValues.Values",
        "IMergeBuilderThenInsert.Values",
        "IMergeBuilderWhenNotMatchedAndThenInsert.Values",
        "IReturning.Returning",
        "ISelectBuilder.Select",
        "ISelectBuilderFrom.GroupBy",
        "ISelectBuilderFrom.OrderBy",
        "ISelectBuilderGroupBy.OrderBy",
        "ISelectBuilderHaving.OrderBy",
        "ISelectBuilderWhere.GroupBy",
        "ISelectBuilderWhere.OrderBy",
        "ISelectBuilderWithRollup.OrderBy",
        "IUpdateBuilderSetOutput.Output",
        "PartitionByClause.OrderBy",
        "SqlExpression.In",
        "SqlExpression.NotIn",
    ];

    public RegisteredArrayListSweepTests()
    {
        SqlArtisanConfig.AddValueConverter<string[]>(tags => string.Join(",", tags));
    }

    public void Dispose() => SqlArtisanConfig.ResetValueConverters();

    [Fact]
    public void Factories_RegisteredArrayTypeAsTheList_Throw()
    {
        List<string> violations = [];
        int checkedLists = 0;

        foreach (MethodInfo method in typeof(Sql)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.GetParameters() is [.., ParameterInfo last] && IsObjectList(last)))
        {
            checkedLists++;
            ParameterInfo[] parameters = method.GetParameters();
            object?[] args = new object?[parameters.Length];
            for (int i = 0; i < parameters.Length - 1; i++)
            {
                args[i] = FactoryGuardSweepTests.Filler(method, parameters[i]);
            }

            args[^1] = new[] { "a", "b" };
            try
            {
                method.Invoke(null, args);
                violations.Add($"{FactoryGuardSweepTests.Signature(method)} — accepted");
            }
            catch (TargetInvocationException ex)
                when (ex.InnerException is ArgumentException inner
                    && (inner.Message.Contains(Ambiguous)
                        || KeyPositionMessages.Contains(inner.Message)))
            {
            }
            catch (TargetInvocationException ex)
            {
                violations.Add(
                    $"{FactoryGuardSweepTests.Signature(method)} — {ex.InnerException?.Message}");
            }
        }

        Assert.True(checkedLists > 20, $"Only {checkedLists} lists were checked.");
        Assert.True(
            violations.Count == 0,
            $"{violations.Count} list(s) take a registered array as the list:\n"
                + string.Join("\n", violations));
    }

    [Fact]
    public void InstanceListMembers_MatchTheCoveredSet()
    {
        string[] found =
        [
            .. typeof(Sql).Assembly.GetExportedTypes()
                .SelectMany(t => t.GetMethods(
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                .Where(m => m.GetParameters() is [.., ParameterInfo last] && IsObjectList(last))
                .Select(m => $"{m.DeclaringType!.Name}.{m.Name}")
                .Distinct()
                .Order(StringComparer.Ordinal),
        ];

        Assert.Equal(InstanceListMembers, found);
    }

    private static bool IsObjectList(ParameterInfo parameter) =>
        parameter.ParameterType == typeof(object[])
        && parameter.IsDefined(typeof(ParamArrayAttribute));
}
