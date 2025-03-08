namespace InlineSqlSharp;

public sealed class CharacterDecodeFunction<TSearchExpr>(
	TSearchExpr expr,
	(TSearchExpr, CharacterExpr)[] searchResultPairs,
	CharacterExpr @default)
	: CharacterExpr
	where TSearchExpr : IDataExpr
{
	private readonly DecodeFunctionCore<TSearchExpr, CharacterExpr> _core =
		new(expr, searchResultPairs, @default);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
