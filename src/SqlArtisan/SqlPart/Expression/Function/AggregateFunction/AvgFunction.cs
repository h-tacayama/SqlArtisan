namespace SqlArtisan;

public sealed class AvgFunction : SqlExpression
{
    private readonly DistinctKeyword? _distinct;
    private readonly SqlPart _expr;

    internal AvgFunction(SqlExpression expr)
    {
        _distinct = null;
        _expr = expr;
    }

    internal AvgFunction(DistinctKeyword distinct, SqlExpression expr)
    {
        _distinct = distinct;
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Avg)
        .OpenParenthesis()
        .AppendSpaceIfNotNull(_distinct)
        .Append(_expr)
        .CloseParenthesis();
}
