namespace InlineSqlSharp;

public sealed class SumFunctionWithDistinct : AbstractExpr
{
    private readonly UnaryFunctionWithDistinct _core;

    internal SumFunctionWithDistinct(Distinct distinct, AbstractExpr expr)
    {
        _core = new(Keywords.SUM, distinct, expr);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
