using static SqlArtisan.Internal.ExpressionResolver;

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

    public LikeEscapeCondition Escape(object escapeChar) =>
        new(_leftSide, _rightSide, Resolve(escapeChar));

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .Append($" {Keywords.Like} ")
        .Append(_rightSide);
}
