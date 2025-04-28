namespace InlineSqlSharp;

internal sealed class UpdateClause(AbstractTable table) : AbstractSqlPart
{
    private readonly AbstractTable _table = table;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Update} ")
        .Append(_table);
}
