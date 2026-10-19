namespace SqlArtisan.Internal;

/// <summary>
/// An <c>ANY</c> / <c>ALL</c> / <c>SOME (SELECT ...)</c> operand, valid only as the
/// right operand of a comparison, so it is no <see cref="SqlExpression"/> (#666).
/// </summary>
public sealed class QuantifiedSubquery : SqlPart, IIncompleteExpression
{
    private readonly string _keyword;
    private readonly ISubquery _subquery;

    internal QuantifiedSubquery(string keyword, ISubquery subquery)
    {
        _keyword = keyword;
        _subquery = SqlBuilderBase.Composed(subquery);
    }

    string IIncompleteExpression.IncompleteMessage => IncompleteMessageFor(_keyword);

    internal static string IncompleteMessageFor(string keyword) =>
        $"{keyword} requires a comparison; use it as the right operand of one, as in "
            + $"col == {keyword[0]}{keyword[1..].ToLowerInvariant()}(...).";

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_keyword)
        .AppendSpace()
        .EncloseInParentheses(_subquery);
}
