namespace SqlArtisan.TableClassGen;

internal static class TableInfoRepositoryFactory
{
    public static ITableInfoRepository Create(
        DbConnectionInfo connInfo,
        bool lowercaseNames) =>
        connInfo.DbmsType switch
        {
            DbmsType.Oracle => new OracleTableInfoRepository(connInfo, lowercaseNames),
            DbmsType.PostgreSql or DbmsType.MySql or DbmsType.SqlServer =>
                new InformationSchemaTableInfoRepository(connInfo, lowercaseNames),
            DbmsType.Sqlite => new SqliteTableInfoRepository(connInfo, lowercaseNames),
            _ => throw new ArgumentOutOfRangeException(nameof(connInfo.DbmsType))
        };
}
