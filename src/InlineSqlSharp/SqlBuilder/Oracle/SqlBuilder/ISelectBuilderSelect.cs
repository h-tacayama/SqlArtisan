namespace InlineSqlSharp.Oracle;

public interface ISelectBuilderSelect : ISqlBuilder, ISubqueryBuilder
{
	ISelectBuilderFrom FROM(params ITableReference[] tables);
}
