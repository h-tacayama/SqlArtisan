namespace SqlArtisan.Internal;

public sealed class StddevFunction : UnfilteredAggregateFunction
{
    private readonly SqlPart _expr;

    internal StddevFunction(SqlExpression expr)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Stddev)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
