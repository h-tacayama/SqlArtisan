namespace SqlArtisan;

internal sealed class InsertSelectClause(DbTableBase table, DbColumn[] columns) :
    SqlPart
{
    private readonly DbTableBase _table = table;
    private readonly DbColumn[] _columns = columns;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Insert} {Keywords.Into} ")
        .AppendSpace(_table)
        .OpenParenthesis()
        .AppendCsv(_columns)
        .CloseParenthesis();
}
