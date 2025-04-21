namespace InlineSqlSharp;

public sealed class LengthBFunction(AbstractExpr source) : AbstractExpr
{
    private readonly UnaryFunctionCore _core = new(Keywords.LENGTHB, source);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
