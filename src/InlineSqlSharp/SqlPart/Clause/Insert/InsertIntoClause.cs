namespace InlineSqlSharp;

internal sealed class InsertIntoClause(AbstractTable table) : AbstractSqlPart
{
    private readonly AbstractTable _table = table;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.Insert)
        .AppendSpace(Keywords.Into)
        .Append(_table);
}
