namespace SqlArtisan.Internal;

public sealed class MinFunction : AggregateFunction
{
    private readonly SqlPart _expr;

    internal MinFunction(SqlExpression expr)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Min)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
