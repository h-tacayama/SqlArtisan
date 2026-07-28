using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>
/// Ties <see cref="SchemaMetadata"/>'s strings to the real
/// <see cref="DbColumnMetadataAttribute"/>, which the analyzer matches by name
/// rather than by a type reference (ADR 0009).
/// </summary>
/// <remarks>
/// Other suites go red on a symptom — a diagnostic that stopped firing, or a
/// member with no matrix exclusion — and the obvious fix for each leaves the
/// coupling broken. Past that fix, an unread fact reaches this gate alone.
/// </remarks>
public class SchemaMetadataParityTests
{
    private static readonly Type AttributeType = typeof(DbColumnMetadataAttribute);

    // Collected by suffix, so a new *Argument constant joins the gate on its own.
    private static readonly IReadOnlyDictionary<string, string> ArgumentConstants =
        typeof(SchemaMetadata)
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
                | BindingFlags.DeclaredOnly)
            .Where(f => f.IsLiteral
                && f.FieldType == typeof(string)
                && f.Name.EndsWith("Argument", StringComparison.Ordinal))
            .ToDictionary(f => f.Name, f => (string)f.GetRawConstantValue()!, StringComparer.Ordinal);

    private static readonly IReadOnlyList<string> SettableProperties =
        [.. AttributeType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(p => p.CanWrite)
            .Select(p => p.Name)];

    [Fact]
    public void AttributeName_MatchesTheRealAttributesFullName() =>
        Assert.Equal(SchemaMetadata.AttributeName, AttributeType.FullName);

    [Fact]
    public void EveryArgumentConstant_NamesASettablePropertyOnTheAttribute()
    {
        List<string> orphaned =
        [
            .. ArgumentConstants
                .Where(c => !SettableProperties.Contains(c.Value, StringComparer.Ordinal))
                .Select(c => $"SchemaMetadata.{c.Key} = \"{c.Value}\""),
        ];

        Assert.True(
            orphaned.Count == 0,
            $"{orphaned.Count} constant(s) in SchemaMetadata.cs name no property on "
                + $"{AttributeType.Name}, so the rules reading them can never match:\n  "
                + string.Join("\n  ", orphaned));
    }

    [Fact]
    public void EverySettableProperty_HasAnArgumentConstant()
    {
        List<string> unread =
        [
            .. SettableProperties
                .Where(p => !ArgumentConstants.Values.Contains(p, StringComparer.Ordinal)),
        ];

        Assert.True(
            unread.Count == 0,
            $"{unread.Count} propert(y|ies) on {AttributeType.Name} have no constant in "
                + "SchemaMetadata.cs, and the rules read facts only through those "
                + $"constants:\n  {string.Join("\n  ", unread)}");
    }
}
