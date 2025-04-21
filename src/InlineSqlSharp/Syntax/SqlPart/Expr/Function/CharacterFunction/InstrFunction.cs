namespace InlineSqlSharp;

public sealed class InstrFunction(
    AbstractExpr source,
    AbstractExpr substring,
    AbstractExpr? position = null,
    AbstractExpr? occurrence = null) : AbstractExpr
{
    private readonly VariadicFunctionCore _core =
        new(Keywords.INSTR, source, substring, position, occurrence);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
