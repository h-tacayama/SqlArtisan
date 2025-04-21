namespace InlineSqlSharp;

internal sealed class UpdateClause(AbstractTable table) : AbstractSqlPart
{
    private readonly AbstractTable _table = table;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.UPDATE)
        .Append(_table);
}
