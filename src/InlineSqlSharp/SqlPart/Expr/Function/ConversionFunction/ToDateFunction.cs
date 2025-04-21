namespace InlineSqlSharp;

public sealed class ToDateFunction : AbstractExpr
{
    private readonly BinaryFunctionCore _core;

    internal ToDateFunction(AbstractExpr text, AbstractExpr format)
    {
        _core = new(Keywords.TO_DATE, text, format);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
