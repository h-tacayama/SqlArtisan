namespace SqlArtisan.TableClassGen;

internal interface ITableInfoRepository
{
    IReadOnlyList<DbTableInfo> GetAllTables();

    bool TryGetTableInfo(string tableName, out DbTableInfo? table);
}
