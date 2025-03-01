namespace InlineSqlSharp;

public sealed class RtrimFunction : CharacterExpr
{
	private readonly CharacterExpr _source;
	private readonly CharacterExpr? _trimChars;

	internal static RtrimFunction Of(CharacterExpr source) => new(source, null);

	internal static RtrimFunction Of(
		CharacterExpr source,
		CharacterExpr trimChars) =>
		new(source, trimChars);

	public override void FormatSql(SqlBuildingBuffer buffer) => buffer
		.Append(Keywords.RTRIM)
		.OpenParenthesis()
		.Append(_source)
		.PrependCommaIfNotNull(_trimChars)
		.CloseParenthesis();

	private RtrimFunction(
		CharacterExpr source,
		CharacterExpr? trimChars)
	{
		_source = source;
		_trimChars = trimChars;
	}
}
