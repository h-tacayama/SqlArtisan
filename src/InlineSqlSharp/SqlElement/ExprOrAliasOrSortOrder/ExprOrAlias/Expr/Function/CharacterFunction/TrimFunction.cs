namespace InlineSqlSharp;

public sealed class TrimFunction : CharacterExpr
{
	private readonly CharacterExpr _source;
	private readonly CharacterExpr? _trimChar;

	public override void FormatSql(SqlBuildingBuffer buffer)
	{
		buffer.Append(Keywords.TRIM)
			.OpenParenthesis();

		if (_trimChar is not null)
		{
			buffer.AppendSpace(Keywords.BOTH)
				.AppendSpace(_trimChar)
				.AppendSpace(Keywords.FROM);
		}

		buffer.Append(_source)
			.CloseParenthesis();
	}

	internal static TrimFunction Of(CharacterExpr source) => new(source, null);

	internal static TrimFunction Of(
		CharacterExpr source,
		CharacterExpr trimChar) => new(source, trimChar);

	private TrimFunction(
		CharacterExpr source,
		CharacterExpr? trimChar)
	{
		_source = source;
		_trimChar = trimChar;
	}
}
