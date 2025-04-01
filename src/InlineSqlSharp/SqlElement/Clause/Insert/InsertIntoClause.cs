namespace InlineSqlSharp;

public sealed class InsertIntoClause(AbstractTable table) : ISqlElement
{
	private readonly AbstractTable _table = table;

	public void FormatSql(SqlBuildingBuffer buffer) => buffer
		.AppendSpace(Keywords.INSERT)
		.AppendSpace(Keywords.INTO)
		.Append(_table);
}
