namespace SqlArtisan;

public sealed class CaseThenExpression : SqlExpression
{
    private readonly SqlExpression _thenExpr;

    internal CaseThenExpression(SqlExpression thenExpr)
    {
        _thenExpr = thenExpr;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(_thenExpr);
}
