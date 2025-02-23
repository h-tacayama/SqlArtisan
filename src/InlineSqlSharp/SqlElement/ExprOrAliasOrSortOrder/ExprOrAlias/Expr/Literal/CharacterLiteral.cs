namespace InlineSqlSharp;

public sealed class CharacterLiteral(string value, bool isEscaped = false)
	: CharacterExpr, ILiteral
{
	private readonly string _value = value;
	private readonly bool _isEscaped = isEscaped;

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		buffer.EncloseInSingleQuotes(_value, _isEscaped);
}
