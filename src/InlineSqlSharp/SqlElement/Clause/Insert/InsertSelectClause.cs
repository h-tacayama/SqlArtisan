namespace InlineSqlSharp;

public sealed class InsertSelectClause(AbstractTable table, IColumn[] columns)
	: ISqlElement
{
	private readonly AbstractTable _table = table;
	private readonly IColumn[] _columns = columns;

	public void FormatSql(SqlBuildingBuffer buffer) => buffer
		.AppendSpace(Keywords.INSERT)
		.AppendSpace(Keywords.INTO)
		.AppendSpace(_table)
		.OpenParenthesis()
		.AppendCsv(_columns)
		.CloseParenthesis();
}
