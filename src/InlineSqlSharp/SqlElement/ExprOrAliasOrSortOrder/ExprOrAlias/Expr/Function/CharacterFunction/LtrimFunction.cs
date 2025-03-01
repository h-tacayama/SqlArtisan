namespace InlineSqlSharp;

public sealed class LtrimFunction : CharacterExpr
{
	private readonly CharacterExpr _source;
	private readonly CharacterExpr? _trimChars;

	internal static LtrimFunction Of(CharacterExpr source) => new(source, null);

	internal static LtrimFunction Of(
		CharacterExpr source,
		CharacterExpr trimChars) => new(source, trimChars);

	public override void FormatSql(SqlBuildingBuffer buffer) => buffer
		.Append(Keywords.LTRIM)
		.OpenParenthesis()
		.Append(_source)
		.PrependCommaIfNotNull(_trimChars)
		.CloseParenthesis();

	private LtrimFunction(
		CharacterExpr source,
		CharacterExpr? trimChars)
	{
		_source = source;
		_trimChars = trimChars;
	}
}
