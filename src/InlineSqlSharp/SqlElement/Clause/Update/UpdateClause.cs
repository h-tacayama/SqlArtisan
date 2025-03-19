namespace InlineSqlSharp;

internal sealed class UpdateClause(Table table) : ISqlElement
{
	private readonly Table _table = table;

	public void FormatSql(SqlBuildingBuffer buffer) => buffer
		.AppendSpace(Keywords.UPDATE)
		.Append(_table);
}
