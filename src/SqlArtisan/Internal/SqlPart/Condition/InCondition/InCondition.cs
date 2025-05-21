namespace SqlArtisan.Internal;

public sealed class InCondition : SqlCondition
{
    private readonly SqlExpression _leftSide;
    private readonly SqlExpression[] _expressions;

    internal InCondition(SqlExpression leftSide, SqlExpression[] expressions)
    {
        _leftSide = leftSide;
        _expressions = expressions;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .Append($" {Keywords.In} ")
        .OpenParenthesis()
        .AppendCsv(_expressions)
        .CloseParenthesis();
}
