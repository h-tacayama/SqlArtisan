namespace InlineSqlSharp;

public sealed class SimpleCaseWhenClause<TWhenExpr, TReturnExpr>(
    SimpleCaseWhenExpr<TWhenExpr> whenExpr,
    CaseThenExpr<TReturnExpr> thenExpr) : ISqlElement
    where TWhenExpr : IDataTypeExpr
    where TReturnExpr : IDataTypeExpr
{
    private readonly SimpleCaseWhenExpr<TWhenExpr> _whenExpr = whenExpr;
    private readonly CaseThenExpr<TReturnExpr> _thenExpr = thenExpr;

    public void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.WHEN)
        .AppendSpace(_whenExpr)
        .AppendSpace(Keywords.THEN)
        .Append(_thenExpr);
}
