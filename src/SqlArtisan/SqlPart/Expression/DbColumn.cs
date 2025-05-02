namespace SqlArtisan;

public sealed class DbColumn(string tableAlias, string columnName) : SqlExpression
{
    private readonly string _tableAlias = tableAlias;
    private readonly string _columnName = columnName;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .EncloseInDoubleQuotes(_tableAlias)
        .Append($".{_columnName}");
}
