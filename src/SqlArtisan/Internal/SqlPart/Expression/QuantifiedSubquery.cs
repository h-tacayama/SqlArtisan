namespace SqlArtisan.Internal;

public sealed class QuantifiedSubquery : SqlExpression
{
    private readonly string _keyword;
    private readonly ISubquery _subquery;

    internal QuantifiedSubquery(string keyword, ISubquery subquery)
    {
        _keyword = keyword;
        _subquery = subquery;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_keyword)
        .AppendSpace()
        .EncloseInParentheses(_subquery);
}
