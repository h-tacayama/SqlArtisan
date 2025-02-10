namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	public static ISelectBuilderSelect SELECT(params IExprOrAlias[] selectList) =>
		new SelectBuilder(new SelectClause(selectList));
}