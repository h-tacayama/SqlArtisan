namespace InlineSqlSharp;

public sealed class IsNotNullCondition(IExpr leftSide) : ICondition
{
	private readonly IExpr _leftSide = leftSide;

	public void FormatSql(ref SqlBuildingBuffer buffer) => buffer
		.AppendSpace(_leftSide)
		.AppendSpace(Keywords.IS)
		.AppendSpace(Keywords.NOT)
		.Append(Keywords.NULL);
}
