namespace InlineSqlSharp;

public abstract class Table : ITableReference
{
	private readonly string _tableName;
	private readonly string _alias;

	public Table(string alias)
	{
		_tableName = GetType().Name;
		_alias = alias;
	}

	public Table(string tableName, string alias)
	{
		_tableName = tableName;
		_alias = alias;
	}

	public void FormatSql(SqlBuildingBuffer buffer) => buffer
		.AppendSpace(_tableName)
		.Append(_alias);
}
