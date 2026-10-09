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
            "Regenerated 1 table in ., leaving 1 "
                + "file untouched:", report, StringComparison.Ordinal);
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

    // --verbose lists every table in the drift modes as it does in generate mode;
    // without it, a table found current and one never checked read the same.
    [Fact]
    public void Report_CheckVerbose_ListsUnchangedTablesToo()
    {
        TableResult unchanged = new("item", "ItemTable.cs", TableStatus.Unchanged, []);
        TableResult modified = new("order", "OrderTable.cs", TableStatus.Modified, ["+ note"]);

        string report = Capture(
            () => new Reporter(Options(RunMode.Check, verbose: true)).Report(
                [unchanged, modified]));

        Assert.Contains("unchanged item", report, StringComparison.Ordinal);
        Assert.Contains("modified  order", report, StringComparison.Ordinal);
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

    // A --tables run never scans for orphans, so "All drifted tables" claimed a
    // directory it did not read while --check, run unscoped, still found one.
    [Fact]
    public void Report_FixScopedByTables_ClosingLineSendsOrphansToAFullRun()
    {
        TableResult modified = new("item", "ItemTable.cs", TableStatus.Modified, ["+ note"]);
        RunOptions options = new(
            RunMode.Fix, DummyConnection(), TestSettings.Create(tableNames: ["item"]));

        string report = Capture(() => new Reporter(options).Report([modified]));

        Assert.Contains(
            "The drifted tables named by --tables were regenerated; run without --tables "
                + "to also find files whose table is gone.",
            report,
            StringComparison.Ordinal);
        Assert.DoesNotContain("All drifted tables", report, StringComparison.Ordinal);
    }

    // The documented --format json shape, byte for byte: a script parses it, so a
    // renamed member or a reordered key must fail here (README § "JSON output").
    [Theory]
    [InlineData("generate")]
    [InlineData("check")]
    [InlineData("fix")]
    public void Report_Json_EmitsTheDocumentedShape(string modeName)
    {
        RunMode mode = modeName switch
        {
            "generate" => RunMode.Generate,
            "check" => RunMode.Check,
            _ => RunMode.Fix,
        };
        TableResult[] results =
        [
            new("customers", "out/CustomersTable.cs", TableStatus.Unchanged, []),
            new("audit_log", "out/AuditLogTable.cs", TableStatus.Added, []),
            new("employees", "out/EmployeesTable.cs", TableStatus.Modified, ["+ email", "~ id"]),
            new("DepartmentsTable.cs", "out/DepartmentsTable.cs", TableStatus.Removed, []),
        ];
        RunOptions options = new(
            mode,
            DummyConnection(),
            TestSettings.Create(),
            dryRun: true,
            json: true);

        string report = Capture(() => new Reporter(options).Report(results));

        Assert.Equal(
            $$"""
            {
              "mode": "{{modeName}}",
              "dryRun": true,
              "drift": true,
              "tables": [
                {
                  "name": "customers",
                  "status": "unchanged",
                  "path": "out/CustomersTable.cs",
                  "changes": []
                },
                {
                  "name": "audit_log",
                  "status": "added",
                  "path": "out/AuditLogTable.cs",
                  "changes": []
                },
                {
                  "name": "employees",
                  "status": "modified",
                  "path": "out/EmployeesTable.cs",
                  "changes": [
                    "+ email",
                    "~ id"
                  ]
                },
                {
                  "name": "DepartmentsTable.cs",
                  "status": "removed",
                  "path": "out/DepartmentsTable.cs",
                  "changes": []
                }
              ]
            }

            """,
            report.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void Report_Json_NoDrift_DriftIsFalse()
    {
        TableResult unchanged = new("item", "ItemTable.cs", TableStatus.Unchanged, []);
        RunOptions options = new(
            RunMode.Check,
            DummyConnection(),
            TestSettings.Create(),
            json: true);

        string report = Capture(() => new Reporter(options).Report([unchanged]));

        Assert.Contains("\"drift\": false", report, StringComparison.Ordinal);
    }

    private static RunOptions FixOptions() => Options(RunMode.Fix);

    private static RunOptions Options(RunMode mode, bool dryRun = false, bool verbose = false) =>
        new(mode, DummyConnection(), TestSettings.Create(), dryRun, verbose: verbose);

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
