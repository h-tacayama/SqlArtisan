namespace SqlArtisan.Internal;

internal sealed class ScalarSubquery(ISubquery subquery) : SqlExpression
{
    private readonly ISubquery _subquery = SqlBuilderBase.MarkComposed(subquery);

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.EncloseInParentheses(_subquery);
}
