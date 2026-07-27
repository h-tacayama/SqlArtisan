namespace SqlArtisan.TableClassGen;

internal interface ICatalogReader
{
    IReadOnlyList<CatalogTable> GetAllTables();

    bool TryGetTable(string tableName, out CatalogTable? table);
}
