using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SqlArtisan.Internal;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>
/// Ties <see cref="ArgumentValueValidity"/>'s strings to the real public API,
/// matched by name (ADR 0009); <see cref="DatepartValidityParityTests"/> is the
/// sibling gate on SQLA0104's table.
/// </summary>
public class ArgumentValueValidityParityTests
{
    // None emits the empty match parameter, which every engine takes, and the
    // rule skips zero-valued members outright — so it is not an alphabet entry.
    private static readonly IReadOnlyList<string> RealMatchOptionNames =
    [
        .. Enum.GetNames(typeof(RegexpOptions))
            .Where(name => (int)Enum.Parse(typeof(RegexpOptions), name) != 0)
            .OrderBy(name => name, StringComparer.Ordinal),
    ];

    // The pagination surface SQLA0104's row-count table is measured against.
    private static readonly Type[] PaginationInterfaces =
        [typeof(IPagination), typeof(ILimitOffsetBuilder), typeof(IOffsetFetchBuilder)];

    // Not routed by decision (#532): engines do diverge on a negative offset,
    // but one goes negative by arithmetic, so the call-site constant this rule
    // reads is not where the value turns bad.
    private static readonly string[] UnroutedRowCountConstructs = ["Offset", "OffsetRows"];

    [Fact]
    public void EveryNameInEveryAlphabet_IsARealRegexpOptionsMember()
    {
        string[] unmatchable =
        [
            .. ArgumentValueValidity.AllKnownMatchOptionNames
                .Except(RealMatchOptionNames, StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal),
        ];

        Assert.True(
            unmatchable.Length == 0,
            $"{unmatchable.Length} name(s) in ArgumentValueValidity's alphabets name no real "
                + $"RegexpOptions member, so SQLA0104 can never match them:\n  "
                + string.Join("\n  ", unmatchable));
    }

    [Fact]
    public void EveryRealRegexpOptionsMember_AppearsInAtLeastOneAlphabet()
    {
        string[] uncovered =
        [
            .. RealMatchOptionNames
                .Except(ArgumentValueValidity.AllKnownMatchOptionNames, StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal),
        ];

        Assert.True(
            uncovered.Length == 0,
            $"{uncovered.Length} RegexpOptions member(s) appear in none of "
                + "ArgumentValueValidity's alphabets, so SQLA0104 reports them on every dialect "
                + $"it has an alphabet for — a false positive on each:\n  "
                + string.Join("\n  ", uncovered));
    }

    [Fact]
    public void EveryRegexpOptionsFactory_HasAParameterNameEntry()
    {
        string[] unrouted =
        [
            .. typeof(Sql)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.GetParameters().Any(p => p.ParameterType == typeof(RegexpOptions)))
                .Select(m => m.Name)
                .Distinct(StringComparer.Ordinal)
                .Where(name =>
                    !ArgumentValueValidity.MatchOptionParameterName.ContainsKey(name))
                .OrderBy(name => name, StringComparer.Ordinal),
        ];

        Assert.True(
            unrouted.Length == 0,
            $"{unrouted.Length} RegexpOptions-taking factory(ies) have no entry in "
                + $"ArgumentValueValidity.MatchOptionParameterName, so SQLA0104 can never read "
                + $"their argument:\n  {string.Join("\n  ", unrouted)}");
    }

    [Fact]
    public void EveryRoutedParameterName_NamesARealParameter()
    {
        List<string> wrong = [];

        foreach (KeyValuePair<string, string> entry in
            ArgumentValueValidity.MatchOptionParameterName)
        {
            bool found = typeof(Sql)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == entry.Key)
                .SelectMany(m => m.GetParameters())
                .Any(p => p.ParameterType == typeof(RegexpOptions) && p.Name == entry.Value);

            if (!found)
            {
                wrong.Add($"{entry.Key} -> '{entry.Value}'");
            }
        }

        foreach (KeyValuePair<string, string> entry in ArgumentValueValidity.RowCountParameterName)
        {
            bool found = RowCountMethods()
                .Where(m => m.Name == entry.Key)
                .SelectMany(m => m.GetParameters())
                .Any(p => p.ParameterType == typeof(int) && p.Name == entry.Value);

            if (!found)
            {
                wrong.Add($"{entry.Key} -> '{entry.Value}'");
            }
        }

        bool groupByFound = typeof(IPagination).Assembly
            .GetType("SqlArtisan.Internal.ISelectBuilderWhere")!
            .GetMethod("GroupBy")!
            .GetParameters()
            .Any(p => p.Name == ArgumentValueValidity.GroupByItemsParameterName);

        if (!groupByFound)
        {
            wrong.Add($"GroupBy -> '{ArgumentValueValidity.GroupByItemsParameterName}'");
        }

        Assert.True(
            wrong.Count == 0,
            "entries naming a parameter the factory does not have, so SQLA0104 reads nothing: "
                + string.Join(", ", wrong));
    }

    [Fact]
    public void EveryRowCountConstruct_IsRoutedOrRecordedUnrouted()
    {
        string[] unaccounted =
        [
            .. RowCountMethods()
                .Select(m => m.Name)
                .Distinct(StringComparer.Ordinal)
                .Where(name => !ArgumentValueValidity.RowCountParameterName.ContainsKey(name))
                .Except(UnroutedRowCountConstructs, StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal),
        ];
        string[] stale =
        [
            .. UnroutedRowCountConstructs
                .Except(RowCountMethods().Select(m => m.Name), StringComparer.Ordinal),
        ];

        Assert.True(
            unaccounted.Length == 0 && stale.Length == 0,
            $"row-count constructs outside both SQLA0104's table and the unrouted list: "
                + $"[{string.Join(", ", unaccounted)}]; "
                + $"stale unrouted entries: [{string.Join(", ", stale)}]");
    }

    // A cell on a dialect SQLA0100/SQLA0101 owns can never fire, so it is a
    // fact recorded against the wrong construct rather than a harmless extra.
    [Fact]
    public void EveryRejectedCell_IsOnADialectTheMatrixSupportsTheConstructOn()
    {
        List<string> unreachable = [];

        foreach ((string member, TargetDbms dbms) in
            ArgumentValueValidity.AllRejectedRowCountCells)
        {
            if (!ArgumentValueValidity.RowCountParameterName.ContainsKey(member))
            {
                unreachable.Add($"{member} is in no parameter-name entry");
                continue;
            }

            if (DialectSupportResolver.MatchMatrixEntry(member, arity: 1) is { } match
                && !DialectSupportResolver.Evaluate(match, dbms, targetVersion: null).IsSupported)
            {
                unreachable.Add($"{member} on {TargetDbmsNames.Display(dbms)}");
            }
        }

        Assert.True(
            unreachable.Count == 0,
            "row-count cells SQLA0104 can never report, because the construct itself is "
                + "unsupported there: " + string.Join(", ", unreachable));
    }

    private static IEnumerable<MethodInfo> RowCountMethods() =>
        PaginationInterfaces
            .SelectMany(type => type.GetMethods())
            .Where(m => m.GetParameters() is [{ ParameterType: Type p }] && p == typeof(int))
            .Concat(typeof(Sql)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == "Top"));
}
