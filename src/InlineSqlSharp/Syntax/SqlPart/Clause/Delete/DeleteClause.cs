namespace InlineSqlSharp;

public sealed class DeleteClause(AbstractTable table) : AbstractSqlPart
{
    private readonly AbstractTable _table = table;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.DELETE)
        .AppendSpace(Keywords.FROM)
        .Append(_table);
}
