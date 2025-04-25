namespace InlineSqlSharp;

public sealed class AvgFunctionWithDistinct : AbstractExpr
{
    private readonly UnaryFunctionWithDistinct _core;

    internal AvgFunctionWithDistinct(Distinct distinct, AbstractExpr expr)
    {
        _core = new(Keywords.AVG, distinct, expr);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
