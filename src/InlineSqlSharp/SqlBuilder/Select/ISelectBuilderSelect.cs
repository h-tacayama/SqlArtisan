namespace InlineSqlSharp;

public interface ISelectBuilderSelect : ISqlBuilder, ISubquery, ISetOperator
{
	ISelectBuilderFrom FROM(params ITableReference[] tables);
}
