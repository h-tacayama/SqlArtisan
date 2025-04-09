namespace InlineSqlSharp;
public sealed class CaseThenExpr<TReturnExpr>(TReturnExpr thenExpr) :
    ISqlElement
    where TReturnExpr : IExpr
{
    private readonly TReturnExpr _thenExpr = thenExpr;

    public void FormatSql(SqlBuildingBuffer buffer) =>
        _thenExpr.FormatSql(buffer);
}
