namespace SqlArtisan.TableClassGen;

// isNullable / hasDefault are tri-state: null means the catalog path could not
// determine the fact, which is not the same as determining it false.
internal sealed class DbColumnInfo(
    string name,
    string dataType,
    bool? isNullable = null,
    bool? hasDefault = null)
{
    public string Name => name;

    public string PascalCaseName => CaseConverter.SnakeToPascalCase(name);

    public string DataType => dataType;

    public bool? IsNullable => isNullable;

    public bool? HasDefault => hasDefault;
}
