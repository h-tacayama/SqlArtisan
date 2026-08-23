using SqlArtisan.TableClassGen;

namespace SqlArtisan.TableClassGen.Tests;

// Console.ReadLine returns "" on a blank Enter (null only at EOF), so the old
// `?? "default"` fallbacks never fired; the prompts now go through the
// IsNullOrWhiteSpace-then-default idiom ReadPort set (#430). Gated here for
// the prompts reachable under redirected input — the connection prompts end in
// a Console.ReadKey password read, which redirection cannot drive.
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
