namespace InlineSqlSharp;

public sealed class LengthFunction(AbstractExpr source) : AbstractExpr
{
    private readonly UnaryFunctionCore _core = new(Keywords.LENGTH, source);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
