namespace InlineSqlSharp;

public sealed class CountFunction(IExpr expr) : AggregateFunction
{
	private readonly IExpr _expr = expr;

	public override void FormatSql(ref SqlBuildingBuffer buffer) =>
		buffer.AppendUnaryFunction(Keywords.COUNT, _expr);
}
