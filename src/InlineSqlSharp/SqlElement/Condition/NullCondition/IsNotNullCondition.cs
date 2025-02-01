namespace InlineSqlSharp;

public sealed class IsNotNullCondition(IExpr leftSide) : ICondition
{
	private readonly IExpr _leftSide = leftSide;

	public void FormatSql(ref SqlBuildingBuffer buffer)
	{
		_leftSide.FormatSql(ref buffer);
		buffer.AppendFormat(" {0} {1} {2}", Keywords.IS, Keywords.NOT, Keywords.NULL);
	}
}
