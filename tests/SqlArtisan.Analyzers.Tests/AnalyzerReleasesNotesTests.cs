using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace SqlArtisan.Analyzers.Tests;

// AnalyzerReleases.Unshipped.md is the shipped rule table; a row's Notes drift
// from the descriptor (release audit pass 8: SQLA0300 still said UPDATE/DELETE
// after MERGE joined) is caught on id, category, severity, and statement names.
public class AnalyzerReleasesNotesTests
{
    private static readonly Regex s_row = new(
        @"^(SQLA\d{4}) \| (\S+) \| (\w+) \| (.+)$", RegexOptions.Compiled);

    private static readonly Regex s_statement = new(
        @"\b(INSERT|UPDATE|DELETE|MERGE|SELECT)\b", RegexOptions.Compiled);

    [Fact]
    public void EveryRow_MatchesItsDescriptor()
    {
        // Every field, not one per id: four fields carry SQLA0001, and a row's
        // Notes must answer the messages of all of them.
        ILookup<string, DiagnosticDescriptor> descriptors = typeof(DiagnosticDescriptors)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(DiagnosticDescriptor))
            .Select(f => (DiagnosticDescriptor)f.GetValue(null)!)
            .ToLookup(d => d.Id, StringComparer.Ordinal);

        string path = Path.Combine(
            FindRepoRoot(), "src", "SqlArtisan.Analyzers", "AnalyzerReleases.Unshipped.md");
        List<string> drift = [];
        int rows = 0;

        foreach (string line in File.ReadAllLines(path))
        {
            Match row = s_row.Match(line.Trim());
            if (!row.Success)
            {
                continue;
            }

            rows++;
            string id = row.Groups[1].Value;
            if (!descriptors.Contains(id))
            {
                drift.Add($"{id}: no descriptor");
                continue;
            }

            foreach (DiagnosticDescriptor descriptor in descriptors[id])
            {
                if (descriptor.Category != row.Groups[2].Value)
                {
                    drift.Add($"{id}: category {row.Groups[2].Value} vs {descriptor.Category}");
                }

                // The release-tracking format spells an off-by-default rule "Disabled".
                string severity = descriptor.IsEnabledByDefault
                    ? descriptor.DefaultSeverity.ToString()
                    : "Disabled";
                if (severity != row.Groups[3].Value)
                {
                    drift.Add(
                        $"{id}: severity {row.Groups[3].Value} vs {descriptor.DefaultSeverity}");
                }

                string message = descriptor.MessageFormat.ToString();
                foreach (Match statement in s_statement.Matches(message))
                {
                    if (!row.Groups[4].Value.Contains(statement.Value, StringComparison.Ordinal))
                    {
                        drift.Add($"{id}: notes omit {statement.Value}, which the message names");
                    }
                }
            }
        }

        Assert.Equal(descriptors.Count, rows);
        Assert.True(drift.Count == 0, string.Join("\n", drift));
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
