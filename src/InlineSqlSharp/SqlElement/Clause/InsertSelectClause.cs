namespace InlineSqlSharp;

public sealed class InsertSelectClause(Table table, IColumn[] columns)
	: ISqlElement
{
	private readonly Table _table = table;
	private readonly IColumn[] _columns = columns;

	public void FormatSql(SqlBuildingBuffer buffer) => buffer
		.AppendSpace(Keywords.INSERT)
		.AppendLine(Keywords.INTO)
		.AppendLine(_table)
		.OpenParenthesisBeforeLine()
		.AppendCsvLines(_columns)
		.CloseParenthesisAfterLine();
}
