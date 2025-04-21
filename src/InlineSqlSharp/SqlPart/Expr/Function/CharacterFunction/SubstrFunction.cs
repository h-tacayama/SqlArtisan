namespace InlineSqlSharp;

public sealed class SubstrFunction(
    AbstractExpr source,
    AbstractExpr position,
    AbstractExpr? length = null) : AbstractExpr
{
    private readonly VariadicFunctionCore _core =
        new(Keywords.SUBSTR, source, position, length);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
