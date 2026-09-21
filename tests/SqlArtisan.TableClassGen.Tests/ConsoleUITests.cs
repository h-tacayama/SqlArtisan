using SqlArtisan.TableClassGen;

namespace SqlArtisan.TableClassGen.Tests;

// Console.ReadLine returns "" on a blank Enter (null only at EOF), so the old
// `?? "default"` fallbacks never fired (#430). Only the prompts reachable under
// redirected input are gated; the password ReadKey cannot be driven.
[Collection(ConsoleRedirectionCollection.Name)]
public class ConsoleUITests
{
    [Fact]
    public void ReadCodeGenerationSettings_BlankAnswers_AppliesTheDisplayedDefaults()
    {
        CodeGenerationSettings settings = WithInput(
            "\n\n\n\n\n",
            () => new ConsoleUI().ReadCodeGenerationSettings());

        Assert.Equal("SqlArtisan.TableDefinitions", settings.OutputNamespace);
        Assert.Equal(".", settings.OutputDirectory);
        Assert.False(settings.LowercaseNames);
        Assert.Empty(settings.TableNames);
        // No initial-letter subfolder — the create-subfolders default is "n".
        Assert.Equal(Path.Combine(".", "Users.cs"), settings.CreateOutputFilePath("Users"));
    }

    private static T WithInput<T>(string input, Func<T> read)
    {
        TextReader originalIn = Console.In;
        TextWriter originalOut = Console.Out;

        try
        {
            Console.SetIn(new StringReader(input));
            Console.SetOut(TextWriter.Null);
            return read();
        }
        finally
        {
            Console.SetIn(originalIn);
            Console.SetOut(originalOut);
        }
    }
}
