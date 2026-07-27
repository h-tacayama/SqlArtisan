namespace SqlArtisan.TableClassGen;

internal static class CatalogReaderFactory
{
    public static ICatalogReader Create(
        DbConnectionInfo connInfo,
        bool lowercaseNames) =>
        connInfo.DbmsType switch
        {
            DbmsType.Oracle => new OracleCatalogReader(connInfo, lowercaseNames),
            DbmsType.PostgreSql or DbmsType.MySql or DbmsType.SqlServer =>
                new InformationSchemaCatalogReader(connInfo, lowercaseNames),
            DbmsType.Sqlite => new SqliteCatalogReader(connInfo, lowercaseNames),
            _ => throw new ArgumentOutOfRangeException(nameof(connInfo.DbmsType))
        };
}
