namespace SqlArtisan.Internal;

public sealed class LikeEscapeCondition : SqlCondition
{
    private readonly SqlExpression _leftSide;
    private readonly SqlExpression _rightSide;
    private readonly char _escapeChar;

    internal LikeEscapeCondition(SqlExpression leftSide, SqlExpression rightSide, char escapeChar)
    {
        _leftSide = leftSide;
        _rightSide = rightSide;
        _escapeChar = escapeChar;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .EncloseInSpaces(Keywords.Like)
        .Append(_rightSide)
        .EncloseInSpaces(Keywords.Escape)
        .AppendStringLiteral(_escapeChar);
}
