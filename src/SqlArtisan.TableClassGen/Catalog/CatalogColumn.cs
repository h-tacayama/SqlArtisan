namespace SqlArtisan.TableClassGen;

// Every fact is tri-state: null means the catalog path could not determine it,
// which is not the same as determining it false.
internal sealed class CatalogColumn(
    string name,
    string dataType,
    bool? isNullable = null,
    bool? hasDefault = null,
    bool? isIndexed = null)
{
    public string Name => name;

    public string PascalCaseName => CaseConverter.SnakeToPascalCase(name);

    public string DataType => dataType;

    public bool? IsNullable => isNullable;

    public bool? HasDefault => hasDefault;

    public bool? IsIndexed => isIndexed;
}
