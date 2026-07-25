namespace SqlArtisan.TableClassGen;

internal static class CliRunner
{
    public static int Run(string[] args)
    {
        if (CommandLine.WantsHelp(args))
        {
            Console.WriteLine(CommandLine.HelpText);
            return 0;
        }

        // Redirected stdin is never interactive, so the exit pause below cannot fire
        // on a run that had no terminal to prompt at.
        bool interactive = args.Length == 0 && !Console.IsInputRedirected;

        try
        {
            RunOptions options = args.Length == 0
                ? ReadInteractively()
                : CommandLine.Parse(args);

            IReadOnlyList<TableResult> results = new TableClassGenService(
                TableInfoRepositoryFactory.Create(options.Connection, options.Settings.LowercaseNames),
                options).Run();

            new Reporter(options).Report(results);

            return ExitCode(options, results);
        }
        catch (Exception ex)
        {
            Reporter.Error(ex.Message);
            return 2;
        }
        finally
        {
            if (interactive)
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }

    // Prompting is reserved for a terminal: a redirected stdin means a script, a CI
    // job, or an agent, and a prompt there hangs with nothing to correct from.
    private static RunOptions ReadInteractively()
    {
        if (Console.IsInputRedirected)
        {
            throw new CommandLineException(
                "No options given and stdin is not a terminal. Pass --dbms and the "
                    + "connection options (or --config <path>); run --help for the list");
        }

        ConsoleUI ui = new();

        return new RunOptions(
            RunMode.Generate,
            ui.ReadDatabaseConnectionInfo(),
            ui.ReadCodeGenerationSettings());
    }

    private static int ExitCode(RunOptions options, IReadOnlyList<TableResult> results) =>
        options.Mode switch
        {
            // A removed table's file is reported, never deleted, so --fix cannot
            // clear that drift on its own.
            RunMode.Check => results.Any(r => r.Status != TableStatus.Unchanged) ? 1 : 0,
            RunMode.Fix => results.Any(r => r.Status == TableStatus.Removed) ? 1 : 0,
            _ => 0,
        };
}
