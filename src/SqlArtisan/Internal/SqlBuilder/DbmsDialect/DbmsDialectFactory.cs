namespace SqlArtisan;

internal static class DbmsDialectFactory
{
    internal static IDbmsDialect Create(Dbms dbmsType)
    {
        return dbmsType switch
        {
            Dbms.MySql => new MySqlDialect(),
            Dbms.Oracle => new OracleDialect(),
            Dbms.PostgreSql => new PostgreSqlDialect(),
            Dbms.Sqlite => new SqliteDialect(),
            Dbms.SqlServer => new SqlServerDialect(),
            _ => throw new ArgumentOutOfRangeException(nameof(dbmsType), dbmsType, "Unsupported DBMS.")
        };
    }
}
