using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>
/// The identifier-length rule spent four releases sitting after the schema
/// rules because nothing checked (#379). Of the three lists that carry this
/// order, only this one is reachable by reflection, so it is the one that can
/// be gated.
/// </summary>
public class DiagnosticOrderingTests
{
    [Fact]
    public void SupportedDiagnostics_AreDeclaredInIdOrder()
    {
        string[] declared = [.. new DialectUsageAnalyzer().SupportedDiagnostics.Select(d => d.Id)];

        Assert.Equal([.. declared.OrderBy(id => id, StringComparer.Ordinal)], declared);
    }

    // The band an id falls in *is* its category (#433), so unlike the id <= 6
    // heuristic this replaced, the rule does not expire once a family fills up.
    [Fact]
    public void EveryDiagnostic_SitsInTheCategoryItsBandImplies()
    {
        foreach (DiagnosticDescriptor descriptor in new DialectUsageAnalyzer().SupportedDiagnostics)
        {
            string expected = int.Parse(descriptor.Id.Substring("SQLA".Length)) switch
            {
                < 100 => "SqlArtisan.Configuration",
                < 200 => "SqlArtisan.Dialect",
                < 300 => "SqlArtisan.Schema",
                < 400 => "SqlArtisan.Validity",
                _ => "no band assigned yet",
            };

            Assert.True(
                expected == descriptor.Category,
                $"{descriptor.Id} is {descriptor.Category}, but its band reads as {expected}. "
                    + "Give a new rule the next id inside its category's band, "
                    + "not the next free number overall.");
        }
    }
}
