namespace SqlArtisan.Internal;

/// <summary>
/// An <c>ANY</c> / <c>ALL</c> / <c>SOME (array)</c> operand, valid only as the right
/// operand of a comparison, so it is no <see cref="SqlExpression"/> (#666).
/// </summary>
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
