namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	public static ISelectBuilderSelect SELECT(params IExprOrAlias[] selectList) =>
		new SelectBuilder(new SelectClause(false, selectList));

	public static ISelectBuilderSelect SELECT_DISTINCT(params IExprOrAlias[] selectList) =>
		new SelectBuilder(new SelectClause(true, selectList));
}