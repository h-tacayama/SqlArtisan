namespace SqlArtisan.Internal;

public sealed class InstrFunction : SqlExpression
{
    private readonly VariadicFunctionCore _core;

    internal InstrFunction(
        SqlExpression source,
        SqlExpression substring,
        SqlExpression? position = null,
        SqlExpression? occurrence = null)
    {
        _core = new(Keywords.Instr, source, substring, position, occurrence);
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        _core.Format(buffer);
}
