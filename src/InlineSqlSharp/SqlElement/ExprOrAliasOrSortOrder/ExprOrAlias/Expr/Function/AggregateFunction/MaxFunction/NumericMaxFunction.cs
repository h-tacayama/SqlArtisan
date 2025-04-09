namespace InlineSqlSharp;

public sealed class NumericMaxFunction(NumericExpr expr) : NumericExpr
{
    readonly UnaryFunctionCore _core = new(Keywords.MAX, expr);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
