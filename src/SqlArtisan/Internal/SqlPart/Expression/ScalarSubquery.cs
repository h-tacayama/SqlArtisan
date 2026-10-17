namespace SqlArtisan.Internal;

internal sealed class ScalarSubquery(ISubquery subquery) : SqlExpression
{
    private readonly ISubquery _subquery = SqlBuilderBase.Composed(subquery);

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.EncloseInParentheses(_subquery);
}
