namespace InlineSqlSharp;

public sealed class ConcatFunction : AbstractExpr
{
    private readonly VariadicFunctionCore _core;

    internal ConcatFunction(
        AbstractExpr primary,
        AbstractExpr secondary,
        AbstractExpr[] others)
    {
        _core = new(Keywords.Concat, [primary, secondary, .. others]);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
