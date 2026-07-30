using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace SqlArtisan.TableClassGen;

// Quiet by default — a summary line plus the drift detail a caller has to act on —
// because this output is context every scripted caller carries.
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
        IReadOnlyList<TableResult> scanned =
            [.. results.Where(r => r.Status != TableStatus.Removed)];
        int written = scanned.Count(r => r.Status is TableStatus.Added or TableStatus.Modified);

        if (options.Verbose)
        {
            // Every table read, not only the writes: a file left alone because it is
            // already current would otherwise read as one the run never looked at.
            foreach (TableResult result in scanned)
            {
                Console.WriteLine($"  {Label(result.Status),-9} {result.Path}");
            }
        }

        string verb = options.DryRun ? "Would generate" : "Generated";
        string noun = scanned.Count == 1 ? "class" : "classes";

        // Both counts, so the line answers "what changed" and "what was read" at
        // once — and a dry run states the same pair a real run will.
        Console.WriteLine(
            $"{verb} {written} of {scanned.Count} table {noun} in {options.Settings.OutputDirectory}");

        // The orphans are found in every full run, and --format json reports them
        // either way, so the text output must not be the one that stays quiet.
        if (Orphans(results) is { Count: > 0 } orphans)
        {
            Console.WriteLine();
            Console.WriteLine(OrphanNotice(orphans));
        }
    }

    private void ReportDrift(IReadOnlyList<TableResult> results)
    {
        List<TableResult> drifted = [.. results.Where(r => r.Status != TableStatus.Unchanged)];

        if (drifted.Count == 0)
        {
            Console.WriteLine(
                $"In sync: {results.Count} {(results.Count == 1 ? "table" : "tables")} match {options.Settings.OutputDirectory}");
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
        IReadOnlyList<TableResult> removed = Orphans(drifted);

        if (options.Mode == RunMode.Fix)
        {
            return removed.Count == 0
                ? options.DryRun
                    ? "Re-run without --dry-run to regenerate them."
                    : "All drifted tables were regenerated."
                : "Their tables are gone from the database; delete these files by hand:"
                    + Environment.NewLine
                    + Paths(removed);
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

            next.Append(OrphanNotice(removed));
        }

        return next.ToString();
    }

    private static IReadOnlyList<TableResult> Orphans(IReadOnlyList<TableResult> results) =>
        [.. results.Where(r => r.Status == TableStatus.Removed)];

    private static string OrphanNotice(IReadOnlyList<TableResult> orphans) =>
        "These files have no table in the database and are left untouched:"
        + Environment.NewLine
        + Paths(orphans);

    private static string Paths(IReadOnlyList<TableResult> results) =>
        string.Join(Environment.NewLine, results.Select(r => $"  {r.Path}"));

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
