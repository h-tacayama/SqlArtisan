namespace InlineSqlSharp;

public sealed class UpperFunction : AbstractExpr
{
    private readonly UnaryFunctionCore _core;

    internal UpperFunction(AbstractExpr source)
    {
        _core = new(Keywords.UPPER, source);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
