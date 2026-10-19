namespace SqlArtisan.Internal;

/// <summary>
/// An <c>ANY</c> / <c>ALL</c> / <c>SOME (array)</c> operand, valid in one slot alone, so
/// it is no <see cref="SqlExpression"/> (#666).
/// </summary>
/// <remarks>Taken on the right of a comparison, <c>Like</c> / <c>NotLike</c> and
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

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_keyword)
        .AppendSpace()
        .EncloseInParentheses(_operand);
}
