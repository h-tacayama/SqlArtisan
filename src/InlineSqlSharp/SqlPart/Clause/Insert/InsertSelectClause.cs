namespace InlineSqlSharp;

internal sealed class InsertSelectClause(AbstractTable table, Column[] columns) :
    AbstractSqlPart
{
    private readonly AbstractTable _table = table;
    private readonly Column[] _columns = columns;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.Insert)
        .AppendSpace(Keywords.Into)
        .AppendSpace(_table)
        .OpenParenthesis()
        .AppendCsv(_columns)
        .CloseParenthesis();
}
