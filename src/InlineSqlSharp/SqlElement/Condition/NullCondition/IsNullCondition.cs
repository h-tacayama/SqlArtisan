namespace InlineSqlSharp;

public sealed class IsNullCondition(IExpr leftSide) : ICondition
{
	private readonly IExpr _leftSide = leftSide;

	public void FormatSql(ref SqlBuildingBuffer buffer) =>
		buffer.FormatSql(_leftSide)
			.AppendFormat(" {0} {1}", Keywords.IS, Keywords.NULL);
}
