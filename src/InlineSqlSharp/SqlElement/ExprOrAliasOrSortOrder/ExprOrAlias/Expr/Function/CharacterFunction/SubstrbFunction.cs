namespace InlineSqlSharp;

public sealed class SubstrbFunction(
	CharacterExpr source,
	NumericExpr position,
	NumericExpr? length = null) : CharacterExpr
{
	private readonly VariadicFunctionCore _core =
		new(Keywords.SUBSTRB, source, position, length);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
