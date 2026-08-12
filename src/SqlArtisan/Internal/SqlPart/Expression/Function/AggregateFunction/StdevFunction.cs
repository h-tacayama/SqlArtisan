namespace SqlArtisan.Internal;

public sealed class StdevFunction : UnfilteredAggregateFunction
{
    private readonly SqlPart _expr;

    internal StdevFunction(SqlExpression expr)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Stdev)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
