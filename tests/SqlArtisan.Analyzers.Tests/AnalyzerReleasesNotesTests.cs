using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace SqlArtisan.Analyzers.Tests;

// The release-tracking ledger is the shipped rule table: each rule's id, category and
// severity are checked, and an unreleased row's Notes against the statements its message
// names (release audit pass 8: SQLA0300 still said UPDATE/DELETE after MERGE joined).
public class AnalyzerReleasesNotesTests
{
    private static readonly Regex s_id = new(@"^SQLA\d{4}$", RegexOptions.Compiled);

    private static readonly Regex s_statement = new(
        @"\b(INSERT|UPDATE|DELETE|MERGE|SELECT)\b", RegexOptions.Compiled);

    [Fact]
    public void EveryRow_MatchesItsDescriptor()
    {
        // Every field, not one per id: several fields carry SQLA0001, and a row's
        // Notes must answer the messages of all of them.
        ILookup<string, DiagnosticDescriptor> descriptors = typeof(DiagnosticDescriptors)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(DiagnosticDescriptor))
            .Select(f => (DiagnosticDescriptor)f.GetValue(null)!)
            .ToLookup(d => d.Id, StringComparer.Ordinal);

        string folder = Path.Combine(FindRepoRoot(), "src", "SqlArtisan.Analyzers");
        Dictionary<string, LedgerRow> ledger = ReadLedger(
            File.ReadAllLines(Path.Combine(folder, "AnalyzerReleases.Shipped.md")),
            File.ReadAllLines(Path.Combine(folder, "AnalyzerReleases.Unshipped.md")));
        List<string> drift = [];

        foreach ((string id, LedgerRow row) in ledger)
        {
            if (!descriptors.Contains(id))
            {
                drift.Add($"{id}: no descriptor");
                continue;
            }

            foreach (DiagnosticDescriptor descriptor in descriptors[id])
            {
                if (descriptor.Category != row.Category)
                {
                    drift.Add($"{id}: category {row.Category} vs {descriptor.Category}");
                }

                // The release-tracking format spells an off-by-default rule "Disabled".
                string severity = descriptor.IsEnabledByDefault
                    ? descriptor.DefaultSeverity.ToString()
                    : "Disabled";
                if (severity != row.Severity)
                {
                    drift.Add($"{id}: severity {row.Severity} vs {descriptor.DefaultSeverity}");
                }

                // A released row is history (Shipped.md is append-only) and message text is
                // not covered, so only a row this release adds is held to today's message.
                if (row.Released)
                {
                    continue;
                }

                string message = descriptor.MessageFormat.ToString();
                foreach (Match statement in s_statement.Matches(message))
                {
                    if (!row.Notes.Contains(statement.Value, StringComparison.Ordinal))
                    {
                        drift.Add($"{id}: notes omit {statement.Value}, which the message names");
                    }
                }
            }
        }

        drift.AddRange(descriptors
            .Select(group => group.Key)
            .Where(id => !ledger.ContainsKey(id))
            .Select(id => $"{id}: no ledger row"));
        Assert.True(drift.Count == 0, string.Join("\n", drift));
    }

    // The release step moves Unshipped's rows under a Shipped heading; that move, and the
    // Changed/Removed rows later releases add, must leave the ledger reading the same rules.
    [Fact]
    public void ReadLedger_FollowsReleasesInOrder()
    {
        string[] shipped =
        [
            "## Release 1.0.0",
            "### New Rules",
            "Rule ID | Category | Severity | Notes",
            "--------|----------|----------|-------",
            "SQLA0001 | SqlArtisan.Configuration | Warning | a",
            "SQLA0100 | SqlArtisan.Dialect | Warning | b",
            "SQLA0203 | SqlArtisan.Schema | Disabled | c",
        ];
        string[] unshipped =
        [
            "### Changed Rules",
            "Rule ID | New Category | New Severity | Old Category | Old Severity | Notes",
            "--------|--------------|--------------|--------------|--------------|-------",
            "SQLA0203 | SqlArtisan.Schema | Info | SqlArtisan.Schema | Disabled | c2",
            "### Removed Rules",
            "Rule ID | Category | Severity | Notes",
            "--------|----------|----------|-------",
            "SQLA0100 | SqlArtisan.Dialect | Warning | b",
        ];

        Dictionary<string, LedgerRow> ledger = ReadLedger(shipped, unshipped);

        KeyValuePair<string, LedgerRow>[] expected =
        [
            new("SQLA0001", new LedgerRow("SqlArtisan.Configuration", "Warning", "a", true)),
            new("SQLA0203", new LedgerRow("SqlArtisan.Schema", "Info", "c2", false)),
        ];
        Assert.Equal(expected, ledger.OrderBy(entry => entry.Key, StringComparer.Ordinal));
    }

    private static Dictionary<string, LedgerRow> ReadLedger(
        IEnumerable<string> shipped,
        IEnumerable<string> unshipped)
    {
        Dictionary<string, LedgerRow> ledger = new(StringComparer.Ordinal);
        Apply(ledger, shipped, released: true);
        Apply(ledger, unshipped, released: false);
        return ledger;
    }

    // Each section's table has its own columns: New and Removed Rules are
    // "ID | Category | Severity | Notes", Changed Rules leads with the new pair.
    private static void Apply(
        Dictionary<string, LedgerRow> ledger,
        IEnumerable<string> lines,
        bool released)
    {
        string section = string.Empty;
        foreach (string line in lines.Select(l => l.Trim()))
        {
            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                section = line.Substring("### ".Length);
                continue;
            }

            string[] cells = [.. line.Split('|').Select(cell => cell.Trim())];
            if (!s_id.IsMatch(cells[0]))
            {
                continue;
            }

            switch (section)
            {
                case "New Rules":
                    ledger[cells[0]] = new LedgerRow(cells[1], cells[2], cells[3], released);
                    break;
                case "Changed Rules":
                    ledger[cells[0]] = new LedgerRow(cells[1], cells[2], cells[5], released);
                    break;
                case "Removed Rules":
                    ledger.Remove(cells[0]);
                    break;
            }
        }
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

    private sealed record LedgerRow(
        string Category,
        string Severity,
        string Notes,
        bool Released);
}
