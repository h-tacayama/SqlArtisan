namespace InlineSqlSharp.Oracle;

public interface ISelectBuilderSelect : ISqlBuilder, ISubquery
{
	ISelectBuilderFrom FROM(params ITableReference[] tables);
}
