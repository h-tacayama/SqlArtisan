using static SqlArtisan.Internal.ExpressionResolver;

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

    public NotLikeEscapeCondition Escape(object escapeChar) =>
        new(_leftSide, _rightSide, Resolve(escapeChar));

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .Append($" {Keywords.Not} {Keywords.Like} ")
        .Append(_rightSide);
}
