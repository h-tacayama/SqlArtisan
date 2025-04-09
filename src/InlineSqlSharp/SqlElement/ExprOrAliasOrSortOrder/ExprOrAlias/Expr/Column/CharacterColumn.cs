namespace InlineSqlSharp;

public sealed class CharacterColumn(string tableAlias, string columnName) :
    CharacterExpr,
    IColumn
{
    private readonly ColumnCore _core = new(tableAlias, columnName);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
