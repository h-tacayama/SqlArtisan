namespace SqlArtisan;

public sealed class DeleteClause : AbstractSqlPart
{
    private readonly AbstractTable _table;

    internal DeleteClause(AbstractTable table)
    {
        _table = table;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Delete} {Keywords.From} ")
        .Append(_table);
}
