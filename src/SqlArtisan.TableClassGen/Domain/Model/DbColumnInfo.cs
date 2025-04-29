namespace InlineSqlSharp.TableClassGen;

internal sealed class DbColumnInfo(string name, string dataType)
{
    public string Name => name;

    public string PascalCaseName => CaseConverter.SnakeToPascalCase(name);

    public string DataType => dataType;
}
