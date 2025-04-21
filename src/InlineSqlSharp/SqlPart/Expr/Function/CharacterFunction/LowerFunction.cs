namespace InlineSqlSharp;

public sealed class LowerFunction(AbstractExpr source) : AbstractExpr
{
    private readonly UnaryFunctionCore _core = new(Keywords.LOWER, source);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
