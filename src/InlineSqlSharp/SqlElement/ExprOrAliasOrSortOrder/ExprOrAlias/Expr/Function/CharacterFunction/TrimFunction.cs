namespace InlineSqlSharp;

public sealed class TrimFunction(
	CharacterExpr source,
	CharacterExpr? trimChar = null) : CharacterExpr
{
	private readonly CharacterExpr _source = source;
	private readonly CharacterExpr? _trimChar = trimChar;

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
}
