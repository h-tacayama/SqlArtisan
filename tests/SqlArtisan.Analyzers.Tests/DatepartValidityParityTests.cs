using System;
using System.Collections.Generic;
using System.Linq;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>
/// Ties <see cref="DatepartValidity"/>'s member-name strings to the real
/// <see cref="DateTimePart"/> enum, which the rule matches by name rather
/// than by a type reference (ADR 0009) — see <see cref="SchemaMetadataParityTests"/>
/// for the sibling gate on the same pattern.
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
            .. DatepartValidity.AllKnownMemberNames
                .Except(RealMemberNames, StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal),
        ];

        Assert.True(
            unmatchable.Length == 0,
            $"{unmatchable.Length} name(s) in DatepartValidity's lists name no real "
                + $"DateTimePart member, so SQLA0104 can never match them:\n  "
                + string.Join("\n  ", unmatchable));
    }

    // One direction only, and deliberately not "every member is covered by
    // every consumer" — the core's own DateTimePart.cs doc says explicitly
    // that not every field is valid for every function or dialect. This
    // checks a coarser fact: a member absent from every list combined is
    // a member SQLA0104 can never flag anywhere, which is worth knowing
    // about even though it is not on its own a defect (a member can
    // legitimately belong to no function this rule covers yet).
    [Fact]
    public void EveryRealDateTimePartMember_AppearsInAtLeastOneList()
    {
        string[] uncovered =
        [
            .. RealMemberNames
                .Except(DatepartValidity.AllKnownMemberNames, StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal),
        ];

        Assert.True(
            uncovered.Length == 0,
            $"{uncovered.Length} DateTimePart member(s) appear in none of DatepartValidity's "
                + "lists, so SQLA0104 never checks them for any function. If this is "
                + "intentional (the member has no function this rule covers), reword this "
                + $"test's assertion rather than silently leaving it failing:\n  "
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
}
