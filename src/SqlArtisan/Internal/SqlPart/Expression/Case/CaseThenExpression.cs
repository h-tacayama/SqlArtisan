namespace SqlArtisan.Internal;

internal sealed class CaseThenExpression : SqlExpression
{
    private readonly SqlExpression _thenExpr;

    internal CaseThenExpression(SqlExpression thenExpr)
    {
        _thenExpr = thenExpr;
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        _thenExpr.Format(buffer);
}
