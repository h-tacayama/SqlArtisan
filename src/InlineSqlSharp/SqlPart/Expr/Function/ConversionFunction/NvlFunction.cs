namespace InlineSqlSharp;

public sealed class NvlFunction : AbstractExpr
{
    private readonly BinaryFunctionCore _core;

    internal NvlFunction(AbstractExpr expr1, AbstractExpr expr2)
    {
        _core = new(Keywords.NVL, expr1, expr2);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
