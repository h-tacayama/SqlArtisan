namespace InlineSqlSharp;

public sealed class InsertIntoClause(Table table) : ISqlElement
{
	private readonly Table _table = table;

	public void FormatSql(SqlBuildingBuffer buffer) => buffer
		.AppendSpace(Keywords.INSERT)
		.AppendLine(Keywords.INTO)
		.Append(_table);
}
