namespace SqlArtisan.Internal;

public sealed class NotLikeEscapeCondition : SqlCondition
{
    private readonly SqlExpression _leftSide;
    private readonly SqlExpression _rightSide;
    private readonly SqlExpression _escapeChar;

    internal NotLikeEscapeCondition(
        SqlExpression leftSide,
        SqlExpression rightSide,
        SqlExpression escapeChar)
    {
        _leftSide = leftSide;
        _rightSide = rightSide;
        _escapeChar = escapeChar;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .Append($" {Keywords.Not} {Keywords.Like} ")
        .Append(_rightSide)
        .Append($" {Keywords.Escape} ")
        .Append(_escapeChar);
}
