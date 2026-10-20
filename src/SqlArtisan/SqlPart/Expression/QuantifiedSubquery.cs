using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// An <c>ANY</c> / <c>ALL</c> / <c>SOME (SELECT ...)</c> operand, valid in the slots
/// <see cref="QuantifiedExpression"/> lists alone, so it is no <see cref="SqlExpression"/>.
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

    internal const string StringBesideQuantifiedMessage =
        "ANY, ALL or SOME cannot be added to a string; use it as the right operand of a "
            + "comparison, as in col == Any(...).";

    internal static string IncompleteMessageFor(string keyword) =>
        $"{keyword} requires a comparison; use it as the right operand of one, as in "
            + $"col == {keyword[0]}{keyword[1..].ToLowerInvariant()}(...).";

    /// <summary>
    /// Not supported: a string beside an <c>ANY</c> / <c>ALL</c> / <c>SOME</c> operand,
    /// which completes only a predicate's right side; it would otherwise compile as C#
    /// concatenation.
    /// </summary>
    /// <param name="leftSide">The string.</param>
    /// <param name="rightSide">The quantified operand.</param>
    /// <returns>Never returns; the call does not compile.</returns>
    [Obsolete(QuantifiedSubquery.StringBesideQuantifiedMessage, error: true)]
    public static SqlExpression operator +(string leftSide, QuantifiedSubquery rightSide) =>
        throw new InvalidOperationException(QuantifiedSubquery.StringBesideQuantifiedMessage);

    /// <inheritdoc cref="op_Addition(string, QuantifiedSubquery)"/>
    /// <param name="leftSide">The quantified operand.</param>
    /// <param name="rightSide">The string.</param>
    /// <returns>Never returns; the call does not compile.</returns>
    [Obsolete(QuantifiedSubquery.StringBesideQuantifiedMessage, error: true)]
    public static SqlExpression operator +(QuantifiedSubquery leftSide, string rightSide) =>
        throw new InvalidOperationException(QuantifiedSubquery.StringBesideQuantifiedMessage);

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_keyword)
        .AppendSpace()
        .EncloseInParentheses(_subquery);
}
