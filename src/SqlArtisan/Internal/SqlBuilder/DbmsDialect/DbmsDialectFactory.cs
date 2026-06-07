namespace SqlArtisan.Internal;

internal static class DbmsDialectFactory
{
    // Dialects are stateless, so a single cached instance per DBMS is shared
    // across all builds to avoid allocating one on every Build() call.
    private static readonly MySqlDialect s_mySql = new();
    private static readonly OracleDialect s_oracle = new();
    private static readonly PostgreSqlDialect s_postgreSql = new();
    private static readonly SqliteDialect s_sqlite = new();
    private static readonly SqlServerDialect s_sqlServer = new();

    internal static IDbmsDialect Create(Dbms dbmsType)
    {
        return dbmsType switch
        {
            Dbms.MySql => s_mySql,
            Dbms.Oracle => s_oracle,
            Dbms.PostgreSql => s_postgreSql,
            Dbms.Sqlite => s_sqlite,
            Dbms.SqlServer => s_sqlServer,
            _ => throw new ArgumentOutOfRangeException(nameof(dbmsType), dbmsType, "Unsupported DBMS.")
        };
    }
}
