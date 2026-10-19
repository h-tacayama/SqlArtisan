namespace SqlArtisan.Internal;

/// <summary>
/// An <c>ANY</c> / <c>ALL</c> / <c>SOME (array)</c> operand, valid in one slot alone, so
/// it is no <see cref="SqlExpression"/> (#666).
/// </summary>
/// <remarks>Taken on the right of a comparison, an unescaped <c>Like</c> / <c>NotLike</c> and
/// <c>ArrayContains</c> / <c>ArrayContainedBy</c> / <c>ArrayOverlaps</c>, and as
/// <c>JsonbContains</c>' value, <c>JsonbExists</c>' key and <c>TsMatch</c>'s query.</remarks>
public sealed class QuantifiedExpression : SqlPart, IIncompleteExpression
{
    private readonly string _keyword;
    private readonly SqlExpression _operand;

    internal QuantifiedExpression(string keyword, SqlExpression operand)
    {
        _keyword = keyword;
        _operand = operand;
    }

    string IIncompleteExpression.IncompleteMessage =>
        QuantifiedSubquery.IncompleteMessageFor(_keyword);

    /// <summary>
    /// Not supported: a string beside an <c>ANY</c> / <c>ALL</c> / <c>SOME</c> operand,
    /// which completes only a predicate's right side; it would otherwise compile as C#
    /// concatenation.
    /// </summary>
    /// <param name="leftSide">The string.</param>
    /// <param name="rightSide">The quantified operand.</param>
    /// <returns>Never returns; the call does not compile.</returns>
    [Obsolete(QuantifiedSubquery.StringBesideQuantifiedMessage, error: true)]
    public static AdditionOperator operator +(string leftSide, QuantifiedExpression rightSide) =>
        throw new InvalidOperationException(QuantifiedSubquery.StringBesideQuantifiedMessage);

    /// <inheritdoc cref="op_Addition(string, QuantifiedExpression)"/>
    /// <param name="leftSide">The quantified operand.</param>
    /// <param name="rightSide">The string.</param>
    /// <returns>Never returns; the call does not compile.</returns>
    [Obsolete(QuantifiedSubquery.StringBesideQuantifiedMessage, error: true)]
    public static AdditionOperator operator +(QuantifiedExpression leftSide, string rightSide) =>
        throw new InvalidOperationException(QuantifiedSubquery.StringBesideQuantifiedMessage);

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_keyword)
        .AppendSpace()
        .EncloseInParentheses(_operand);
}
