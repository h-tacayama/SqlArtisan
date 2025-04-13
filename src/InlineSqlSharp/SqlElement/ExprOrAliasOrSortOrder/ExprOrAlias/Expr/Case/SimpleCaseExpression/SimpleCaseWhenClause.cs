namespace InlineSqlSharp;

public sealed class SimpleCaseWhenClause(
    SimpleCaseWhenExpr whenExpr,
    CaseThenExpr thenExpr) : ISqlElement
{
    private readonly SimpleCaseWhenExpr _whenExpr = whenExpr;
    private readonly CaseThenExpr _thenExpr = thenExpr;

    public void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.WHEN)
        .AppendSpace(_whenExpr)
        .AppendSpace(Keywords.THEN)
        .Append(_thenExpr);
}
