namespace SqlArtisan.Internal;

public sealed class LikeEscapeCondition : SqlCondition
{
    private readonly SqlExpression _leftSide;
    private readonly SqlExpression _rightSide;
    private readonly SqlExpression _escapeChar;

    internal LikeEscapeCondition(
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
        .Append($" {Keywords.Like} ")
        .Append(_rightSide)
        .Append($" {Keywords.Escape} ")
        .Append(_escapeChar);
}
