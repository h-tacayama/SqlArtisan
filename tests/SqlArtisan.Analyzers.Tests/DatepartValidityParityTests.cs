using System;
using System.Collections.Generic;
using System.Linq;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>
/// Ties <see cref="DatepartValidity"/>'s member-name strings to the real
/// <see cref="DateTimePart"/> enum, matched by name (ADR 0009);
/// <see cref="SchemaMetadataParityTests"/> is the sibling gate on the same pattern.
/// </summary>
public class DatepartValidityParityTests
{
    private static readonly IReadOnlyList<string> RealMemberNames =
        [.. Enum.GetNames(typeof(DateTimePart)).OrderBy(n => n, StringComparer.Ordinal)];

    [Fact]
    public void EveryNameInEveryList_IsARealDateTimePartMember()
    {
        string[] unmatchable =
        [
            .. DatepartValidity.AllKnownDatepartNames
                .Except(RealMemberNames, StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal),
        ];

        Assert.True(
            unmatchable.Length == 0,
            $"{unmatchable.Length} name(s) in DatepartValidity's lists name no real "
                + $"DateTimePart member, so SQLA0104 can never match them:\n  "
                + string.Join("\n  ", unmatchable));
    }

    // One direction only (DateTimePart.cs says not every field fits every function):
    // the hazard is a member in no list at all, which the rule then flags wherever
    // it has a list — including on dialects that accept the field.
    [Fact]
    public void EveryRealDateTimePartMember_AppearsInAtLeastOneList()
    {
        string[] uncovered =
        [
            .. RealMemberNames
                .Except(DatepartValidity.AllKnownDatepartNames, StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal),
        ];

        Assert.True(
            uncovered.Length == 0,
            $"{uncovered.Length} DateTimePart member(s) appear in none of DatepartValidity's "
                + "lists, so SQLA0104 reports them for every (function, dialect) pair it does "
                + "cover — a false positive on each dialect whose grammar accepts them. Add "
                + $"each member to the lists that accept it:\n  "
                + string.Join("\n  ", uncovered));
    }

    [Fact]
    public void EveryDatepartConsumer_HasAParameterNameEntry()
    {
        string[] consumers =
        [
            "Extract", "Datepart", "Dateadd", "Datediff", "DateTrunc", "Datetrunc", "Interval",
            "Timestampadd", "Timestampdiff",
        ];

        string[] missing =
        [
            .. consumers.Where(name => !DatepartValidity.DatepartParameterName.ContainsKey(name)),
        ];

        Assert.True(
            missing.Length == 0,
            $"{missing.Length} DateTimePart consumer(s) have no entry in "
                + $"DatepartValidity.DatepartParameterName, so SQLA0104 can never read their "
                + $"argument:\n  {string.Join("\n  ", missing)}");
    }

    // NUMTOYMINTERVAL/NUMTODSINTERVAL take their unit through an eager
    // value-domain guard instead of SQLA0104 (ADR 0012, #448) — no other engine
    // has either function, so the guard can reject outright.
    private static readonly string[] EagerGuardRoutedConsumers =
        ["Numtodsinterval", "Numtoyminterval"];

    [Fact]
    public void EveryDateTimePartFactory_IsListedOrEagerGuardRouted()
    {
        string[] realConsumers =
        [
            .. typeof(Sql)
                .GetMethods(
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(m => m.GetParameters().Any(p => p.ParameterType == typeof(DateTimePart)))
                .Select(m => m.Name)
                .Distinct()
                .OrderBy(n => n, StringComparer.Ordinal),
        ];

        string[] unrouted =
        [
            .. realConsumers
                .Where(name => !DatepartValidity.DatepartParameterName.ContainsKey(name))
                .Except(EagerGuardRoutedConsumers, StringComparer.Ordinal),
        ];
        string[] staleRouted =
        [
            .. EagerGuardRoutedConsumers.Except(realConsumers, StringComparer.Ordinal),
        ];

        Assert.True(
            unrouted.Length == 0 && staleRouted.Length == 0,
            $"DateTimePart-taking factories outside both SQLA0104's consumer map and the "
                + $"eager-guard route: [{string.Join(", ", unrouted)}]; "
                + $"stale eager-guard entries: [{string.Join(", ", staleRouted)}]");
    }
}
