namespace InlineSqlSharp;

public sealed class LengthBFunction : AbstractExpr
{
    private readonly UnaryFunctionCore _core;

    internal LengthBFunction(AbstractExpr source)
    {
        _core = new(Keywords.LENGTHB, source);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
