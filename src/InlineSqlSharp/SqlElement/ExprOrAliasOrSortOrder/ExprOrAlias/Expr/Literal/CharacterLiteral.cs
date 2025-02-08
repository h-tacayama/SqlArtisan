namespace InlineSqlSharp;

public sealed class CharacterLiteral(string value) : CharacterExpr, ILiteral
{
	private readonly string _value = value;

	public override void FormatSql(ref SqlBuildingBuffer buffer) =>
		buffer.AppendFormat("'{0}'", _value.Replace("'", "''"));
}
