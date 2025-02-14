namespace InlineSqlSharp;

public interface ISelectBuilderSetOperator : ISqlBuilder
{
	ISelectBuilderSelect SELECT(params IExprOrAlias[] selectList);

	ISelectBuilderSelect SELECT_DISTINCT(params IExprOrAlias[] selectList);
}
