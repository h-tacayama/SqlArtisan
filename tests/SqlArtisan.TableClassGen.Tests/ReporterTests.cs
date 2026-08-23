using SqlArtisan.TableClassGen;

namespace SqlArtisan.TableClassGen.Tests;

// The regression this guards: the --fix headline counted every drifted table,
// Removed included, though Fix regenerates only the NeedsWrite ones and deletes
// nothing. It now states both counts, in words that do not claim a deletion.
[Collection(ConsoleRedirectionCollection.Name)]
public class ReporterTests
{
    [Fact]
    public void Report_Fix_ModifiedAndRemovedDrift_HeadlineStatesBothCounts()
    {
        TableResult modified = new("item", "ItemTable.cs", TableStatus.Modified, ["+ note"]);
        TableResult removed = new("gone", "GoneTable.cs", TableStatus.Removed, []);

        string report = Capture(() => new Reporter(FixOptions()).Report([modified, removed]));

        Assert.Contains(
            "Regenerated 1 table in ., leaving 1 file untouched:", report, StringComparison.Ordinal);
    }

    [Fact]
    public void Report_Fix_RemovedOnlyDrift_HeadlineRegeneratesNothing()
    {
        TableResult removed = new("gone", "GoneTable.cs", TableStatus.Removed, []);

        string report = Capture(() => new Reporter(FixOptions()).Report([removed]));

        Assert.Contains(
            "Regenerated 0 tables in ., leaving 1 file untouched:",
            report,
            StringComparison.Ordinal);
    }

    // The legal twin: with nothing left behind, the headline drops the clause
    // rather than claiming "leaving 0 files untouched".
    [Fact]
    public void Report_Fix_NoRemovedDrift_HeadlineOmitsTheUntouchedClause()
    {
        TableResult added = new("item", "ItemTable.cs", TableStatus.Added, []);
        TableResult modified = new("tag", "TagTable.cs", TableStatus.Modified, ["+ note"]);

        string report = Capture(() => new Reporter(FixOptions()).Report([added, modified]));

        Assert.Contains("Regenerated 2 tables in .:", report, StringComparison.Ordinal);
        Assert.DoesNotContain("untouched:", report, StringComparison.Ordinal);
    }

    [Fact]
    public void Report_FixDryRun_ModifiedAndRemovedDrift_HeadlineClaimsNoWriteYet()
    {
        TableResult modified = new("item", "ItemTable.cs", TableStatus.Modified, ["+ note"]);
        TableResult removed = new("gone", "GoneTable.cs", TableStatus.Removed, []);

        string report = Capture(
            () => new Reporter(Options(RunMode.Fix, dryRun: true)).Report([modified, removed]));

        Assert.Contains(
            "Would regenerate 1 table in ., leaving 1 file untouched:",
            report,
            StringComparison.Ordinal);
    }

    // Check reports drift rather than writes, so its count stays the plain total.
    [Fact]
    public void Report_Check_ModifiedAndRemovedDrift_HeadlineCountsEveryDriftedTable()
    {
        TableResult modified = new("item", "ItemTable.cs", TableStatus.Modified, ["+ note"]);
        TableResult removed = new("gone", "GoneTable.cs", TableStatus.Removed, []);

        string report = Capture(
            () => new Reporter(Options(RunMode.Check)).Report([modified, removed]));

        Assert.Contains("Drift detected against . (2 tables):", report, StringComparison.Ordinal);
    }

    // The regression this guards: with orphans beside regenerated tables, NextStep
    // dropped the one instruction a --fix --dry-run run exists to lead into.
    [Fact]
    public void Report_FixDryRun_ModifiedAndRemovedDrift_NextStepKeepsTheReRunInstruction()
    {
        TableResult modified = new("item", "ItemTable.cs", TableStatus.Modified, ["+ note"]);
        TableResult removed = new("gone", "GoneTable.cs", TableStatus.Removed, []);

        string report = Capture(
            () => new Reporter(Options(RunMode.Fix, dryRun: true)).Report([modified, removed]));

        Assert.Contains(
            "Re-run without --dry-run to regenerate them.", report, StringComparison.Ordinal);
        Assert.Contains("delete these files by hand:", report, StringComparison.Ordinal);
    }

    // The legal twin: with only orphans, no regeneration happened, so neither the
    // dry-run instruction nor the "regenerated" claim may appear.
    [Fact]
    public void Report_Fix_RemovedOnlyDrift_NextStepOmitsTheRegeneratedClaim()
    {
        TableResult removed = new("gone", "GoneTable.cs", TableStatus.Removed, []);

        string report = Capture(() => new Reporter(FixOptions()).Report([removed]));

        Assert.DoesNotContain(
            "All drifted tables were regenerated.", report, StringComparison.Ordinal);
        Assert.Contains("delete these files by hand:", report, StringComparison.Ordinal);
    }

    private static RunOptions FixOptions() => Options(RunMode.Fix);

    private static RunOptions Options(RunMode mode, bool dryRun = false) =>
        new(mode, DummyConnection(), TestSettings.Create(), dryRun);

    private static DbConnectionInfo DummyConnection() =>
        new(Dbms.Sqlite, string.Empty, 0, string.Empty, string.Empty, string.Empty, string.Empty);

    private static string Capture(Action report)
    {
        TextWriter original = Console.Out;
        StringWriter captured = new();

        try
        {
            Console.SetOut(captured);
            report();
        }
        finally
        {
            Console.SetOut(original);
        }

        return captured.ToString();
    }
}
