namespace InlineSqlSharp;

public sealed class GreatestFunction(AbstractExpr[] expressions) : AbstractExpr
{
    private readonly VariadicFunctionCore _core =
        new(Keywords.GREATEST, expressions);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
