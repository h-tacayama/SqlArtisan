namespace SqlArtisan;

public sealed class Column(string tableAlias, string columnName) :
    AbstractExpr
{
    private readonly string _tableAlias = tableAlias;
    private readonly string _columnName = columnName;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .EncloseInDoubleQuotes(_tableAlias)
        .Append(".")
        .Append(_columnName);
}
