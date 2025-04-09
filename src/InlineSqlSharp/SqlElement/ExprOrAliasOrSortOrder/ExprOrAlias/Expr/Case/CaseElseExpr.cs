namespace InlineSqlSharp;
public sealed class CaseElseExpr<TReturnExpr>(TReturnExpr elseExpr) :
    ISqlElement
    where TReturnExpr : IExpr
{
    private readonly TReturnExpr _elseExpr = elseExpr;

    public void FormatSql(SqlBuildingBuffer buffer) =>
        _elseExpr.FormatSql(buffer);
}
