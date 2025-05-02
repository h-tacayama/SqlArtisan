namespace SqlArtisan;

public sealed class NotInCondition : SqlCondition
{
    private readonly SqlExpression _leftSide;
    private readonly SqlExpression[] _expressions;

    internal NotInCondition(SqlExpression leftSide, SqlExpression[] expressions)
    {
        _leftSide = leftSide;
        _expressions = expressions;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .Append($" {Keywords.Not} {Keywords.In} ")
        .OpenParenthesis()
        .AppendCsv(_expressions)
        .CloseParenthesis();
}
