namespace InlineSqlSharp;

internal sealed class DeleteClause(Table table) : ISqlElement
{
	private readonly Table _table = table;

	public void FormatSql(SqlBuildingBuffer buffer) => buffer
		.AppendSpace(Keywords.DELETE)
		.AppendSpace(Keywords.FROM)
		.Append(_table);
}
