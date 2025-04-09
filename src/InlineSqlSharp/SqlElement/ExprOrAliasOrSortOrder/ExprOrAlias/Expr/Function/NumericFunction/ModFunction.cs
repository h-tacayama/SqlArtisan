namespace InlineSqlSharp;

public sealed class ModFunction(
    NumericExpr dividend,
    NumericExpr divisor) : NumericExpr
{
    private readonly BinaryFunctionCore _core = new(Keywords.MOD, dividend, divisor);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
