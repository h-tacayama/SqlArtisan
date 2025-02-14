namespace InlineSqlSharp;

internal sealed class ColumnCore(string tableAlias, string columnName)
{
	private readonly string _tableAlias = tableAlias;
	private readonly string _columnName = columnName;

	internal void FormatSql(ref SqlBuildingBuffer buffer) => buffer
		.Append(_tableAlias)
		.Append(".")
		.Append(_columnName);
}
