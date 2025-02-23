namespace InlineSqlSharp;

public sealed class CharacterLiteral(string value, bool isEscaped = false)
	: CharacterExpr, ILiteral
{
	private readonly string _value = isEscaped ? value : value.Replace("'", "''");

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		buffer.EncloseInSingleQuotes(_value);
}
