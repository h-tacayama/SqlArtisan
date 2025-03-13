namespace InlineSqlSharp;

public sealed class AnalyticRankFunction() : AnalyticFunction
{
	public override void FormatSql(SqlBuildingBuffer buffer) => buffer
		.Append(Keywords.RANK)
		.OpenParenthesis()
		.CloseParenthesis();
}
