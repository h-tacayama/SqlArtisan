namespace SqlArtisan;

public sealed class CaseThenExpr : AbstractExpr
{
    private readonly AbstractExpr _thenExpr;

    internal CaseThenExpr(AbstractExpr thenExpr)
    {
        _thenExpr = thenExpr;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(_thenExpr);
}
