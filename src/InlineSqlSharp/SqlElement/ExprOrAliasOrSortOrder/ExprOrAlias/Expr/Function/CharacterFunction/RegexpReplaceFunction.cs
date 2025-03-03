namespace InlineSqlSharp;

public sealed class RegexpReplaceFunction(
	CharacterExpr source,
	CharacterExpr pattern,
	CharacterExpr replacement,
	NumericExpr? position = null,
	NumericExpr? occurrence = null,
	RegexpOptions? options = null) : CharacterExpr
{
	private readonly VariadicFunctionCore _core = new(
		Keywords.REGEXP_REPLACE,
		source,
		pattern,
		replacement,
		position,
		occurrence,
		options?.ToValue());

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
