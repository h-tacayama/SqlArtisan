using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SqlArtisan.TableClassGen;

namespace SqlArtisan.TableClassGen.Tests;

/// <summary>
/// The generator writes a ColumnType category and SQLA0012 reads it back, coupled
/// only by the spelling of the string (ADR 0009). Each side holds its own copy, so
/// a category renamed on one of them would leave the rule matching nothing.
/// </summary>
/// <remarks>
/// That silence is indistinguishable from the designed kind, an unrecognized type
/// name, which is why the gate is here rather than left to the rule's own suite.
/// </remarks>
public class ColumnCategoryParityTests
{
    private static readonly IReadOnlyList<string> Written = Categories(typeof(ColumnCategory));

    private static readonly IReadOnlyList<string> Read =
        Categories(typeof(SqlArtisan.Analyzers.ColumnCategories));

    private static IReadOnlyList<string> Categories(Type holder) =>
        [.. holder
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
                | BindingFlags.DeclaredOnly)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .OrderBy(value => value, StringComparer.Ordinal)];

    [Fact]
    public void TheGeneratorsCategories_AreExactlyTheOnesTheRuleReads() =>
        Assert.Equal(Written, Read);

    // Every category has to be reachable from a real catalog type name, or it is a
    // spelling the rule can never see.
    [Fact]
    public void EveryCategory_IsProducedBySomeCatalogTypeName()
    {
        string[] samples =
        [
            "varchar", "number", "timestamp", "bytea", "boolean"
        ];

        List<string> produced =
        [
            .. samples
                .Select(name => ColumnCategory.Of(Dbms.PostgreSql, name)
                    ?? ColumnCategory.Of(Dbms.Oracle, name))
                .Where(category => category is not null)
                .Select(category => category!)
                .Distinct()
                .OrderBy(category => category, StringComparer.Ordinal),
        ];

        Assert.Equal(Written, produced);
    }
}
