namespace InlineSqlSharp.Oracle;

public static partial class SqlWordbook
{
	public static ISelectBuilderSelect SELECT(
		IExprOrAlias primaryItem,
		params IExprOrAlias[] secondaryItems) =>
		new SelectBuilder(new SelectClause(primaryItem, secondaryItems));
}