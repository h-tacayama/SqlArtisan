using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace SqlArtisan.TableClassGen;

/// <summary>
/// Writes the run's outcome. Quiet by default — a summary line, plus the drift
/// detail a caller has to act on — because the output is context every scripted
/// caller carries.
/// </summary>
internal sealed class Reporter(RunOptions options)
{
    public void Report(IReadOnlyList<TableResult> results)
    {
        if (options.Json)
        {
            ReportJson(results);
            return;
        }

        if (options.Mode == RunMode.Generate)
        {
            ReportGenerated(results);
            return;
        }

        ReportDrift(results);
    }

    public static void Error(string message)
    {
        Console.Error.WriteLine($"error: {message}");
    }

    private void ReportGenerated(IReadOnlyList<TableResult> results)
    {
        if (options.Verbose)
        {
            foreach (TableResult result in results.Where(r => r.Status != TableStatus.Removed))
            {
                Console.WriteLine($"  {result.Path}");
            }
        }

        string verb = options.DryRun ? "Would generate" : "Generated";
        int count = results.Count(r => r.Status != TableStatus.Removed);

        Console.WriteLine($"{verb} {count} table {(count == 1 ? "class" : "classes")} in {options.Settings.OutputDirectory}");
    }

    private void ReportDrift(IReadOnlyList<TableResult> results)
    {
        List<TableResult> drifted = [.. results.Where(r => r.Status != TableStatus.Unchanged)];

        if (drifted.Count == 0)
        {
            Console.WriteLine($"In sync: {results.Count} tables match {options.Settings.OutputDirectory}");
            return;
        }

        string headline = options.Mode == RunMode.Fix
            ? options.DryRun ? "Would regenerate" : "Regenerated"
            : "Drift detected against";

        Console.WriteLine(
            $"{headline} {options.Settings.OutputDirectory} ({drifted.Count} {(drifted.Count == 1 ? "table" : "tables")}):");
        Console.WriteLine();

        foreach (TableResult result in drifted)
        {
            Console.WriteLine($"  {Label(result.Status),-9} {result.TableName}");

            foreach (string change in result.Changes)
            {
                Console.WriteLine($"            {change}");
            }
        }

        Console.WriteLine();
        Console.WriteLine(NextStep(drifted));
    }

    private string NextStep(IReadOnlyList<TableResult> drifted)
    {
        IReadOnlyList<TableResult> removed = [.. drifted.Where(r => r.Status == TableStatus.Removed)];

        if (options.Mode == RunMode.Fix)
        {
            return removed.Count == 0
                ? "All drifted tables were regenerated."
                : "Their tables are gone from the database; delete these files by hand:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, removed.Select(r => $"  {r.Path}"));
        }

        StringBuilder next = new();
        IReadOnlyList<TableResult> fixable =
            [.. drifted.Where(r => r.Status is TableStatus.Added or TableStatus.Modified)];

        if (fixable.Count > 0)
        {
            next.AppendLine("Regenerate the affected tables by re-running with:");
            next.Append($"  --fix --tables {string.Join(",", fixable.Select(r => r.TableName))}");
        }

        if (removed.Count > 0)
        {
            if (next.Length > 0)
            {
                next.AppendLine();
                next.AppendLine();
            }

            next.Append(
                "These files have no table in the database and are left untouched:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, removed.Select(r => $"  {r.Path}")));
        }

        return next.ToString();
    }

    private void ReportJson(IReadOnlyList<TableResult> results)
    {
        Console.WriteLine(JsonSerializer.Serialize(
            new
            {
                mode = options.Mode.ToString().ToLowerInvariant(),
                dryRun = options.DryRun,
                drift = results.Any(r => r.Status != TableStatus.Unchanged),
                tables = results.Select(r => new
                {
                    name = r.TableName,
                    status = Label(r.Status),
                    path = r.Path,
                    changes = r.Changes,
                }),
            },
            // The relaxed encoder keeps the diff markers readable; this output goes to
            // a console and a log, never into HTML.
            new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            }));
    }

    private static string Label(TableStatus status) => status.ToString().ToLowerInvariant();
}
