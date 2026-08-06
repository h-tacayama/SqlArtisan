using SqlArtisan.TableClassGen;

namespace SqlArtisan.TableClassGen.Tests;

// The regression this guards: the --fix headline counted every drifted table,
// Removed included, though Fix only ever regenerates the NeedsWrite ones. It now
// states the pair, so the orphans it cannot write show as the shortfall.
[Collection(ConsoleRedirectionCollection.Name)]
public class ReporterTests
{
    [Fact]
    public void Report_Fix_RemovedOnlyDrift_HeadlineWritesNoneOfTheDrift()
    {
        TableResult removed = new("gone", "GoneTable.cs", TableStatus.Removed, []);

        string report = Capture(() => new Reporter(FixOptions()).Report([removed]));

        Assert.Contains("Regenerated . (0 of 1 table):", report, StringComparison.Ordinal);
    }

    [Fact]
    public void Report_Fix_ModifiedAndRemovedDrift_HeadlineWritesOnlyTheModified()
    {
        TableResult modified = new("item", "ItemTable.cs", TableStatus.Modified, ["+ note"]);
        TableResult removed = new("gone", "GoneTable.cs", TableStatus.Removed, []);

        string report = Capture(() => new Reporter(FixOptions()).Report([modified, removed]));

        Assert.Contains("Regenerated . (1 of 2 tables):", report, StringComparison.Ordinal);
    }

    // The legal twin: with no orphan to leave behind, the pair has to close.
    [Fact]
    public void Report_Fix_NoRemovedDrift_HeadlineWritesEveryDriftedTable()
    {
        TableResult added = new("item", "ItemTable.cs", TableStatus.Added, []);
        TableResult modified = new("tag", "TagTable.cs", TableStatus.Modified, ["+ note"]);

        string report = Capture(() => new Reporter(FixOptions()).Report([added, modified]));

        Assert.Contains("Regenerated . (2 of 2 tables):", report, StringComparison.Ordinal);
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

    private static RunOptions FixOptions() => Options(RunMode.Fix);

    private static RunOptions Options(RunMode mode) =>
        new(mode, DummyConnection(), TestSettings.Create());

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
