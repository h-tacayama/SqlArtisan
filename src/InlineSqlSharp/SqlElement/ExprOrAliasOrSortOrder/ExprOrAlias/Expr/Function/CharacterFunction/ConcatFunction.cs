namespace InlineSqlSharp;

public sealed class ConcatFunction(
	CharacterExpr primary,
	CharacterExpr secondary,
	CharacterExpr[] others) : CharacterExpr
{
	private readonly CharacterExpr[] _values = [primary, secondary, .. others];

	public override void FormatSql(SqlBuildingBuffer buffer) => buffer
		.Append(Keywords.CONCAT)
		.OpenParenthesis()
		.AppendCsv(_values)
		.CloseParenthesis();
}
