namespace SqlArtisan.TableClassGen;

// One accepted set for --dbms and for the interactive prompt alike: while each
// had its own parser, the prompt printed a "PostgreSQL" label its own parser
// rejected.
internal static class DbmsOption
{
    private static readonly Dictionary<string, Dbms> Names =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["mysql"] = Dbms.MySql,
            ["oracle"] = Dbms.Oracle,
            ["postgresql"] = Dbms.PostgreSql,
            ["postgres"] = Dbms.PostgreSql,
            ["sqlite"] = Dbms.Sqlite,
            ["sqlserver"] = Dbms.SqlServer,
            ["mssql"] = Dbms.SqlServer,
        };

    public static Dbms Parse(string value) =>
        TryParse(value, out Dbms dbms)
            ? dbms
            : throw new CommandLineException(
                $"--dbms must be one of mysql, oracle, postgresql, sqlite, sqlserver (got '{value}')");

    // The interactive prompt shares the name table but owns its own wording —
    // its user never typed a --dbms flag.
    public static bool TryParse(string value, out Dbms dbms) =>
        Names.TryGetValue(value.Trim(), out dbms);

    // SQLite is file-based and reaches here with no port to default.
    public static int DefaultPort(Dbms dbms) =>
        dbms switch
        {
            Dbms.Oracle => 1521,
            Dbms.PostgreSql => 5432,
            Dbms.MySql => 3306,
            Dbms.SqlServer => 1433,
            _ => 0,
        };
}
