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
