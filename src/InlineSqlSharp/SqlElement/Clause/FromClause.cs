namespace InlineSqlSharp;

public sealed class FromClause(
	ITableReference primaryTable,
	ITableReference[] secondaryTables) : ISqlElement
{
	private readonly ITableReference _primaryTable = primaryTable;
	private readonly ITableReference[] _secondaryTables = secondaryTables;

	public void FormatSql(ref SqlBuildingBuffer buffer)
	{
		buffer.AppendLine(Keywords.FROM);
		_primaryTable.FormatSql(ref buffer);

		for (int i = 0; i < _secondaryTables.Length; i++)
		{
			buffer.AppendLine();
			buffer.Append(", ");
			_secondaryTables[i].FormatSql(ref buffer);
		}
	}
}