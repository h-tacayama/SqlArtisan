namespace InlineSqlSharp;

public sealed class AnalyticDenseRankFunction() : AnalyticFunction
{
	public override void FormatSql(SqlBuildingBuffer buffer) => buffer
		.Append(Keywords.DENSE_RANK)
		.OpenParenthesis()
		.CloseParenthesis();
}
