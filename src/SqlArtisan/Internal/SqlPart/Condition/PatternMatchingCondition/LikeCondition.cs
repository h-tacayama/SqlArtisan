namespace SqlArtisan.Internal;

public sealed class LikeCondition : SqlCondition
{
    private readonly SqlExpression _leftSide;
    private readonly SqlExpression _rightSide;

    internal LikeCondition(SqlExpression leftSide, SqlExpression rightSide)
    {
        _leftSide = leftSide;
        _rightSide = rightSide;
    }

    // The escape character is emitted as an inline string literal, not a bind
    // parameter: MySQL rejects a parameter marker after ESCAPE.
    public LikeEscapeCondition Escape(char escapeChar) =>
        new(_leftSide, _rightSide, escapeChar);

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .Append($" {Keywords.Like} ")
        .Append(_rightSide);
}
