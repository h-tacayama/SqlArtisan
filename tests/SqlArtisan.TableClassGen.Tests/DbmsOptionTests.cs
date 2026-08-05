using SqlArtisan.TableClassGen;

namespace SqlArtisan.TableClassGen.Tests;

[Collection(ConsoleRedirectionCollection.Name)]
public class DbmsOptionTests
{
    public static TheoryData<string, Dbms> Spellings =>
        new()
        {
            { "mysql", Dbms.MySql },
            { "oracle", Dbms.Oracle },
            { "postgresql", Dbms.PostgreSql },
            { "postgres", Dbms.PostgreSql },
            { "sqlite", Dbms.Sqlite },
            { "sqlserver", Dbms.SqlServer },
            { "mssql", Dbms.SqlServer },
        };

    [Theory]
    [MemberData(nameof(Spellings))]
    public void Parse_KnownSpelling_ReturnsTheDbms(string value, Dbms expected) =>
        Assert.Equal(expected, DbmsOption.Parse(value));

    [Theory]
    [MemberData(nameof(Spellings))]
    public void ParseDatabaseType_KnownSpelling_ReturnsTheDbms(string value, Dbms expected) =>
        Assert.Equal(expected, ConsoleUI.ParseDatabaseType(value));

    [Theory]
    [InlineData("PostgreSQL")]
    [InlineData("  oracle  ")]
    public void Parse_CasingAndPadding_ReturnsTheDbms(string value) =>
        Assert.Equal(
            value.Trim().StartsWith("P", StringComparison.OrdinalIgnoreCase)
                ? Dbms.PostgreSql
                : Dbms.Oracle,
            DbmsOption.Parse(value));

    [Fact]
    public void Parse_UnknownSpelling_ThrowsCommandLineException()
    {
        CommandLineException ex = Assert.Throws<CommandLineException>(() => DbmsOption.Parse("db2"));

        Assert.Equal(
            "--dbms must be one of mysql, oracle, postgresql, sqlite, sqlserver (got 'db2')",
            ex.Message);
    }

    // One exception type across both paths: the interactive one threw
    // ArgumentException, so the two reported the same mistake differently.
    [Fact]
    public void ParseDatabaseType_UnknownSpelling_ThrowsCommandLineException() =>
        Assert.Throws<CommandLineException>(() => ConsoleUI.ParseDatabaseType("db2"));

    // The bug this closes: the prompt printed "PostgreSQL" and its own parser
    // rejected that spelling. Reading the labels back out of the rendered prompt
    // keeps the two from drifting apart again.
    [Fact]
    public void ParseDatabaseType_EveryLabelThePromptPrints_Parses()
    {
        string prompt = ConsoleUI.DatabaseTypePrompt;
        string[] choices = prompt[(prompt.IndexOf('(') + 1)..prompt.LastIndexOf(')')].Split('/');

        Assert.Equal(5, choices.Length);

        for (int i = 0; i < choices.Length; i++)
        {
            string label = choices[i].Split('.', 2)[1];

            Assert.Equal(ConsoleUI.ParseDatabaseType(label), ConsoleUI.ParseDatabaseType($"{i + 1}"));
        }
    }

    [Theory]
    [InlineData(Dbms.Oracle, 1521)]
    [InlineData(Dbms.PostgreSql, 5432)]
    [InlineData(Dbms.MySql, 3306)]
    [InlineData(Dbms.SqlServer, 1433)]
    // SQLite is file-based, so it never reaches a port prompt or --port default.
    [InlineData(Dbms.Sqlite, 0)]
    public void DefaultPort_EveryDbms_ReturnsThePort(Dbms dbms, int expected) =>
        Assert.Equal(expected, DbmsOption.DefaultPort(dbms));

    [Fact]
    public void ReadPort_BlankAnswer_ReturnsTheDbmsDefault() =>
        Assert.Equal(5432, WithInput("\n", () => ConsoleUI.ReadPort(Dbms.PostgreSql)));

    [Fact]
    public void ReadPort_ValidNumber_ReturnsIt() =>
        Assert.Equal(6543, WithInput("6543\n", () => ConsoleUI.ReadPort(Dbms.PostgreSql)));

    // Used to throw FormatException straight out of the prompt loop; a bad answer
    // must reprompt instead of crashing the interactive flow.
    [Fact]
    public void ReadPort_NonNumericThenValid_RepromptsAndReturnsTheValidAnswer() =>
        Assert.Equal(6543, WithInput("abc\n6543\n", () => ConsoleUI.ReadPort(Dbms.PostgreSql)));

    [Fact]
    public void ReadPort_OutOfRangeThenValid_RepromptsAndReturnsTheValidAnswer() =>
        Assert.Equal(6543, WithInput("70000\n6543\n", () => ConsoleUI.ReadPort(Dbms.PostgreSql)));

    private static int WithInput(string input, Func<int> read)
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
