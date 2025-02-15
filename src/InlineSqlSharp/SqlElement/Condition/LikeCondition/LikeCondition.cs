namespace InlineSqlSharp;

public sealed class LikeCondition(IExpr leftSide, IExpr rightSide) : ICondition
{
	private readonly LikeConditionCore _core = new(false, leftSide, rightSide);

	public void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
