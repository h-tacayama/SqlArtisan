namespace InlineSqlSharp;

public sealed class ToDateFunction(
    AbstractExpr text,
    AbstractExpr format) : AbstractExpr
{
    private readonly BinaryFunctionCore _core =
        new(Keywords.TO_DATE, text, format);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
