namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	public static IUpdateBuilderUpdate UPDATE(Table table)
		=> new UpdateBuilder(new UpdateClause(table));

	public static UpperFunction UPPER(CharacterExpr source) => new(source);
}
