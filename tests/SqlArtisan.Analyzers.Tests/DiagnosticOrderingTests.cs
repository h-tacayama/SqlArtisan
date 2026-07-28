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

    // ID order doubles as family order only while the dialect rules stop at 0006,
    // and that range is full. A seventh takes the next free ID rather than a
    // renumbering that would break released suppressions, and this is where that
    // shows up — expect to delete this test rather than satisfy it.
    [Fact]
    public void EveryDiagnostic_SitsInTheCategoryItsNumberImplies()
    {
        foreach (DiagnosticDescriptor descriptor in new DialectUsageAnalyzer().SupportedDiagnostics)
        {
            string expected = int.Parse(descriptor.Id.Substring("SQLA".Length)) <= 6
                ? "SqlArtisan.Dialect"
                : "SqlArtisan.Schema";

            Assert.True(
                expected == descriptor.Category,
                $"{descriptor.Id} is {descriptor.Category}, which its number reads as {expected}. "
                    + "A dialect rule past 0006 ends the family-order half of the numbering; "
                    + "drop this test rather than renumber a released ID.");
        }
    }
}
