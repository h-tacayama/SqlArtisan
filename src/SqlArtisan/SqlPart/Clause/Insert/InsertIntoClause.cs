namespace SqlArtisan;

internal sealed class InsertIntoClause(DbTableBase table) : SqlPart
{
    private readonly DbTableBase _table = table;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Insert} {Keywords.Into} ")
        .Append(_table);
}
