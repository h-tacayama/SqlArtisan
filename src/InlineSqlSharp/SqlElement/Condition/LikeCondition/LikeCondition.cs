namespace InlineSqlSharp;

public sealed class LikeCondition(IExpr leftSide, IExpr rightSide) : ICondition
{
	private readonly LikeConditionCore _core = new(false, leftSide, rightSide);

	public void FormatSql(ref SqlBuildingBuffer buffer) =>
		_core.FormatSql(ref buffer);
}
