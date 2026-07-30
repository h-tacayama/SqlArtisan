using SqlArtisan.TableClassGen;

namespace SqlArtisan.TableClassGen.Tests;

public class CliRunnerTests
{
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

        Assert.Equal(2, CliRunner.Run([]));
    }

    // The two must agree: a dry run that promises nothing followed by a real run
    // claiming three writes reads as a broken preview, and the --verbose listing
    // would name files whose timestamps never moved.
    [Fact]
    public void Run_OnInSyncTree_DryRunAndRealRunAgreeOnNoWrites()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            "CREATE TABLE item (id INTEGER PRIMARY KEY, name TEXT);");
        string outputDirectory = Path.Combine(
            Path.GetTempPath(), $"sqlartisan_tcg_cli_{Guid.NewGuid():N}");
        TextWriter original = Console.Out;

        try
        {
            string[] args =
            [
                "--dbms", "sqlite", "--file", db.ConnectionInfo.ServiceName,
                "--namespace", "N", "--output", outputDirectory,
            ];

            Assert.Equal(0, CliRunner.Run(args));

            StringWriter captured = new();
            Console.SetOut(captured);
            Assert.Equal(0, CliRunner.Run([.. args, "--verbose"]));
            Console.SetOut(original);

            string output = captured.ToString();

            Assert.Contains("Generated 0 table classes", output, StringComparison.Ordinal);
            Assert.DoesNotContain("ItemTable.cs", output, StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(original);

            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    // --dry-run promises what a real run would write, and a real run stopped
    // rewriting files that are already current — so an in-sync tree must promise
    // nothing. Naming files here would send a scripted caller to review an empty
    // change. Console is safe to redirect: CliRunner is driven only from this
    // class, whose tests xUnit runs as one serialized collection.
    [Fact]
    public void Run_DryRunOnInSyncTree_PromisesNoWrites()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            "CREATE TABLE item (id INTEGER PRIMARY KEY, name TEXT);");
        string outputDirectory = Path.Combine(
            Path.GetTempPath(), $"sqlartisan_tcg_cli_{Guid.NewGuid():N}");
        TextWriter original = Console.Out;

        try
        {
            string[] args =
            [
                "--dbms", "sqlite", "--file", db.ConnectionInfo.ServiceName,
                "--namespace", "N", "--output", outputDirectory,
            ];

            Assert.Equal(0, CliRunner.Run(args));

            StringWriter captured = new();
            Console.SetOut(captured);
            Assert.Equal(0, CliRunner.Run([.. args, "--dry-run", "--verbose"]));
            Console.SetOut(original);

            string output = captured.ToString();

            Assert.Contains("Would generate 0 table classes", output, StringComparison.Ordinal);
            Assert.DoesNotContain("ItemTable.cs", output, StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(original);

            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    // The legal twin: a dry run with real work to do must still name it.
    [Fact]
    public void Run_DryRunWithDrift_PromisesTheDriftedTable()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            "CREATE TABLE item (id INTEGER PRIMARY KEY, name TEXT);");
        string outputDirectory = Path.Combine(
            Path.GetTempPath(), $"sqlartisan_tcg_cli_{Guid.NewGuid():N}");
        TextWriter original = Console.Out;

        try
        {
            string[] args =
            [
                "--dbms", "sqlite", "--file", db.ConnectionInfo.ServiceName,
                "--namespace", "N", "--output", outputDirectory,
            ];

            Assert.Equal(0, CliRunner.Run(args));
            db.Execute("ALTER TABLE item ADD COLUMN note TEXT");

            StringWriter captured = new();
            Console.SetOut(captured);
            Assert.Equal(0, CliRunner.Run([.. args, "--dry-run", "--verbose"]));
            Console.SetOut(original);

            string output = captured.ToString();

            Assert.Contains("Would generate 1 table class", output, StringComparison.Ordinal);
            Assert.Contains("ItemTable.cs", output, StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(original);

            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
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
}
