namespace InlineSqlSharp;

public abstract class Table : ITableReference
{
	private readonly string _tableName;
	private readonly AliasName _alias;

	public Table(AliasName alias)
	{
		_tableName = GetType().Name;
		_alias = alias;
	}

	public Table(string tableName, AliasName alias)
	{
		_tableName = tableName;
		_alias = alias;
	}

	public void FormatSql(ref SqlBuildingBuffer buffer) =>
		buffer.Core.AppendFormat("{0} {1}", _tableName, _alias);
}
