namespace InlineSqlSharp;

public sealed class GreatestFunction : AbstractExpr
{
    private readonly VariadicFunctionCore _core;

    internal GreatestFunction(AbstractExpr[] expressions)
    {
        _core = new(Keywords.GREATEST, expressions);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
