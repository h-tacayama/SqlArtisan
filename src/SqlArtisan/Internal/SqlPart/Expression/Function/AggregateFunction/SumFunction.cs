namespace SqlArtisan.Internal;

public sealed class SumFunction : SqlExpression
{
    private readonly DistinctKeyword? _distinct;
    private readonly SqlPart _expr;

    internal SumFunction(SqlExpression expr)
    {
        _distinct = null;
        _expr = expr;
    }

    internal SumFunction(DistinctKeyword distinct, SqlExpression expr)
    {
        _distinct = distinct;
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Sum)
        .OpenParenthesis()
        .AppendSpaceIfNotNull(_distinct)
        .Append(_expr)
        .CloseParenthesis();
}
