namespace InlineSqlSharp;

public sealed class AnalyticPercentRankFunction() : AnalyticFunction
{
	public override void FormatSql(SqlBuildingBuffer buffer) => buffer
		.Append(Keywords.PERCENT_RANK)
		.OpenParenthesis()
		.CloseParenthesis();
}
