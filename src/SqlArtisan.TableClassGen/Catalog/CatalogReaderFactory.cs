namespace SqlArtisan.TableClassGen;

internal static class CatalogReaderFactory
{
    public static ICatalogReader Create(
        DbConnectionInfo connInfo,
        bool lowercaseNames) =>
        connInfo.Dbms switch
        {
            Dbms.Oracle => new OracleCatalogReader(connInfo, lowercaseNames),
            Dbms.PostgreSql or Dbms.MySql or Dbms.SqlServer =>
                new InformationSchemaCatalogReader(connInfo, lowercaseNames),
            Dbms.Sqlite => new SqliteCatalogReader(connInfo, lowercaseNames),
            _ => throw new ArgumentOutOfRangeException(nameof(connInfo))
        };
}
