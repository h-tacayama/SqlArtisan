namespace InlineSqlSharp;

public sealed class InstrFunction : AbstractExpr
{
    private readonly VariadicFunctionCore _core;

    internal InstrFunction(
        AbstractExpr source,
        AbstractExpr substring,
        AbstractExpr? position = null,
        AbstractExpr? occurrence = null)
    {
        _core = new(Keywords.Instr, source, substring, position, occurrence);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
