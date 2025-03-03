namespace InlineSqlSharp;

public sealed class RpadFunction(
	CharacterExpr source,
	NumericExpr length,
	CharacterExpr? padding = null) : NumericExpr
{
	private readonly VariadicFunctionCore _core =
		new(Keywords.RPAD, source, length, padding);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
