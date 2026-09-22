using System.Xml.Linq;
using SqlArtisan.Internal;

namespace SqlArtisan.Analyzers.Tests;

// A builder-step interface's summary lists the continuations the state offers;
// a continuation the interface inherits but the summary omits is a doc narrower
// than the code (release audit pass 8; #520 widened it past the set operators).
public class BuilderStepDocsTests
{
    public static TheoryData<string, string> Continuations() => new()
    {
        { nameof(IForUpdate), "lock" },
        { nameof(IPagination), "paginat" },
        { nameof(ISetOperator), "set operator" },
    };

    [Theory]
    [MemberData(nameof(Continuations))]
    public void EveryStepDerivingTheContinuation_NamesItInItsSummary(
        string continuation, string phrase)
    {
        XDocument doc = XDocument.Load(Path.Combine(
            Path.GetDirectoryName(typeof(Sql).Assembly.Location)!, "SqlArtisan.xml"));
        Type marker = Assert.Single(
            typeof(Sql).Assembly.GetExportedTypes().Where(t => t.Name == continuation));
        List<string> missing = [];

        foreach (Type type in typeof(Sql).Assembly.GetExportedTypes())
        {
            if (!type.IsInterface || type == marker || !marker.IsAssignableFrom(type))
            {
                continue;
            }

            string? summary = doc.Descendants("member")
                .FirstOrDefault(m => (string?)m.Attribute("name") == $"T:{type.FullName}")
                ?.Element("summary")?.Value;

            if (summary is null || !summary.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            {
                missing.Add(type.Name);
            }
        }

        Assert.True(
            missing.Count == 0,
            $"summaries omitting the {continuation} continuation: " + string.Join(", ", missing));
    }
}
