namespace InlineSqlSharp;

public sealed class ModFunction : AbstractExpr
{
    private readonly BinaryFunctionCore _core;

    internal ModFunction(AbstractExpr dividend, AbstractExpr divisor)
    {
        _core = new(Keywords.MOD, dividend, divisor);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
