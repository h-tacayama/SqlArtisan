using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// A <c>expr NOT LIKE pattern</c> predicate, from <c>expr.NotLike(...)</c>; add an
/// escape character with <see cref="Escape(char)"/>.
/// </summary>
public sealed class NotLikeCondition : SqlCondition
{
    private readonly SqlExpression _leftSide;
    private readonly SqlExpression _rightSide;

    internal NotLikeCondition(SqlExpression leftSide, SqlExpression rightSide)
    {
        _leftSide = leftSide;
        _rightSide = rightSide;
    }

    /// <summary>
    /// Adds an escape character: <c>expr NOT LIKE pattern ESCAPE 'c'</c>.
    /// </summary>
    /// <param name="escapeChar">The character that makes the next <c>%</c> or <c>_</c> literal.</param>
    public SqlCondition Escape(char escapeChar) =>
        new NotLikeEscapeCondition(
            _leftSide,
            PositionBoundOperand.ThrowIfQuantifiedPattern(
                _rightSide, $"{Keywords.Not} {Keywords.Like}"),
            escapeChar);

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .EncloseInSpaces($"{Keywords.Not} {Keywords.Like}")
        .Append(_rightSide);
}
