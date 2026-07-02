namespace SqlArtisan.Internal;

public sealed class QuantifiedSubquery(string keyword, ISubquery subquery) : SqlExpression
{
    private readonly ISubquery _subquery = subquery;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(keyword)
        .AppendSpace()
        .EncloseInParentheses(_subquery);
}
