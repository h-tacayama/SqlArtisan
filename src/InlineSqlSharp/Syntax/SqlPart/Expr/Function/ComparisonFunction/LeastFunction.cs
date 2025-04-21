namespace InlineSqlSharp;

public sealed class LeastFunction(AbstractExpr[] expressions) : AbstractExpr
{
    private readonly VariadicFunctionCore _core =
        new(Keywords.LEAST, expressions);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
