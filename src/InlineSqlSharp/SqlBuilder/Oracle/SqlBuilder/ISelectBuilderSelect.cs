namespace InlineSqlSharp.Oracle;

public interface ISelectBuilderSelect : ISqlBuilder
{
	ISelectBuilderFrom FROM(
		ITableReference primaryTable,
		params ITableReference[] secondaryTables);
}
