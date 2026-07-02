namespace SqlArtisan.Internal;

public sealed class NotLikeCondition : SqlCondition
{
    private readonly SqlExpression _leftSide;
    private readonly SqlExpression _rightSide;

    internal NotLikeCondition(SqlExpression leftSide, SqlExpression rightSide)
    {
        _leftSide = leftSide;
        _rightSide = rightSide;
    }

    // The escape character is emitted as an inline string literal, not a bind
    // parameter: MySQL rejects a parameter marker after ESCAPE.
    public NotLikeEscapeCondition Escape(char escapeChar) =>
        new(_leftSide, _rightSide, escapeChar);

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .EncloseInSpaces($"{Keywords.Not} {Keywords.Like}")
        .Append(_rightSide);
}
