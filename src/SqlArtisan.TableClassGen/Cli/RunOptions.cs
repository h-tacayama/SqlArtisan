namespace SqlArtisan.TableClassGen;

internal enum RunMode
{
    Generate = 0,
    Check = 1,
    Fix = 2,
}

internal sealed class RunOptions(
    RunMode mode,
    DbConnectionInfo connection,
    CodeGenerationSettings settings,
    bool dryRun = false,
    bool json = false,
    bool verbose = false)
{
    public RunMode Mode => mode;

    public DbConnectionInfo Connection => connection;

    public CodeGenerationSettings Settings => settings;

    public bool DryRun => dryRun;

    public bool Json => json;

    public bool Verbose => verbose;
}
