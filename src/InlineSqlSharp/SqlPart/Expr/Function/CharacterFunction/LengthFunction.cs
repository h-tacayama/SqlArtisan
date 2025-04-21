namespace InlineSqlSharp;

public sealed class LengthFunction : AbstractExpr
{
    private readonly UnaryFunctionCore _core;

    internal LengthFunction(AbstractExpr source)
    {
        _core = new(Keywords.LENGTH, source);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
