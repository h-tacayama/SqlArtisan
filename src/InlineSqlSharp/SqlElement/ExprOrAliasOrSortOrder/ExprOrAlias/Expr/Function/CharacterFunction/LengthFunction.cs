namespace InlineSqlSharp;

public sealed class LengthFunction(CharacterExpr source) : NumericExpr
{
	private readonly UnaryFunctionCore _core = new(Keywords.LENGTH, source);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
