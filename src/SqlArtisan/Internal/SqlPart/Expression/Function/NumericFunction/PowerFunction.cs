namespace SqlArtisan.Internal;

public sealed class PowerFunction : SqlExpression
{
    private readonly SqlExpression _base;
    private readonly SqlExpression _exponent;

    internal PowerFunction(SqlExpression @base, SqlExpression exponent)
    {
        _base = @base;
        _exponent = exponent;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Power)
        .OpenParenthesis()
        .Append(_base)
        .PrependComma(_exponent)
        .CloseParenthesis();
}
