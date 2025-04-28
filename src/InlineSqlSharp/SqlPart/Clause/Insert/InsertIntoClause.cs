namespace InlineSqlSharp;

internal sealed class InsertIntoClause(AbstractTable table) : AbstractSqlPart
{
    private readonly AbstractTable _table = table;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Insert} {Keywords.Into} ")
        .Append(_table);
}
