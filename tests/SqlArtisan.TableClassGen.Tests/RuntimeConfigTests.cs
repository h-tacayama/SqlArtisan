using System.Text.Json;

namespace SqlArtisan.TableClassGen.Tests;

// The tool targets net8.0; without RollForward the produced runtimeconfig keeps
// the Minor policy, and `dotnet tool install` gives a tool that does not start
// where only .NET 9 or later is installed.
public class RuntimeConfigTests
{
    [Fact]
    public void ProducedRuntimeConfig_RollsForwardToNewerMajorRuntimes()
    {
        string path = Path.Combine(
            AppContext.BaseDirectory,
            "SqlArtisan.TableClassGen.runtimeconfig.json");

        using JsonDocument config = JsonDocument.Parse(File.ReadAllText(path));

        JsonElement runtimeOptions = config.RootElement.GetProperty("runtimeOptions");

        Assert.Equal("Major", runtimeOptions.GetProperty("rollForward").GetString());
    }
}
