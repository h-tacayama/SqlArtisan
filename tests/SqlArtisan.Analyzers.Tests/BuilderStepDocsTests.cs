using System.Xml.Linq;
using SqlArtisan.Internal;

namespace SqlArtisan.Analyzers.Tests;

// A builder-step interface's summary lists the continuations the state offers;
// a continuation the interface inherits but the summary omits is a doc narrower
// than the code (release audit pass 8: the set operators after WHERE/GROUP BY/HAVING).
public class BuilderStepDocsTests
{
    [Fact]
    public void EveryStepDerivingISetOperator_NamesTheSetOperatorContinuation()
    {
        XDocument doc = XDocument.Load(Path.Combine(
            Path.GetDirectoryName(typeof(Sql).Assembly.Location)!, "SqlArtisan.xml"));
        List<string> missing = [];

        foreach (Type type in typeof(Sql).Assembly.GetExportedTypes())
        {
            if (!type.IsInterface
                || type == typeof(ISetOperator) || !typeof(ISetOperator).IsAssignableFrom(type))
            {
                continue;
            }

            string? summary = doc.Descendants("member")
                .FirstOrDefault(m => (string?)m.Attribute("name") == $"T:{type.FullName}")
                ?.Element("summary")?.Value;

            if (summary is null
                || !summary.Contains("set operator", StringComparison.OrdinalIgnoreCase))
            {
                missing.Add(type.Name);
            }
        }

        Assert.True(
            missing.Count == 0,
            "summaries omitting the set-operator continuation: " + string.Join(", ", missing));
    }
}
