namespace InlineSqlSharp;

public sealed class DeleteClause : AbstractSqlPart
{
    private readonly AbstractTable _table;

    internal DeleteClause(AbstractTable table)
    {
        _table = table;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.Delete)
        .AppendSpace(Keywords.From)
        .Append(_table);
}
