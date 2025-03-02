namespace InlineSqlSharp;

public sealed class RpadFunction : NumericExpr
{
	private readonly PadFunctionCore _core;

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);

	internal static RpadFunction Of(
		CharacterExpr source,
		NumericExpr length) =>
		new(source, length, null);


	internal static RpadFunction Of(
		CharacterExpr source,
		NumericExpr length,
		CharacterExpr pad) =>
		new(source, length, pad);

	private RpadFunction(
		CharacterExpr source,
		NumericExpr length,
		CharacterExpr? padding)
	{
		_core = new(Keywords.RPAD, source, length, padding);
	}
}
