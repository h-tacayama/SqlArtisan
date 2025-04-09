namespace InlineSqlSharp;

public sealed class NumericColumn(string tableAlias, string columnName) :
    NumericExpr,
    IColumn
{
    private readonly ColumnCore _core = new(tableAlias, columnName);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
