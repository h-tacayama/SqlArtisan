namespace SqlArtisan.Internal;

public sealed class ScalarSubquery(ISubquery subquery) : SqlExpression
{
    private readonly SqlPartAgent _subquery = new(subquery.Format);

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.EncloseInParentheses(_subquery);
}
