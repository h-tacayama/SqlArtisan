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

    // Inlined as a string literal, never bound: MySQL rejects a parameter marker
    // after ESCAPE (ADR 0004). NotLikeCondition.Escape is the same construct.
    public LikeEscapeCondition Escape(char escapeChar) =>
        new(_leftSide, _rightSide, escapeChar);

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .EncloseInSpaces(Keywords.Like)
        .Append(_rightSide);
}
