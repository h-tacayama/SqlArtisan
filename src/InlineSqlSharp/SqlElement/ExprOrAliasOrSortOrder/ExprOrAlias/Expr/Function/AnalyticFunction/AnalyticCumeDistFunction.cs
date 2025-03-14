namespace InlineSqlSharp;

public sealed class AnalyticCumeDistFunction() : AnalyticFunction
{
	public override void FormatSql(SqlBuildingBuffer buffer) => buffer
		.Append(Keywords.CUME_DIST)
		.OpenParenthesis()
		.CloseParenthesis();
}
