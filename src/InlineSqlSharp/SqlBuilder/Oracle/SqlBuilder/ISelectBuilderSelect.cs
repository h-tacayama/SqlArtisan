namespace InlineSqlSharp.Oracle;

public interface ISelectBuilderSelect : ISqlBuilder, ISubqueryBuilder
{
	ISelectBuilderFrom FROM(
		ITableReference primaryTable,
		params ITableReference[] secondaryTables);
}
