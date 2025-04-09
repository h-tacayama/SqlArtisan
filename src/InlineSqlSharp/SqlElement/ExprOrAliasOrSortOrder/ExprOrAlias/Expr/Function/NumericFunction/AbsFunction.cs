namespace InlineSqlSharp;

public sealed class AbsFunction(NumericExpr expr) : NumericExpr
{
    private readonly UnaryFunctionCore _core = new(Keywords.ABS, expr);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
