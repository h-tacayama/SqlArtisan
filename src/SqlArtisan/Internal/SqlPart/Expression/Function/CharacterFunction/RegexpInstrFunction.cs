namespace SqlArtisan.Internal;

public sealed class RegexpInstrFunction : SqlExpression
{
    private readonly VariadicFunctionCore _core;

    internal RegexpInstrFunction(
        SqlExpression source,
        SqlExpression pattern,
        SqlExpression? position = null,
        SqlExpression? occurrence = null,
        SqlExpression? returnOption = null,
        RegexpOptions? options = null,
        SqlExpression? subPatternPos = null)
    {
        _core = new(
            Keywords.RegexpInstr,
            source,
            pattern,
            position,
            occurrence,
            returnOption,
            options?.ToValue(),
            subPatternPos);
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        _core.Format(buffer);
}
