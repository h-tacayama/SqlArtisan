namespace SqlArtisan.TableClassGen;

// Every fact is tri-state: null means the catalog path could not determine it,
// which is not the same as determining it false.
internal sealed class CatalogColumn(
    string name,
    string dataType,
    bool? isNullable = null,
    bool? hasDefault = null,
    bool? isIndexed = null,
    Dbms dbms = Dbms.Unknown)
{
    public string Name => name;

    public string PascalCaseName => CaseConverter.SnakeToPascalCase(name);

    public string DataType => dataType;

    // Derived rather than stored, so the catalog's own type name and the category
    // it reduces to can never disagree.
    public DbTypeCategory? ColumnType => ColumnCategory.Of(dbms, dataType);

    public bool? IsNullable => isNullable;

    public bool? HasDefault => hasDefault;

    public bool? IsIndexed => isIndexed;
}
