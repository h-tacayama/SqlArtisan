namespace SqlArtisan.Internal;

public sealed class QuantifiedSubquery(string keyword, ISubquery subquery) : SqlExpression
{
    private readonly SqlPartAgent _subquery = new(subquery.Format);

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append(keyword);
        buffer.Append(' ');
        buffer.EncloseInParentheses(_subquery);
    }
}
