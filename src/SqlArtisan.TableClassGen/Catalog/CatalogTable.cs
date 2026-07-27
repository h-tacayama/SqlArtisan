namespace SqlArtisan.TableClassGen;

internal sealed class CatalogTable(
    string tableName,
    IReadOnlyList<CatalogColumn> columns,
    string? schema = null)
{
    public string TableName => tableName;

    public string Schema => schema ?? string.Empty;

    public string ClassName => $"{CaseConverter.SnakeToPascalCase(tableName)}Table";

    public IReadOnlyList<CatalogColumn> Columns => columns;
}
