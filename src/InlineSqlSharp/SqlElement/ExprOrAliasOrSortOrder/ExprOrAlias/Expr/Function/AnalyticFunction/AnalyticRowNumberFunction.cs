namespace InlineSqlSharp;

public sealed class AnalyticRowNumberFunction() : AnalyticFunction
{
	public override void FormatSql(SqlBuildingBuffer buffer) => buffer
		.Append(Keywords.ROW_NUMBER)
		.OpenParenthesis()
		.CloseParenthesis();
}
