using System;
using System.Linq;
using System.Reflection;

namespace SqlArtisan.Analyzers.Tests;

// A factory's return type offers only what some engine with the function runs
// (public-api-design.md § Factory return types), read here from the matrix's Filter cell:
// STDEV took a Filter the cell gives none of its engines (#815).
public class FilterCapabilityTests
{
    [Fact]
    public void FactoryReturningAFilterableAggregate_NamesAFunctionAFilterEngineHas()
    {
        Assert.True(DialectMatrix.TryGetEntry("Filter", 1, out DbmsSupport filter, out _));
        MethodInfo[] factories = [.. typeof(Sql)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.ReturnType.GetMethod(
                "Filter", BindingFlags.Public | BindingFlags.Instance) is not null)];

        string[] offenders = [.. factories
            .Where(method => !RunsOnAFilterEngine(method, filter))
            .Select(method => method.Name)
            .Distinct()];

        Assert.True(factories.Length > 10, $"Only {factories.Length} factories were swept.");
        Assert.Empty(offenders);
    }

    private static bool RunsOnAFilterEngine(MethodInfo method, DbmsSupport filter)
    {
        Assert.True(
            DialectMatrix.TryGetEntry(
                method.Name, method.GetParameters().Length, out DbmsSupport function, out _),
            $"{method.Name} has no matrix entry.");
        return (function.MySql && filter.MySql)
            || (function.Oracle && filter.Oracle)
            || (function.PostgreSql && filter.PostgreSql)
            || (function.Sqlite && filter.Sqlite)
            || (function.SqlServer && filter.SqlServer);
    }
}
