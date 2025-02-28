namespace InlineSqlSharp;

public sealed class RpadFunction : NumericExpr
{
	private readonly PadFunctionCore _core;

	public static RpadFunction Of(
		CharacterExpr source,
		NumericExpr length) =>
		new(source, length, null);


	public static RpadFunction Of(
		CharacterExpr source,
		NumericExpr length,
		CharacterExpr pad) =>
		new(source, length, pad);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);

	private RpadFunction(
		CharacterExpr source,
		NumericExpr length,
		CharacterExpr? padding)
	{
		_core = new(Keywords.RPAD, source, length, padding);
	}
}
