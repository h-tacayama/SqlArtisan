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
}
