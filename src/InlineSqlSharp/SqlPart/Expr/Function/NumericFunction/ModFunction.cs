namespace InlineSqlSharp;

public sealed class ModFunction(
    AbstractExpr dividend,
    AbstractExpr divisor) : AbstractExpr
{
    private readonly BinaryFunctionCore _core =
        new(Keywords.MOD, dividend, divisor);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
