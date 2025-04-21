namespace InlineSqlSharp;

public sealed class UpperFunction(AbstractExpr source) : AbstractExpr
{
    private readonly UnaryFunctionCore _core = new(Keywords.UPPER, source);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
