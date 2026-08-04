using SqlArtisan.TableClassGen;

namespace SqlArtisan.TableClassGen.Tests;

public class CliRunnerTests
{
    private const string TwoTables =
        """
        CREATE TABLE item (id INTEGER PRIMARY KEY, name TEXT);
        CREATE TABLE tag (id INTEGER PRIMARY KEY, label TEXT);
        """;

    [Fact]
    public void Run_Help_ExitsSuccessfully()
    {
        Assert.Equal(0, CliRunner.Run(["--help"]));
    }

    // The regression this guards: the exit pause used to run for any argument-less
    // invocation, so a redirected stdin reached Console.ReadKey and aborted the
    // process instead of reporting the error.
    [Fact]
    public void Run_NoArgumentsWithRedirectedStdin_ReportsErrorWithoutPrompting()
    {
        Assert.True(Console.IsInputRedirected, "the test host must redirect stdin for this test");

        string stderr = CaptureError(() => CliRunner.Run([]));

        Assert.Contains(
            "error: No options given and stdin is not a terminal.", stderr, StringComparison.Ordinal);
    }

    // Skipping the write must not make a table invisible: the run has to stay
    // distinguishable from one that never read the table at all.
    [Fact]
    public void Run_InSyncTree_ReportsEveryTableReadAndNoneWritten()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(TwoTables);
        using TempOutputDirectory output = new();

        Assert.Equal(0, CliRunner.Run(output.Args(db)));

        string report = Capture(() => CliRunner.Run([.. output.Args(db), "--verbose"]));

        Assert.Contains("Generated 0 of 2 table classes", report, StringComparison.Ordinal);
        Assert.Contains("unchanged", LineNaming(report, "ItemTable.cs"), StringComparison.Ordinal);
        Assert.Contains("unchanged", LineNaming(report, "TagTable.cs"), StringComparison.Ordinal);
    }

    // A dry run is only useful as a preview if it states the pair the real run
    // will: a different denominator on either side reads as a broken preview.
    [Fact]
    public void Run_WithDrift_DryRunAndRealRunReportTheSameCounts()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(TwoTables);
        using TempOutputDirectory output = new();

        Assert.Equal(0, CliRunner.Run(output.Args(db)));
        db.Execute("ALTER TABLE item ADD COLUMN note TEXT");

        string dry = Capture(() => CliRunner.Run([.. output.Args(db), "--dry-run", "--verbose"]));
        string real = Capture(() => CliRunner.Run([.. output.Args(db), "--verbose"]));

        Assert.Contains("Would generate 1 of 2 table classes", dry, StringComparison.Ordinal);
        Assert.Contains("Generated 1 of 2 table classes", real, StringComparison.Ordinal);
        Assert.Contains("modified", LineNaming(dry, "ItemTable.cs"), StringComparison.Ordinal);
        Assert.Contains("modified", LineNaming(real, "ItemTable.cs"), StringComparison.Ordinal);
        Assert.Contains("unchanged", LineNaming(real, "TagTable.cs"), StringComparison.Ordinal);
    }

    // A dry --fix regenerates nothing, so drift must still exit 1 — reporting
    // "fixed" via exit 0 would let a scripted caller skip the real run.
    [Fact]
    public void Run_FixDryRunWithDrift_ExitsOne()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            "CREATE TABLE item (id INTEGER PRIMARY KEY, name TEXT);");
        string outputDirectory = Path.Combine(
            Path.GetTempPath(), $"sqlartisan_tcg_cli_{Guid.NewGuid():N}");

        try
        {
            string[] args =
            [
                "--dbms", "sqlite", "--file", db.ConnectionInfo.ServiceName,
                "--namespace", "N", "--output", outputDirectory,
            ];

            Assert.Equal(0, CliRunner.Run(args));

            db.Execute("ALTER TABLE item ADD COLUMN note TEXT");

            Assert.Equal(1, CliRunner.Run([.. args, "--fix", "--dry-run"]));
            Assert.Equal(1, CliRunner.Run([.. args, "--check"]));
            Assert.Equal(0, CliRunner.Run([.. args, "--fix"]));
            Assert.Equal(0, CliRunner.Run([.. args, "--check"]));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    // Returns stdout; a non-zero exit fails here rather than surfacing later as a
    // puzzling assertion on missing text. Redirecting Console is safe because
    // CliRunner is driven only from this class, which xUnit runs as one collection.
    private static string Capture(Func<int> run)
    {
        TextWriter original = Console.Out;
        StringWriter captured = new();

        try
        {
            Console.SetOut(captured);
            Assert.Equal(0, run());
        }
        finally
        {
            Console.SetOut(original);
        }

        return captured.ToString();
    }

    // The Reporter.Error path writes to stderr, not stdout — Capture() above never
    // sees it, so an untested exit-2 path could silently drop the error message.
    private static string CaptureError(Func<int> run)
    {
        TextWriter original = Console.Error;
        StringWriter captured = new();

        try
        {
            Console.SetError(captured);
            Assert.Equal(2, run());
        }
        finally
        {
            Console.SetError(original);
        }

        return captured.ToString();
    }

    // Asserting the padded column width would pin the format string rather than
    // the status, so the status is read off the line naming the file.
    private static string LineNaming(string report, string fileName) =>
        Assert.Single(
            report.Split(Environment.NewLine)
                .Where(line => line.Contains(fileName, StringComparison.Ordinal)));

    private sealed class TempOutputDirectory : IDisposable
    {
        private readonly string _path = Path.Combine(
            Path.GetTempPath(), $"sqlartisan_tcg_cli_{Guid.NewGuid():N}");

        public string[] Args(TempSqliteDatabase db) =>
        [
            "--dbms", "sqlite", "--file", db.ConnectionInfo.ServiceName,
            "--namespace", "N", "--output", _path,
        ];

        public void Dispose()
        {
            if (Directory.Exists(_path))
            {
                Directory.Delete(_path, recursive: true);
            }
        }
    }
}
