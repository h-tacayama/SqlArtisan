using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>
/// SQLA0006 spent four releases sitting after the schema rules because nothing
/// checked (#379). Of the three lists that carry this order, only this one is
/// reachable by reflection, so it is the one that can be gated.
/// </summary>
public class DiagnosticOrderingTests
{
    [Fact]
    public void SupportedDiagnostics_AreDeclaredInIdOrder()
    {
        string[] declared = [.. new DialectUsageAnalyzer().SupportedDiagnostics.Select(d => d.Id)];

        Assert.Equal([.. declared.OrderBy(id => id, StringComparer.Ordinal)], declared);
    }

    // ID order is family order only while the numbering holds: the dialect rules
    // were given 0001-0006 and the schema rules 0007 up.
    [Fact]
    public void EveryDiagnostic_SitsInTheCategoryItsNumberImplies()
    {
        foreach (DiagnosticDescriptor descriptor in new DialectUsageAnalyzer().SupportedDiagnostics)
        {
            string expected = int.Parse(descriptor.Id.Substring("SQLA".Length)) <= 6
                ? "SqlArtisan.Dialect"
                : "SqlArtisan.Schema";

            Assert.Equal(expected, descriptor.Category);
        }
    }
}
