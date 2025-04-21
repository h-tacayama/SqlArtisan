namespace InlineSqlSharp;

public sealed class CaseThenExpr(AbstractExpr thenExpr) : AbstractExpr
{
    private readonly AbstractExpr _thenExpr = thenExpr;

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(_thenExpr);
}
