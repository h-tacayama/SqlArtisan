namespace InlineSqlSharp;

public sealed class LpadFunction : NumericExpr
{
	private readonly PadFunctionCore _core;

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);

	internal static LpadFunction Of(
		CharacterExpr source,
		NumericExpr length) =>
		new(source, length, null);


	internal static LpadFunction Of(
		CharacterExpr source,
		NumericExpr length,
		CharacterExpr pad) =>
		new(source, length, pad);

	private LpadFunction(
		CharacterExpr source,
		NumericExpr length,
		CharacterExpr? padding)
	{
		_core = new(Keywords.LPAD, source, length, padding);
	}
}
