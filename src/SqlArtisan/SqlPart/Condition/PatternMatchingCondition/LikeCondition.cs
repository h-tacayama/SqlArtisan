using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// A <c>expr LIKE pattern</c> predicate, from <c>expr.Like(...)</c>; add an escape
/// character with <see cref="Escape(char)"/>.
/// </summary>
public sealed class LikeCondition : SqlCondition
{
    private readonly SqlExpression _leftSide;
    private readonly SqlExpression _rightSide;

    internal LikeCondition(SqlExpression leftSide, SqlExpression rightSide)
    {
        _leftSide = leftSide;
        _rightSide = rightSide;
    }

    /// <summary>
    /// Adds an escape character: <c>expr LIKE pattern ESCAPE 'c'</c>.
    /// </summary>
    /// <param name="escapeChar">The character that makes the next <c>%</c> or <c>_</c> literal.</param>
    // Inlined as a string literal: ESCAPE takes a constant char fixed at the call
    // site (ADR 0004). NotLikeCondition.Escape is the same construct.
    public SqlCondition Escape(char escapeChar) =>
        new LikeEscapeCondition(
            _leftSide,
            PositionBoundOperand.ThrowIfQuantifiedPattern(_rightSide, Keywords.Like),
            escapeChar);

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .EncloseInSpaces(Keywords.Like)
        .Append(_rightSide);
}
