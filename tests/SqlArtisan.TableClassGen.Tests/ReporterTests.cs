using SqlArtisan.TableClassGen;

namespace SqlArtisan.TableClassGen.Tests;

// The regression this guards: the --fix headline counted every drifted table,
// Removed included, though Fix only ever regenerates the NeedsWrite ones — the
// count disagreed with the file list NextStep prints right below it.
public class ReporterTests
{
    [Fact]
    public void Report_Fix_RemovedOnlyDrift_HeadlineCountExcludesIt()
    {
        TableResult removed = new("gone", "GoneTable.cs", TableStatus.Removed, []);

        string report = Capture(() => new Reporter(FixOptions()).Report([removed]));

        Assert.Contains("Regenerated . (0 tables):", report, StringComparison.Ordinal);
    }

    [Fact]
    public void Report_Fix_ModifiedAndRemovedDrift_HeadlineCountsOnlyModified()
    {
        TableResult modified = new("item", "ItemTable.cs", TableStatus.Modified, ["+ note"]);
        TableResult removed = new("gone", "GoneTable.cs", TableStatus.Removed, []);

        string report = Capture(() => new Reporter(FixOptions()).Report([modified, removed]));

        Assert.Contains("Regenerated . (1 table):", report, StringComparison.Ordinal);
    }

    private static RunOptions FixOptions() =>
        new(RunMode.Fix, DummyConnection(), TestSettings.Create());

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
