namespace InlineSqlSharp;

internal sealed class ColumnCore(string tableAlias, string columnName)
{
    private readonly string _tableAlias = tableAlias;
    private readonly string _columnName = columnName;

    internal void FormatSql(SqlBuildingBuffer buffer) => buffer
        .EncloseInDoubleQuotes(_tableAlias)
        .Append(".")
        .Append(_columnName);
}
