namespace InlineSqlSharp;

public interface ISelectBuilderSetOperator : ISqlBuilder
{
	ISelectBuilderSelect SELECT(params IExprOrAlias[] selectList);

	ISelectBuilderSelect SELECT(
		AllOrDistinct allOrDistinct,
		params IExprOrAlias[] selectList);

	ISelectBuilderSelect SELECT(Hints hints, params IExprOrAlias[] selectList);

	ISelectBuilderSelect SELECT(
		Hints hints,
		AllOrDistinct allOrDistinct,
		params IExprOrAlias[] selectList);
}
