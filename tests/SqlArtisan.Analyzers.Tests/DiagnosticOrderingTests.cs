using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>
/// The identifier-length rule sat after the schema rules until #379 because
/// nothing checked. Of the three lists that carry this order, only this one
/// is reachable by reflection, so it is the one that can be gated.
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

    // A suppression written for a retired id would attach to whatever rule reused it (#654).
    // SQLA0002 predates the ledger; RS2000-RS2008 let a New Rules row reuse any id a shipped
    // Removed Rules row names, so those are read here.
    [Fact]
    public void RetiredId_IsNeverReused()
    {
        string shipped = Path.Combine(
            FindRepoRoot(), "src", "SqlArtisan.Analyzers", "AnalyzerReleases.Shipped.md");
        string section = string.Empty;
        List<string> retired = ["SQLA0002"];
        foreach (string line in File.ReadAllLines(shipped).Select(l => l.Trim()))
        {
            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                section = line;
            }
            else if (section == "### Removed Rules"
                && line.StartsWith("SQLA", StringComparison.Ordinal))
            {
                retired.Add(line.Split('|')[0].Trim());
            }
        }

        Assert.Empty(new DialectUsageAnalyzer().SupportedDiagnostics
            .Select(d => d.Id)
            .Intersect(retired, StringComparer.Ordinal));
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SqlArtisan.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir.FullName;
    }
}
