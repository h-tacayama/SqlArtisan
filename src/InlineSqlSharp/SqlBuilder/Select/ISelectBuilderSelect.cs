namespace InlineSqlSharp;

public interface ISelectBuilderSelect : ISqlBuilder, ISubquery
{
	ISelectBuilderFrom FROM(params ITableReference[] tables);
}
